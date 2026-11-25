/// <summary>
/// States of the tile-matching flow. Transitions happen synchronously, before
/// any animation for the new state starts.
/// </summary>
public enum MatchFlowState
{
    Loading,
    AwaitingFirstTile,
    AwaitingSecondTile,
    ResolvingMismatch,
    PresentingMatch,
    AwaitingContinue,
    ReturningMatch,
    Complete,
    Failed
}

public enum MatchSelectionResult
{
    Rejected,
    FirstSelected,
    Mismatch,
    Match
}

/// <summary>A tile that can take part in the match flow.</summary>
public interface IMatchTile
{
    /// <summary>Identity used to decide whether two tiles match. Compared by reference.</summary>
    object MatchIdentity { get; }

    /// <summary>True when the tile is face-down, unmatched, and not animating.</summary>
    bool IsSelectable { get; }
}

/// <summary>
/// Plain state model for selection, pair resolution, continuation and completion.
/// Owned by <see cref="GameManager"/>; kept free of Unity types so it can be unit-tested.
/// </summary>
public sealed class MatchFlow<T> where T : class, IMatchTile
{
    public MatchFlowState State { get; private set; } = MatchFlowState.Loading;
    public T First { get; private set; }
    public T Second { get; private set; }
    public int PairCount { get; private set; }
    public int ConfirmedPairs { get; private set; }

    /// <summary>Revealed pairs that did not match on the current board.</summary>
    public int Misses { get; private set; }

    /// <summary>
    /// Incremented whenever running sequences become stale (new board, cancellation, failure).
    /// Sequences capture it at start and must check <see cref="IsCurrent"/> before mutating state.
    /// </summary>
    public int Generation { get; private set; }

    public bool CompletionRaised { get; private set; }

    public bool AcceptsSelection =>
        State == MatchFlowState.AwaitingFirstTile || State == MatchFlowState.AwaitingSecondTile;

    public bool IsLocked => !AcceptsSelection && State != MatchFlowState.AwaitingContinue;

    public bool IsCurrent(int generation) => generation == Generation;

    public void StartBoard(int pairCount)
    {
        Generation++;
        ClearPair();
        PairCount = pairCount < 0 ? 0 : pairCount;
        ConfirmedPairs = 0;
        Misses = 0;
        CompletionRaised = false;
        State = PairCount == 0 ? MatchFlowState.Complete : MatchFlowState.AwaitingFirstTile;
    }

    public void Fail()
    {
        Generation++;
        ClearPair();
        State = MatchFlowState.Failed;
    }

    /// <summary>Returns the flow to <see cref="MatchFlowState.Loading"/> (no board, input ignored).</summary>
    public void ResetToLoading()
    {
        Generation++;
        ClearPair();
        PairCount = 0;
        ConfirmedPairs = 0;
        Misses = 0;
        CompletionRaised = false;
        State = MatchFlowState.Loading;
    }

    public MatchSelectionResult TrySelect(T tile)
    {
        if(tile == null || !AcceptsSelection || !tile.IsSelectable) return MatchSelectionResult.Rejected;

        if(State == MatchFlowState.AwaitingFirstTile)
        {
            First = tile;
            State = MatchFlowState.AwaitingSecondTile;
            return MatchSelectionResult.FirstSelected;
        }

        if(ReferenceEquals(tile, First)) return MatchSelectionResult.Rejected;

        Second = tile;
        bool isMatch = First.MatchIdentity != null && ReferenceEquals(First.MatchIdentity, tile.MatchIdentity);
        State = isMatch ? MatchFlowState.PresentingMatch : MatchFlowState.ResolvingMismatch;
        if(!isMatch) Misses++;
        return isMatch ? MatchSelectionResult.Match : MatchSelectionResult.Mismatch;
    }

    /// <summary>Mismatch feedback finished: clear the pair and accept selections again.</summary>
    public bool CompleteMismatch(int generation)
    {
        if(!IsCurrent(generation) || State != MatchFlowState.ResolvingMismatch) return false;
        ClearPair();
        State = MatchFlowState.AwaitingFirstTile;
        return true;
    }

    /// <summary>The correct-match presentation reached its resting state.</summary>
    public bool CompletePresentation(int generation)
    {
        if(!IsCurrent(generation) || State != MatchFlowState.PresentingMatch) return false;
        State = MatchFlowState.AwaitingContinue;
        return true;
    }

    /// <summary>Consumes the continuation activation. Succeeds at most once per presentation.</summary>
    public bool TryContinue()
    {
        if(State != MatchFlowState.AwaitingContinue) return false;
        State = MatchFlowState.ReturningMatch;
        return true;
    }

    /// <summary>
    /// The matched pair is back on the board. Confirms it and reports whether this
    /// call completed the board (true at most once per board).
    /// </summary>
    public bool CompleteMatchReturn(int generation, out bool boardCompleted)
    {
        boardCompleted = false;
        if(!IsCurrent(generation) || State != MatchFlowState.ReturningMatch) return false;
        boardCompleted = ConfirmPair();
        return true;
    }

    /// <summary>
    /// Cancels any running sequence. When <paramref name="confirmPendingMatch"/> is true, a pair whose
    /// identity already matched is confirmed instead of dropped (used on disable/teardown of a live board).
    /// </summary>
    public void Cancel(bool confirmPendingMatch, out bool boardCompleted)
    {
        boardCompleted = false;
        Generation++;

        bool pendingMatch = State == MatchFlowState.PresentingMatch
            || State == MatchFlowState.AwaitingContinue
            || State == MatchFlowState.ReturningMatch;

        if(pendingMatch && confirmPendingMatch)
        {
            boardCompleted = ConfirmPair();
            return;
        }

        ClearPair();
        if(State != MatchFlowState.Loading && State != MatchFlowState.Failed && State != MatchFlowState.Complete)
        {
            State = MatchFlowState.AwaitingFirstTile;
        }
    }

    private bool ConfirmPair()
    {
        ClearPair();
        ConfirmedPairs++;

        if(ConfirmedPairs >= PairCount)
        {
            State = MatchFlowState.Complete;
            if(!CompletionRaised)
            {
                CompletionRaised = true;
                return true;
            }
            return false;
        }

        State = MatchFlowState.AwaitingFirstTile;
        return false;
    }

    private void ClearPair()
    {
        First = null;
        Second = null;
    }
}
