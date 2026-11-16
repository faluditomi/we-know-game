using NUnit.Framework;

public class MatchFlowTests
{
    private sealed class FakeTile : IMatchTile
    {
        public FakeTile(object identity) { MatchIdentity = identity; }
        public object MatchIdentity { get; }
        public bool Selectable = true;
        public bool IsSelectable => Selectable;
    }

    private static MatchFlow<FakeTile> NewBoard(int pairs)
    {
        MatchFlow<FakeTile> flow = new MatchFlow<FakeTile>();
        flow.StartBoard(pairs);
        return flow;
    }

    [Test]
    public void StartsLoadingAndRejectsSelection()
    {
        MatchFlow<FakeTile> flow = new MatchFlow<FakeTile>();
        Assert.AreEqual(MatchFlowState.Loading, flow.State);
        Assert.AreEqual(MatchSelectionResult.Rejected, flow.TrySelect(new FakeTile(new object())));
    }

    [Test]
    public void FailedFlowRejectsSelection()
    {
        MatchFlow<FakeTile> flow = NewBoard(2);
        flow.Fail();
        Assert.AreEqual(MatchFlowState.Failed, flow.State);
        Assert.AreEqual(MatchSelectionResult.Rejected, flow.TrySelect(new FakeTile(new object())));
    }

    [Test]
    public void MatchUsesIdentityReferenceNotEquality()
    {
        object shared = new object();
        MatchFlow<FakeTile> flow = NewBoard(2);
        Assert.AreEqual(MatchSelectionResult.FirstSelected, flow.TrySelect(new FakeTile(shared)));
        Assert.AreEqual(MatchSelectionResult.Match, flow.TrySelect(new FakeTile(shared)));
        Assert.AreEqual(MatchFlowState.PresentingMatch, flow.State);

        MatchFlow<FakeTile> other = NewBoard(2);
        other.TrySelect(new FakeTile("apple"));
        Assert.AreEqual(MatchSelectionResult.Mismatch, other.TrySelect(new FakeTile(new string('a', 1) + "pple")));
        Assert.AreEqual(MatchFlowState.ResolvingMismatch, other.State);
    }

    [Test]
    public void MismatchesAreCountedAndResetPerBoard()
    {
        MatchFlow<FakeTile> flow = NewBoard(2);
        for(int i = 0; i < 3; i++)
        {
            flow.TrySelect(new FakeTile(new object()));
            Assert.AreEqual(MatchSelectionResult.Mismatch, flow.TrySelect(new FakeTile(new object())));
            Assert.IsTrue(flow.CompleteMismatch(flow.Generation));
        }

        object shared = new object();
        flow.TrySelect(new FakeTile(shared));
        flow.TrySelect(new FakeTile(shared));
        Assert.AreEqual(3, flow.Misses, "a match is not a miss");

        flow.StartBoard(2);
        Assert.AreEqual(0, flow.Misses);
    }

    [Test]
    public void NullIdentityNeverMatches()
    {
        MatchFlow<FakeTile> flow = NewBoard(1);
        flow.TrySelect(new FakeTile(null));
        Assert.AreEqual(MatchSelectionResult.Mismatch, flow.TrySelect(new FakeTile(null)));
    }

    [Test]
    public void SameTileCannotFillSecondSlot()
    {
        MatchFlow<FakeTile> flow = NewBoard(2);
        FakeTile tile = new FakeTile(new object());
        flow.TrySelect(tile);
        Assert.AreEqual(MatchSelectionResult.Rejected, flow.TrySelect(tile));
        Assert.AreEqual(MatchFlowState.AwaitingSecondTile, flow.State);
        Assert.IsNull(flow.Second);
    }

    [Test]
    public void UnselectableTileIsRejected()
    {
        MatchFlow<FakeTile> flow = NewBoard(2);
        Assert.AreEqual(MatchSelectionResult.Rejected, flow.TrySelect(new FakeTile(new object()) { Selectable = false }));
        Assert.AreEqual(MatchFlowState.AwaitingFirstTile, flow.State);
    }

    [Test]
    public void LockedStatesRejectThirdTile()
    {
        MatchFlow<FakeTile> flow = NewBoard(2);
        flow.TrySelect(new FakeTile(new object()));
        flow.TrySelect(new FakeTile(new object()));
        Assert.AreEqual(MatchFlowState.ResolvingMismatch, flow.State);
        FakeTile first = flow.First;
        FakeTile second = flow.Second;

        Assert.AreEqual(MatchSelectionResult.Rejected, flow.TrySelect(new FakeTile(new object())));
        Assert.AreSame(first, flow.First);
        Assert.AreSame(second, flow.Second);
        Assert.IsFalse(flow.TryContinue());
    }

    [Test]
    public void MismatchCompletionClearsPairAndUnlocks()
    {
        MatchFlow<FakeTile> flow = NewBoard(2);
        flow.TrySelect(new FakeTile(new object()));
        flow.TrySelect(new FakeTile(new object()));
        Assert.IsTrue(flow.CompleteMismatch(flow.Generation));
        Assert.AreEqual(MatchFlowState.AwaitingFirstTile, flow.State);
        Assert.IsNull(flow.First);
        Assert.IsNull(flow.Second);
        Assert.IsFalse(flow.CompleteMismatch(flow.Generation), "Unlock must happen exactly once.");
    }

    [Test]
    public void ContinueIsConsumedOnceAndOnlyAfterPresentationRests()
    {
        object shared = new object();
        MatchFlow<FakeTile> flow = NewBoard(2);
        flow.TrySelect(new FakeTile(shared));
        flow.TrySelect(new FakeTile(shared));

        Assert.IsFalse(flow.TryContinue(), "Continue must not be possible while presenting.");
        Assert.IsTrue(flow.CompletePresentation(flow.Generation));
        Assert.IsTrue(flow.TryContinue());
        Assert.IsFalse(flow.TryContinue());
        Assert.AreEqual(MatchFlowState.ReturningMatch, flow.State);
    }

    [Test]
    public void ConfirmedPairsCountAndCompletionIsRaisedOnce()
    {
        MatchFlow<FakeTile> flow = NewBoard(2);

        Assert.IsFalse(ResolveMatch(flow), "First pair must not complete a two-pair board.");
        Assert.AreEqual(1, flow.ConfirmedPairs);
        Assert.AreEqual(MatchFlowState.AwaitingFirstTile, flow.State);

        Assert.IsTrue(ResolveMatch(flow));
        Assert.AreEqual(2, flow.ConfirmedPairs);
        Assert.AreEqual(MatchFlowState.Complete, flow.State);
        Assert.IsTrue(flow.CompletionRaised);

        Assert.AreEqual(MatchSelectionResult.Rejected, flow.TrySelect(new FakeTile(new object())));
        Assert.IsFalse(flow.CompleteMatchReturn(flow.Generation, out bool again));
        Assert.IsFalse(again);
    }

    [Test]
    public void StaleGenerationCannotMutateState()
    {
        MatchFlow<FakeTile> flow = NewBoard(2);
        flow.TrySelect(new FakeTile(new object()));
        flow.TrySelect(new FakeTile(new object()));
        int stale = flow.Generation;

        flow.StartBoard(2);
        Assert.AreNotEqual(stale, flow.Generation);
        Assert.IsFalse(flow.CompleteMismatch(stale));
        Assert.AreEqual(MatchFlowState.AwaitingFirstTile, flow.State);
        Assert.AreEqual(0, flow.ConfirmedPairs);
    }

    [Test]
    public void CancelDuringMismatchReturnsToSelection()
    {
        MatchFlow<FakeTile> flow = NewBoard(2);
        flow.TrySelect(new FakeTile(new object()));
        flow.TrySelect(new FakeTile(new object()));
        int before = flow.Generation;

        flow.Cancel(confirmPendingMatch: true, out bool completed);
        Assert.IsFalse(completed);
        Assert.AreNotEqual(before, flow.Generation);
        Assert.AreEqual(MatchFlowState.AwaitingFirstTile, flow.State);
        Assert.IsNull(flow.First);
        Assert.AreEqual(0, flow.ConfirmedPairs);
    }

    [Test]
    public void CancelDuringPresentationCanConfirmPendingMatch()
    {
        object shared = new object();
        MatchFlow<FakeTile> flow = NewBoard(1);
        flow.TrySelect(new FakeTile(shared));
        flow.TrySelect(new FakeTile(shared));

        flow.Cancel(confirmPendingMatch: true, out bool completed);
        Assert.IsTrue(completed);
        Assert.AreEqual(1, flow.ConfirmedPairs);
        Assert.AreEqual(MatchFlowState.Complete, flow.State);
    }

    [Test]
    public void CancelDuringPresentationCanDropPendingMatch()
    {
        object shared = new object();
        MatchFlow<FakeTile> flow = NewBoard(2);
        flow.TrySelect(new FakeTile(shared));
        flow.TrySelect(new FakeTile(shared));

        flow.Cancel(confirmPendingMatch: false, out bool completed);
        Assert.IsFalse(completed);
        Assert.AreEqual(0, flow.ConfirmedPairs);
        Assert.AreEqual(MatchFlowState.AwaitingFirstTile, flow.State);
    }

    private static bool ResolveMatch(MatchFlow<FakeTile> flow)
    {
        object shared = new object();
        flow.TrySelect(new FakeTile(shared));
        flow.TrySelect(new FakeTile(shared));
        flow.CompletePresentation(flow.Generation);
        flow.TryContinue();
        Assert.IsTrue(flow.CompleteMatchReturn(flow.Generation, out bool completed));
        return completed;
    }
}
