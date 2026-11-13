using NUnit.Framework;

public class MenuFlowTests
{
    private static MenuFlow ReadyFlow()
    {
        MenuFlow flow = new MenuFlow();
        flow.FinishLoading(true);
        return flow;
    }

    [Test]
    public void StartsLoadingThenShowsTitle()
    {
        MenuFlow flow = new MenuFlow();
        Assert.AreEqual(MenuState.Loading, flow.State);
        Assert.IsTrue(flow.FinishLoading(true));
        Assert.AreEqual(MenuState.Title, flow.State);
        Assert.IsFalse(flow.FinishLoading(true));
    }

    [Test]
    public void FailedSetupKeepsTitleButBlocksPlay()
    {
        MenuFlow flow = new MenuFlow();
        flow.FinishLoading(false);
        Assert.AreEqual(MenuState.Title, flow.State);
        Assert.IsFalse(flow.OpenPicker());
        Assert.IsTrue(flow.OpenSettings());
    }

    [Test]
    public void TitleSettingsAndPickerNavigate()
    {
        MenuFlow flow = ReadyFlow();
        Assert.IsTrue(flow.OpenSettings());
        Assert.IsFalse(flow.OpenPicker());
        Assert.IsTrue(flow.CloseSettings());
        Assert.IsTrue(flow.OpenPicker());
        Assert.IsTrue(flow.ClosePicker());
        Assert.AreEqual(MenuState.Title, flow.State);
    }

    [Test]
    public void GameCanPauseResumeAndAbandon()
    {
        MenuFlow flow = ReadyFlow();
        flow.OpenPicker();
        Assert.IsTrue(flow.StartGame());
        Assert.IsTrue(flow.IsInGame);
        Assert.IsTrue(flow.Pause());
        Assert.IsFalse(flow.Complete());
        Assert.IsTrue(flow.Resume());
        Assert.IsTrue(flow.Pause());
        Assert.IsTrue(flow.Abandon());
        Assert.AreEqual(MenuState.Title, flow.State);
        Assert.IsFalse(flow.ShowSuccessTitle);
    }

    [Test]
    public void CompletingShowsTheSuccessTitleUntilCleared()
    {
        MenuFlow flow = ReadyFlow();
        flow.OpenPicker();
        flow.StartGame();
        Assert.IsTrue(flow.Complete());
        Assert.AreEqual(MenuState.Title, flow.State);
        Assert.IsTrue(flow.ShowSuccessTitle);
        flow.ClearSuccessTitle();
        Assert.IsFalse(flow.ShowSuccessTitle);
    }

    [Test]
    public void StartingAnotherGameClearsTheSuccessTitle()
    {
        MenuFlow flow = ReadyFlow();
        flow.OpenPicker();
        flow.StartGame();
        flow.Complete();
        flow.OpenPicker();
        flow.StartGame();
        Assert.IsFalse(flow.ShowSuccessTitle);
    }

    [Test]
    public void InvalidTransitionsChangeNothing()
    {
        MenuFlow flow = ReadyFlow();
        Assert.IsFalse(flow.StartGame());
        Assert.IsFalse(flow.Pause());
        Assert.IsFalse(flow.Resume());
        Assert.IsFalse(flow.Abandon());
        Assert.IsFalse(flow.Complete());
        Assert.AreEqual(MenuState.Title, flow.State);
    }
}
