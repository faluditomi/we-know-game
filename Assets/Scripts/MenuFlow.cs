/// <summary>Screens of the menu flow around a match.</summary>
public enum MenuState
{
    Loading,
    Title,
    Settings,
    Picker,
    Playing,
    Paused
}

/// <summary>
/// Plain state model for the menu screens. Owned by <see cref="MenuController"/>;
/// kept free of Unity types so it can be unit-tested. Every method returns false and
/// changes nothing when the transition is not allowed from the current state.
/// </summary>
public sealed class MenuFlow
{
    public MenuState State { get; private set; } = MenuState.Loading;

    /// <summary>False when the game assets failed to load; PLAY then cannot start a game.</summary>
    public bool GameAvailable { get; private set; }

    /// <summary>True while the title should read the success variant after a completed game.</summary>
    public bool ShowSuccessTitle { get; private set; }

    /// <summary>True during a game, including the settings screen opened from the pause menu.</summary>
    public bool IsInGame => State == MenuState.Playing || State == MenuState.Paused || (State == MenuState.Settings && settingsReturn == MenuState.Paused);

    /// <summary>Screen the settings screen returns to: the title or the pause menu.</summary>
    private MenuState settingsReturn = MenuState.Title;

    public bool FinishLoading(bool gameAvailable)
    {
        if(State != MenuState.Loading) return false;
        GameAvailable = gameAvailable;
        State = MenuState.Title;
        return true;
    }

    /// <summary>Opens settings from the title or the pause menu; <see cref="CloseSettings"/> returns there.</summary>
    public bool OpenSettings()
    {
        if(State != MenuState.Title && State != MenuState.Paused) return false;
        settingsReturn = State;
        State = MenuState.Settings;
        return true;
    }

    public bool CloseSettings() => Move(MenuState.Settings, settingsReturn);

    public bool OpenPicker()
    {
        if(!GameAvailable) return false;
        return Move(MenuState.Title, MenuState.Picker);
    }

    public bool ClosePicker() => Move(MenuState.Picker, MenuState.Title);

    public bool StartGame()
    {
        if(!Move(MenuState.Picker, MenuState.Playing)) return false;
        ShowSuccessTitle = false;
        return true;
    }

    public bool Pause() => Move(MenuState.Playing, MenuState.Paused);
    public bool Resume() => Move(MenuState.Paused, MenuState.Playing);

    /// <summary>MAIN MENU from the pause menu: the board is discarded and no success title shows.</summary>
    public bool Abandon()
    {
        if(!Move(MenuState.Paused, MenuState.Title)) return false;
        ShowSuccessTitle = false;
        return true;
    }

    /// <summary>No input for the inactivity timeout during a game: the board is discarded and the normal title shows.</summary>
    public bool TimeOut()
    {
        if(!IsInGame) return false;
        State = MenuState.Title;
        ShowSuccessTitle = false;
        return true;
    }

    /// <summary>The board was completed: return to the title with the success title.</summary>
    public bool Complete()
    {
        if(!Move(MenuState.Playing, MenuState.Title)) return false;
        ShowSuccessTitle = true;
        return true;
    }

    /// <summary>The success-title duration elapsed.</summary>
    public void ClearSuccessTitle() => ShowSuccessTitle = false;

    private bool Move(MenuState from, MenuState to)
    {
        if(State != from) return false;
        State = to;
        return true;
    }
}
