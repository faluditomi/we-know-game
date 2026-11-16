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
    public void TimeOutReturnsToTitleFromPlayingOrPaused()
    {
        MenuFlow flow = ReadyFlow();
        flow.OpenPicker();
        flow.StartGame();
        Assert.IsTrue(flow.TimeOut());
        Assert.AreEqual(MenuState.Title, flow.State);

        flow.OpenPicker();
        flow.StartGame();
        flow.Pause();
        Assert.IsTrue(flow.TimeOut());
        Assert.AreEqual(MenuState.Title, flow.State);
        Assert.IsFalse(flow.ShowSuccessTitle);
    }

    [Test]
    public void TimeOutClearsAPendingSuccessTitle()
    {
        MenuFlow flow = ReadyFlow();
        flow.OpenPicker();
        flow.StartGame();
        flow.Complete();
        flow.OpenPicker();
        flow.StartGame();
        flow.TimeOut();
        Assert.IsFalse(flow.ShowSuccessTitle);
    }

    [Test]
    public void SettingsFromPauseReturnToPause()
    {
        MenuFlow flow = ReadyFlow();
        flow.OpenPicker();
        flow.StartGame();
        Assert.IsFalse(flow.OpenSettings(), "settings cannot open over a running board");
        flow.Pause();
        Assert.IsTrue(flow.OpenSettings());
        Assert.AreEqual(MenuState.Settings, flow.State);
        Assert.IsTrue(flow.IsInGame);
        Assert.IsFalse(flow.Resume());
        Assert.IsFalse(flow.Abandon());
        Assert.IsTrue(flow.CloseSettings());
        Assert.AreEqual(MenuState.Paused, flow.State);
        Assert.IsTrue(flow.Resume());
    }

    [Test]
    public void SettingsFromTitleStillReturnToTitleAfterAPauseRoute()
    {
        MenuFlow flow = ReadyFlow();
        flow.OpenPicker();
        flow.StartGame();
        flow.Pause();
        flow.OpenSettings();
        flow.CloseSettings();
        flow.Abandon();
        Assert.IsTrue(flow.OpenSettings());
        Assert.IsFalse(flow.IsInGame);
        Assert.IsTrue(flow.CloseSettings());
        Assert.AreEqual(MenuState.Title, flow.State);
    }

    [Test]
    public void TimeOutFromPauseSettingsShowsTheTitle()
    {
        MenuFlow flow = ReadyFlow();
        flow.OpenPicker();
        flow.StartGame();
        flow.Pause();
        flow.OpenSettings();
        Assert.IsTrue(flow.TimeOut());
        Assert.AreEqual(MenuState.Title, flow.State);
        Assert.IsFalse(flow.IsInGame);
        Assert.IsFalse(flow.ShowSuccessTitle);
    }

    [Test]
    public void TimeOutDoesNothingOnMenuScreens()
    {
        MenuFlow flow = ReadyFlow();
        Assert.IsFalse(flow.TimeOut());
        flow.OpenSettings();
        Assert.IsFalse(flow.TimeOut());
        Assert.AreEqual(MenuState.Settings, flow.State);
        flow.CloseSettings();
        flow.OpenPicker();
        Assert.IsFalse(flow.TimeOut());
        Assert.AreEqual(MenuState.Picker, flow.State);
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
