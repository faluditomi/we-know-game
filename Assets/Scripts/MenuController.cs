using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Colours and font shared by every menu screen.</summary>
[Serializable]
public sealed class MenuStyle
{
    [Tooltip("We Know logo teal. Used for buttons, toggles, sliders and the valid picker selection.")]
    public Color accentColor = new Color32(0x12, 0x85, 0x80, 0xFF);
    public Color textColor = Color.white;
    public Color invalidColor = new Color(0.82f, 0.32f, 0.32f, 1f);
    public Color pickerIdleColor = new Color(1f, 1f, 1f, 0.16f);
    [Tooltip("Font for every menu label. Leave empty to keep each label's own font.")]
    public TMP_FontAsset font;
}

/// <summary>
/// Owns the menu screens around a match: title (with the timed success title), grid size picker,
/// settings, pause menu and exit. Starts, pauses and abandons games through <see cref="GameManager"/>
/// and returns to the title when a board is completed. Screen state lives in <see cref="MenuFlow"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuController : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TileWallBackground background;
    [Tooltip("Board/Pause: opens or closes the pause menu, and goes back from the picker and settings.")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("Screens")]
    [SerializeField] private RectTransform safeAreaRoot;
    [Tooltip("Readability overlay shown behind every menu screen.")]
    [SerializeField] private GameObject menuOverlay;
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject pickerScreen;
    [SerializeField] private GameObject settingsScreen;
    [Tooltip("Settings panel. Keeps its scene position when opened from the title; centred when opened from the pause menu.")]
    [SerializeField] private RectTransform settingsPanel;
    [SerializeField] private GameObject pauseScreen;
    [Tooltip("In-game controls (the pause button).")]
    [SerializeField] private GameObject gameHud;

    [Header("Title")]
    [SerializeField] private Image titleImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Sprite titleSprite;
    [SerializeField] private Sprite successTitleSprite;
    [SerializeField] private string titleLabel = "We Know... Do You?";
    [SerializeField] private string successTitleLabel = "We Know... And Now You Do Too!";
    [Tooltip("Seconds the success title stays up after a completed game.")]
    [Min(0f)] [SerializeField] private float successTitleDuration = 4f;
    [Tooltip("Extra seconds the completed board stays after the victory moment, before returning to the title.")]
    [Min(0f)] [SerializeField] private float completionReturnDelay = 0f;
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Round statistics shown under the success title, e.g. \"8 pairs · 1:32 · 3 misses\".")]
    [SerializeField] private TMP_Text roundStatsText;
    [Tooltip("Optional backing panel of the statistics line; shown and hidden with it.")]
    [SerializeField] private GameObject roundStatsPanel;
    [Tooltip("Image behind the title that pulses as a glow when the success title enters. Uses the title sprite.")]
    [SerializeField] private UnityEngine.UI.Image titleGlow;
    [Tooltip("Seconds the success title takes to pop in.")]
    [Min(0.05f)] [SerializeField] private float titlePopDuration = 0.45f;
    [Tooltip("Seconds of the glow pulse behind the success title.")]
    [Min(0.05f)] [SerializeField] private float titleGlowDuration = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float titleGlowAlpha = 0.95f;
    [Tooltip("Glow size relative to the title, so it reaches past the opaque artwork.")]
    [Min(1f)] [SerializeField] private float titleGlowScale = 1.2f;

    [Header("Title buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Picker")]
    [SerializeField] private GridSizePicker picker;
    [SerializeField] private Button pickerBackButton;

    [Header("Settings")]
    [SerializeField] private Toggle vibrationToggle;
    [Tooltip("Battery saver: caps the frame rate at 60 instead of the screen's refresh rate.")]
    [SerializeField] private Toggle batterySaverToggle;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider musicSlider;
    [Tooltip("Inactivity timeout slider. Whole-number steps; each step is 5 seconds (3 = 15 s … 60 = 300 s).")]
    [SerializeField] private Slider inactivitySlider;
    [SerializeField] private TMP_Text inactivityValueText;
    [Tooltip("Language dropdown. Its options are filled from the supported languages at startup.")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Button settingsBackButton;

    [Header("Board sizes")]
    [Tooltip("Free choice / Preset sizes. Its options are filled in code in the current language.")]
    [SerializeField] private TMP_Dropdown boardSizeDropdown;
    [Tooltip("Opens the preset editor. Only works on the settings screen opened from the title.")]
    [SerializeField] private Button editSizesButton;
    [Tooltip("Small note shown while preset mode has no usable sizes.")]
    [SerializeField] private GameObject boardSizeNote;
    [Tooltip("Picker heading: CHOOSE YOUR BOARD, or EDIT BOARD SIZES in the editor.")]
    [SerializeField] private TMP_Text pickerHeading;
    [Tooltip("The picker's message line; hidden with the grid when the preset buttons are shown.")]
    [SerializeField] private GameObject pickerMessage;
    [Tooltip("Area for the preset buttons, which replace the grid in preset mode.")]
    [SerializeField] private RectTransform presetButtonsPanel;
    [Tooltip("Inactive button cloned for each preset; its first TMP label shows the size.")]
    [SerializeField] private Button presetButtonTemplate;
    [Min(1f)] [SerializeField] private Vector2 presetButtonSize = new Vector2(330f, 96f);
    [SerializeField] private Vector2 presetButtonSpacing = new Vector2(20f, 16f);
    [Tooltip("Preset list beside the grid in the editor.")]
    [SerializeField] private RectTransform presetListPanel;
    [Tooltip("Inactive row cloned for each preset: a \"Label\" text child and a remove button. Its position is the first row's.")]
    [SerializeField] private RectTransform presetRowTemplate;
    [Min(1f)] [SerializeField] private float presetRowHeight = 88f;
    [Tooltip("Shown in the preset list while it is empty.")]
    [SerializeField] private GameObject presetListEmpty;
    [Tooltip("The picker's main panel (heading, grid, back). Scaled down with the preset list to fit short screens.")]
    [SerializeField] private RectTransform pickerPanel;
    [Tooltip("Space kept between a panel that had to be scaled down to fit and the safe area's edges.")]
    [Min(0f)] [SerializeField] private float panelFitMargin = 24f;

    [Header("Inactivity")]
    [Tooltip("Mouse or pen movement smaller than this (pixels) is ignored, so jitter cannot keep an abandoned game alive.")]
    [Min(0f)] [SerializeField] private float pointerMoveThreshold = 4f;

    [Header("Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button continueButton;
    [Tooltip("Opens the settings screen from the pause menu; its back action returns to the pause menu.")]
    [SerializeField] private Button pauseSettingsButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Sound")]
    [Tooltip("Sound effects for buttons, toggles, sliders and the picker. Falls back to the first one in the scene; without one the menus are silent.")]
    [SerializeField] private SoundEffects soundEffects;
    [Tooltip("Background music, scaled by the Music volume setting. Falls back to the first one in the scene.")]
    [SerializeField] private MusicPlayer music;

    [Header("Style")]
    [SerializeField] private MenuStyle style = new MenuStyle();

    private readonly MenuFlow flow = new MenuFlow();
    private GameSettings settings;
    private InactivityTimer inactivity;
    private Vector2 lastPointerPosition;
    private bool hasPointerPosition;
    private float successRemaining;
    private Coroutine completionRoutine;
    private Rect appliedSafeArea;
    private Vector2Int appliedScreenSize;
    private LocalizedLabel[] localizedLabels = Array.Empty<LocalizedLabel>();
    private GameLanguage appliedLanguage;
    private bool appliedBatterySaver;
    private string statusKey;
    private float titleEntranceElapsed = -1f;
    private Vector2 settingsAnchorMin, settingsAnchorMax, settingsPivot, settingsPosition;
    private Vector2 pickerPanelPosition, presetListPosition;
    private readonly System.Collections.Generic.List<GameObject> presetButtons = new System.Collections.Generic.List<GameObject>();
    private readonly System.Collections.Generic.List<GameObject> presetRows = new System.Collections.Generic.List<GameObject>();

    private int DesignCount => gameManager != null ? gameManager.DesignCount : 0;

    public MenuState State => flow.State;
    public GameSettings Settings => settings;

    private void Awake()
    {
        if(gameManager == null) gameManager = FindFirstObjectByType<GameManager>();

        settings = new GameSettings(new PlayerPrefsSettingsStorage(), gameManager == null || gameManager.VibrationDefault);
        if(gameManager != null) gameManager.UseSettings(settings);
        if(soundEffects == null) soundEffects = FindFirstObjectByType<SoundEffects>();
        if(soundEffects != null) soundEffects.UseSettings(settings);
        if(music == null) music = FindFirstObjectByType<MusicPlayer>();
        if(music != null) music.UseSettings(settings);
        inactivity = new InactivityTimer(settings.InactivityTimeoutSeconds);
        settings.Changed += () => inactivity.TimeoutSeconds = settings.InactivityTimeoutSeconds;
        settings.Changed += OnSettingsChanged;
        appliedBatterySaver = settings.BatterySaver;
        FrameRate.Apply(appliedBatterySaver);

        // Includes labels on inactive screens and outside this hierarchy (e.g. the match presentation prompt).
        localizedLabels = FindObjectsByType<LocalizedLabel>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Bind(playButton, OnPlay);
        Bind(settingsButton, OnOpenSettings);
        Bind(exitButton, OnExit);
        Bind(pickerBackButton, OnPickerBack);
        Bind(settingsBackButton, OnSettingsBack);
        Bind(pauseButton, OnPause);
        Bind(continueButton, OnContinue);
        Bind(pauseSettingsButton, OnOpenSettings);
        Bind(mainMenuButton, OnMainMenu);
        Bind(editSizesButton, OnEditSizes);
        if(presetButtonTemplate != null) presetButtonTemplate.gameObject.SetActive(false);
        if(presetRowTemplate != null) presetRowTemplate.gameObject.SetActive(false);

        if(picker != null)
        {
            picker.SelectionConfirmed += OnBoardSelected;
            picker.SelectionChanged += () => PlaySound(SoundCue.PickerCell);
        }
        BindSettingsControls();

        if(settingsPanel != null)
        {
            settingsAnchorMin = settingsPanel.anchorMin;
            settingsAnchorMax = settingsPanel.anchorMax;
            settingsPivot = settingsPanel.pivot;
            settingsPosition = settingsPanel.anchoredPosition;
        }
        // The grid's parent is the picker panel; use it when the field isn't wired.
        if(pickerPanel == null && picker != null) pickerPanel = picker.transform.parent as RectTransform;
        if(pickerPanel != null) pickerPanelPosition = pickerPanel.anchoredPosition;
        if(presetListPanel != null) presetListPosition = presetListPanel.anchoredPosition;

#if UNITY_IOS && !UNITY_EDITOR
        if(exitButton != null) exitButton.gameObject.SetActive(false);
#endif

        ApplyStyle();
        ApplyLanguage();
        Refresh();
    }

    private void OnEnable()
    {
        if(gameManager != null)
        {
            gameManager.SetupSucceeded += OnSetupSucceeded;
            gameManager.SetupFailed += OnSetupFailed;
            gameManager.BoardCompleted += OnBoardCompleted;
        }

        InputAction action = pauseAction != null ? pauseAction.action : null;
        if(action != null)
        {
            action.performed += OnPauseAction;
            action.Enable();
        }

        InputSystem.onEvent += OnInputEvent;
    }

    private void OnDisable()
    {
        if(gameManager != null)
        {
            gameManager.SetupSucceeded -= OnSetupSucceeded;
            gameManager.SetupFailed -= OnSetupFailed;
            gameManager.BoardCompleted -= OnBoardCompleted;
        }

        InputAction action = pauseAction != null ? pauseAction.action : null;
        if(action != null) action.performed -= OnPauseAction;

        InputSystem.onEvent -= OnInputEvent;
    }

    private void Start()
    {
        // Setup may already have finished (or failed) before this component subscribed.
        if(flow.State != MenuState.Loading || gameManager == null) return;
        if(gameManager.IsReady) OnSetupSucceeded();
        else if(gameManager.HasSetupFailed) OnSetupFailed();
    }

    private void Update()
    {
        ApplySafeAreaIfChanged();

        if(inactivity != null && inactivity.Tick(Time.unscaledDeltaTime, flow.IsInGame)) OnInactivityTimeout();

        if(titleEntranceElapsed >= 0f) AnimateTitleEntrance(Time.unscaledDeltaTime);

        if(flow.ShowSuccessTitle && !flow.IsInGame)
        {
            successRemaining -= Time.unscaledDeltaTime;
            if(successRemaining <= 0f)
            {
                flow.ClearSuccessTitle();
                RefreshTitle();
            }
        }
    }

    // ---- Game events -------------------------------------------------------------------------

    private void OnSetupSucceeded()
    {
        if(!flow.FinishLoading(true)) return;
        if(picker != null) picker.Build(gameManager.MaxBoardSize, gameManager.DesignCount);
        if(background != null) background.Build(gameManager.TilePrefab, gameManager.TileVariations);
        SetStatus(null);
        Refresh();
    }

    private void OnSetupFailed()
    {
        if(!flow.FinishLoading(false)) return;
        if(playButton != null) playButton.interactable = false;
        SetStatus(LocalizedText.LoadFailed);
        Refresh();
    }

    private void OnBoardCompleted()
    {
        if(flow.State != MenuState.Playing) return;
        if(completionRoutine != null) StopCoroutine(completionRoutine);
        completionRoutine = StartCoroutine(ReturnAfterCompletion());
    }

    private IEnumerator ReturnAfterCompletion()
    {
        // The victory moment runs on the completed board first; it holds while paused.
        while(gameManager != null && gameManager.IsVictoryPlaying)
        {
            yield return null;
            if(!flow.IsInGame) yield break;
        }

        float elapsed = 0f;
        while(elapsed < completionReturnDelay)
        {
            yield return null;
            if(!flow.IsInGame) yield break;
            // Pausing holds the completed board on screen; the delay continues after CONTINUE.
            if(flow.State == MenuState.Playing) elapsed += Time.unscaledDeltaTime;
        }

        completionRoutine = null;
        gameManager.AbandonGame();
        if(!flow.Complete()) yield break;
        successRemaining = successTitleDuration;
        if(background != null) background.Celebrate(successTitleDuration);
        Refresh();
        PlaySound(SoundCue.SuccessTitle);
        titleEntranceElapsed = 0f;
        AnimateTitleEntrance(0f);
    }

    // ---- Buttons -----------------------------------------------------------------------------

    private void OnPlay()
    {
        if(!flow.OpenPicker()) return;

        // Preset mode with a single usable size: there is nothing to choose, so start that board straight away.
        if(settings.UsesBoardPresets(DesignCount))
        {
            System.Collections.Generic.List<BoardSize> usable = settings.UsableBoardPresets(DesignCount);
            if(usable.Count == 1)
            {
                StartBoard(usable[0].Columns, usable[0].Rows);
                if(flow.State == MenuState.Playing) return;
            }
        }

        PlaySound(SoundCue.ButtonClick);
        Refresh();
    }

    private void OnOpenSettings()
    {
        if(!flow.OpenSettings()) return;
        PlaySound(SoundCue.ButtonClick);
        Refresh();
    }

    private void OnSettingsBack()
    {
        if(!flow.CloseSettings()) return;
        PlaySound(SoundCue.ButtonBack);
        Refresh();
    }

    /// <summary>The picker's BACK: to the title, or from the preset editor back to settings.</summary>
    private void OnPickerBack()
    {
        bool moved = flow.State == MenuState.PresetEditor ? flow.ClosePresetEditor() : flow.ClosePicker();
        if(!moved) return;
        PlaySound(SoundCue.ButtonBack);
        Refresh();
    }

    private void OnEditSizes()
    {
        if(!flow.OpenPresetEditor()) return;
        PlaySound(SoundCue.ButtonClick);
        Refresh();
    }

    private void OnBoardSelected(int columns, int rows)
    {
        if(flow.State == MenuState.PresetEditor)
        {
            AddPreset(new BoardSize(columns, rows));
            return;
        }
        StartBoard(columns, rows);
    }

    private void StartBoard(int columns, int rows)
    {
        if(flow.State != MenuState.Picker || gameManager == null) return;
        if(!gameManager.StartGame(columns, rows)) return;
        flow.StartGame();
        PlaySound(SoundCue.BoardStart);
        inactivity.Reset();
        Refresh();
    }

    /// <summary>No input for the inactivity timeout during a game: drop the board and show the title, without asking.</summary>
    private void OnInactivityTimeout()
    {
        if(!flow.IsInGame) return;
        if(completionRoutine != null) StopCoroutine(completionRoutine);
        completionRoutine = null;
        gameManager.AbandonGame();
        flow.TimeOut();
        Refresh();
    }

    /// <summary>
    /// Restarts the inactivity timer on real player input: any keyboard or touchscreen event, and mouse or pen
    /// buttons, scrolling, or movement beyond <see cref="pointerMoveThreshold"/>. Other devices are ignored.
    /// This sees input the board ignores (locked states, UI taps, pause), unlike gameplay actions.
    /// </summary>
    private void OnInputEvent(UnityEngine.InputSystem.LowLevel.InputEventPtr eventPtr, InputDevice device)
    {
        if(inactivity == null || device == null) return;
        if(!eventPtr.IsA<UnityEngine.InputSystem.LowLevel.StateEvent>() && !eventPtr.IsA<UnityEngine.InputSystem.LowLevel.DeltaStateEvent>()) return;

        if(device is Keyboard || device is Touchscreen)
        {
            inactivity.Reset();
            return;
        }

        if(!(device is Pointer pointer)) return;

        foreach(InputControl control in eventPtr.EnumerateChangedControls(device))
        {
            if(control is UnityEngine.InputSystem.Controls.ButtonControl || (device is Mouse mouse && control.parent == mouse.scroll))
            {
                inactivity.Reset();
                return;
            }

            if(control.parent == pointer.position && pointer.position.ReadValueFromEvent(eventPtr, out Vector2 position))
            {
                if(!hasPointerPosition || (position - lastPointerPosition).sqrMagnitude >= pointerMoveThreshold * pointerMoveThreshold)
                {
                    if(hasPointerPosition) inactivity.Reset();
                    lastPointerPosition = position;
                    hasPointerPosition = true;
                }
                return;
            }
        }
    }

    private void OnPause()
    {
        if(!flow.Pause()) return;
        PlaySound(SoundCue.Pause);
        gameManager.SetPaused(true);
        Refresh();
    }

    private void OnContinue()
    {
        if(!flow.Resume()) return;
        PlaySound(SoundCue.Resume);
        gameManager.SetPaused(false);
        Refresh();
    }

    private void OnMainMenu()
    {
        if(!flow.Abandon()) return;
        PlaySound(SoundCue.ButtonBack);
        if(completionRoutine != null) StopCoroutine(completionRoutine);
        completionRoutine = null;
        gameManager.AbandonGame();
        Refresh();
    }

    private void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_IOS
        Application.Quit();
#endif
    }

    /// <summary>Escape / Android back: toggles pause in a game, goes back from the picker and settings.</summary>
    private void OnPauseAction(InputAction.CallbackContext context)
    {
        switch(flow.State)
        {
            case MenuState.Playing: OnPause(); break;
            case MenuState.Paused: OnContinue(); break;
            case MenuState.Picker: OnPickerBack(); break;
            case MenuState.PresetEditor: OnPickerBack(); break;
            case MenuState.Settings: OnSettingsBack(); break;
        }
    }

    // ---- Settings ----------------------------------------------------------------------------

    private void BindSettingsControls()
    {
        if(vibrationToggle != null)
        {
            vibrationToggle.SetIsOnWithoutNotify(settings.Vibration);
            vibrationToggle.onValueChanged.AddListener(value =>
            {
                settings.Vibration = value;
                PlaySound(SoundCue.Toggle);
            });
        }

        if(batterySaverToggle != null)
        {
            batterySaverToggle.SetIsOnWithoutNotify(settings.BatterySaver);
            batterySaverToggle.onValueChanged.AddListener(value =>
            {
                settings.BatterySaver = value;
                PlaySound(SoundCue.Toggle);
            });
        }

        if(soundSlider != null)
        {
            soundSlider.minValue = 0f;
            soundSlider.maxValue = 1f;
            soundSlider.SetValueWithoutNotify(settings.SoundVolume);
            // The tick plays at the new volume, so dragging previews how loud the game will be.
            soundSlider.onValueChanged.AddListener(value =>
            {
                settings.SoundVolume = value;
                PlaySound(SoundCue.SliderTick);
            });
        }

        if(musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.SetValueWithoutNotify(settings.MusicVolume);
            musicSlider.onValueChanged.AddListener(value =>
            {
                settings.MusicVolume = value;
                PlaySound(SoundCue.SliderTick);
            });
        }

        if(inactivitySlider != null)
        {
            int step = GameSettings.InactivityTimeoutStepSeconds;
            inactivitySlider.wholeNumbers = true;
            inactivitySlider.minValue = GameSettings.MinInactivityTimeoutSeconds / step;
            inactivitySlider.maxValue = GameSettings.MaxInactivityTimeoutSeconds / step;
            inactivitySlider.SetValueWithoutNotify(settings.InactivityTimeoutSeconds / step);
            inactivitySlider.onValueChanged.AddListener(value =>
            {
                settings.InactivityTimeoutSeconds = Mathf.RoundToInt(value) * step;
                RefreshInactivityLabel();
                PlaySound(SoundCue.SliderTick);
            });
        }
        RefreshInactivityLabel();

        if(languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            foreach(GameLanguage language in GameLanguages.All) options.Add(GameLanguages.NativeName(language));
            languageDropdown.AddOptions(options);
            languageDropdown.SetValueWithoutNotify(Array.IndexOf(GameLanguages.All, settings.Language));
            languageDropdown.onValueChanged.AddListener(index =>
            {
                if(index >= 0 && index < GameLanguages.All.Length) settings.Language = GameLanguages.All[index];
                PlaySound(SoundCue.DropdownSelect);
            });
        }

        if(boardSizeDropdown != null)
        {
            FillBoardSizeOptions();
            boardSizeDropdown.onValueChanged.AddListener(index =>
            {
                settings.BoardSizeMode = index == (int)BoardSizeMode.Presets ? BoardSizeMode.Presets : BoardSizeMode.FreeChoice;
                PlaySound(SoundCue.DropdownSelect);
            });
        }
    }

    /// <summary>Free choice / Preset sizes in the current language, showing the current mode.</summary>
    private void FillBoardSizeOptions()
    {
        if(boardSizeDropdown == null) return;
        boardSizeDropdown.ClearOptions();
        boardSizeDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            LocalizedText.Get(settings.Language, LocalizedText.FreeChoice),
            LocalizedText.Get(settings.Language, LocalizedText.PresetSizes)
        });
        boardSizeDropdown.SetValueWithoutNotify((int)settings.BoardSizeMode);
        boardSizeDropdown.RefreshShownValue();
    }

    /// <summary>Note while preset mode has no usable sizes; EDIT SIZES only from the title's settings.</summary>
    private void RefreshBoardSizeControls()
    {
        if(boardSizeDropdown != null && boardSizeDropdown.value != (int)settings.BoardSizeMode)
            boardSizeDropdown.SetValueWithoutNotify((int)settings.BoardSizeMode);
        if(editSizesButton != null) editSizesButton.interactable = flow.CanEditPresets;
        SetActive(boardSizeNote, settings.BoardSizeMode == BoardSizeMode.Presets && !settings.UsesBoardPresets(DesignCount));
    }

    // ---- Board size presets ------------------------------------------------------------------

    private void AddPreset(BoardSize size)
    {
        PresetAddResult result = settings.AddBoardPreset(size, DesignCount);
        string message;
        switch(result)
        {
            case PresetAddResult.Added:
                message = LocalizedText.Format(settings.Language, LocalizedText.PresetAdded, size.Columns, size.Rows);
                PlaySound(SoundCue.ButtonClick);
                break;
            case PresetAddResult.Duplicate:
                message = LocalizedText.Get(settings.Language, LocalizedText.PresetDuplicate);
                break;
            case PresetAddResult.Full:
                message = LocalizedText.Get(settings.Language, LocalizedText.PresetListFull);
                break;
            default:
                message = LocalizedText.Get(settings.Language, BoardSelection.Describe(BoardSelection.Validate(size.Columns, size.Rows, DesignCount)));
                break;
        }

        if(picker != null) picker.ShowMessage(message, result != PresetAddResult.Added);
        RebuildPresetList();
    }

    private void RemovePreset(BoardSize size)
    {
        if(!settings.RemoveBoardPreset(size)) return;
        PlaySound(SoundCue.ButtonBack);
        RebuildPresetList();
    }

    private string PresetLabel(BoardSize size) =>
        LocalizedText.Format(settings.Language, LocalizedText.PresetSize, size.Columns, size.Rows, size.Pairs);

    /// <summary>One button per usable preset, two per row, the last one centred when the count is odd.</summary>
    private void RebuildPresetButtons()
    {
        ClearClones(presetButtons);
        if(presetButtonsPanel == null || presetButtonTemplate == null) return;

        System.Collections.Generic.List<BoardSize> sizes = settings.UsableBoardPresets(DesignCount);
        float rowWidth = presetButtonSize.x * 2f + presetButtonSpacing.x;
        float left = (presetButtonsPanel.rect.width - rowWidth) * 0.5f;

        for(int i = 0; i < sizes.Count; i++)
        {
            BoardSize size = sizes[i];
            Button button = Instantiate(presetButtonTemplate, presetButtonsPanel);
            button.name = $"Preset {size}";
            button.gameObject.SetActive(true);
            presetButtons.Add(button.gameObject);

            int column = i % 2, row = i / 2;
            bool alone = column == 0 && i == sizes.Count - 1;
            float x = alone ? (presetButtonsPanel.rect.width - presetButtonSize.x) * 0.5f : left + column * (presetButtonSize.x + presetButtonSpacing.x);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = presetButtonSize;
            rect.anchoredPosition = new Vector2(x, -row * (presetButtonSize.y + presetButtonSpacing.y));

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if(label != null)
            {
                MakeCompact(label);
                label.text = PresetLabel(size);
                label.rectTransform.offsetMin = new Vector2(presetButtonSize.y, label.rectTransform.offsetMin.y);
            }
            AddBoardIcon(rect, size, presetButtonSize.y * 0.62f);
            button.onClick.AddListener(() => StartBoard(size.Columns, size.Rows));
        }
    }

    /// <summary>The editor's list: every preset with a remove button; unusable ones dimmed and marked.</summary>
    private void RebuildPresetList()
    {
        ClearClones(presetRows);
        if(presetListPanel == null || presetRowTemplate == null) return;

        System.Collections.Generic.IReadOnlyList<BoardSize> sizes = settings.BoardPresets;
        SetActive(presetListEmpty, sizes.Count == 0);
        Vector2 first = presetRowTemplate.anchoredPosition;

        for(int i = 0; i < sizes.Count; i++)
        {
            BoardSize size = sizes[i];
            RectTransform row = Instantiate(presetRowTemplate, presetListPanel);
            row.name = $"Preset {size}";
            row.gameObject.SetActive(true);
            row.anchoredPosition = first - new Vector2(0f, i * presetRowHeight);
            presetRows.Add(row.gameObject);

            bool usable = BoardPresets.IsUsable(size, DesignCount);
            Transform labelTransform = row.Find("Label");
            if(labelTransform != null && labelTransform.TryGetComponent(out TMP_Text label))
            {
                MakeCompact(label);
                label.text = usable ? PresetLabel(size) : $"{PresetLabel(size)}  ({LocalizedText.Get(settings.Language, LocalizedText.PresetUnavailable)})";
                AddBoardIcon((RectTransform)labelTransform, size, Mathf.Min(48f, row.rect.height * 0.8f), true);
            }
            if(!usable) row.gameObject.AddComponent<CanvasGroup>().alpha = 0.45f;

            Button remove = row.GetComponentInChildren<Button>(true);
            if(remove != null) remove.onClick.AddListener(() => RemovePreset(size));
        }
    }

    /// <summary>
    /// Draws a small grid of the board's shape at the left of <paramref name="parent"/>, in the picker's cell style.
    /// With <paramref name="outside"/>, it sits just left of the parent instead of inside it.
    /// </summary>
    private void AddBoardIcon(RectTransform parent, BoardSize size, float box, bool outside = false)
    {
        var icon = new GameObject("Icon", typeof(RectTransform));
        icon.layer = parent.gameObject.layer;
        RectTransform iconRect = (RectTransform)icon.transform;
        iconRect.SetParent(parent, false);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(outside ? 1f : 0f, 0.5f);
        iconRect.sizeDelta = new Vector2(box, box);
        iconRect.anchoredPosition = new Vector2(outside ? -box * 0.25f : box * 0.3f, 0f);

        Sprite sprite = picker != null ? picker.CellSprite : null;
        float step = box / Mathf.Max(size.Columns, size.Rows);
        float cell = step * 0.82f;
        Vector2 origin = new Vector2((box - step * size.Columns) * 0.5f, -(box - step * size.Rows) * 0.5f);
        for(int y = 0; y < size.Rows; y++)
        {
            for(int x = 0; x < size.Columns; x++)
            {
                var cellObject = new GameObject("Cell", typeof(RectTransform));
                cellObject.layer = icon.layer;
                RectTransform cellRect = (RectTransform)cellObject.transform;
                cellRect.SetParent(iconRect, false);
                cellRect.anchorMin = cellRect.anchorMax = cellRect.pivot = new Vector2(0f, 1f);
                cellRect.sizeDelta = new Vector2(cell, cell);
                cellRect.anchoredPosition = origin + new Vector2(x * step, -y * step);
                Image image = cellObject.AddComponent<Image>();
                image.sprite = sprite;
                image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                image.color = new Color(1f, 1f, 1f, 0.9f);
                image.raycastTarget = false;
            }
        }
    }

    /// <summary>
    /// Size labels sit beside an icon in a narrow button, so they drop the menu's wide letter spacing
    /// and shrink to fit on one line.
    /// </summary>
    private static void MakeCompact(TMP_Text label)
    {
        label.characterSpacing = -2f;
        label.wordSpacing = -10f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = Mathf.Max(label.fontSizeMax, 26f);
    }

    private static void ClearClones(System.Collections.Generic.List<GameObject> clones)
    {
        foreach(GameObject clone in clones) if(clone != null) Destroy(clone);
        clones.Clear();
    }

    private void RefreshInactivityLabel()
    {
        if(inactivityValueText != null)
            inactivityValueText.text = LocalizedText.Format(settings.Language, LocalizedText.SecondsValue, settings.InactivityTimeoutSeconds);
    }

    // ---- Language ----------------------------------------------------------------------------

    private void OnSettingsChanged()
    {
        if(settings.Language != appliedLanguage) ApplyLanguage();
        if(settings.BatterySaver != appliedBatterySaver)
        {
            appliedBatterySaver = settings.BatterySaver;
            FrameRate.Apply(appliedBatterySaver);
        }
        RefreshBoardSizeControls();
    }

    /// <summary>Shows every localized label, the picker messages and code-driven texts in the current language. The title stays English.</summary>
    private void ApplyLanguage()
    {
        appliedLanguage = settings.Language;
        foreach(LocalizedLabel label in localizedLabels)
        {
            if(label != null) label.Apply(appliedLanguage);
        }

        if(picker != null) picker.SetLanguage(appliedLanguage);
        if(languageDropdown != null) languageDropdown.SetValueWithoutNotify(Array.IndexOf(GameLanguages.All, appliedLanguage));
        FillBoardSizeOptions();
        RefreshPickerHeading();
        RefreshInactivityLabel();
        RefreshStatus();
        RefreshRoundStats();
    }

    // ---- Presentation ------------------------------------------------------------------------

    private void Refresh()
    {
        MenuState state = flow.State;
        bool inMenu = !flow.IsInGame;

        SetActive(menuOverlay, inMenu);
        SetActive(titleScreen, state == MenuState.Title || state == MenuState.Loading);
        SetActive(pickerScreen, state == MenuState.Picker || state == MenuState.PresetEditor);
        SetActive(settingsScreen, state == MenuState.Settings);
        if(state == MenuState.Settings)
        {
            PlaceSettingsPanel(flow.IsInGame);
            RefreshBoardSizeControls();
        }
        SetActive(pauseScreen, state == MenuState.Paused);
        SetActive(gameHud, state == MenuState.Playing);
        RefreshPickerPanels(state);

        if(playButton != null) playButton.interactable = flow.GameAvailable;
        if(background != null) background.SetVisible(inMenu);
        RefreshTitle();
    }

    /// <summary>
    /// Picker screen content: the grid (free choice), the preset buttons (preset mode with usable sizes),
    /// or the grid with the preset list (editor).
    /// </summary>
    private void RefreshPickerPanels(MenuState state)
    {
        bool editing = state == MenuState.PresetEditor;
        bool picking = state == MenuState.Picker;
        bool presetButtonsShown = picking && settings.UsesBoardPresets(DesignCount);
        bool gridShown = editing || (picking && !presetButtonsShown);

        if(picker != null) SetActive(picker.gameObject, gridShown || !(picking || editing));
        SetActive(pickerMessage, gridShown || !(picking || editing));
        if(presetButtonsPanel != null) SetActive(presetButtonsPanel.gameObject, presetButtonsShown);
        if(presetListPanel != null) SetActive(presetListPanel.gameObject, editing);
        if(picking || editing) FitPickerPanels();

        if(gridShown && picker != null) picker.ResetSelection();
        if(presetButtonsShown) RebuildPresetButtons();
        else ClearClones(presetButtons);
        if(editing) RebuildPresetList();
        else ClearClones(presetRows);
        RefreshPickerHeading();
    }

    private void RefreshPickerHeading()
    {
        if(pickerHeading == null) return;
        string key = flow.State == MenuState.PresetEditor ? LocalizedText.EditBoardSizes : LocalizedText.ChooseBoard;
        pickerHeading.text = LocalizedText.Get(settings.Language, key);
    }

    /// <summary>Centres the settings panel over a paused game; otherwise restores its scene position beside the title.</summary>
    private void PlaceSettingsPanel(bool centred)
    {
        if(settingsPanel == null) return;
        Vector2 centre = new Vector2(0.5f, 0.5f);
        settingsPanel.anchorMin = centred ? centre : settingsAnchorMin;
        settingsPanel.anchorMax = centred ? centre : settingsAnchorMax;
        settingsPanel.pivot = centred ? centre : settingsPivot;
        settingsPanel.anchoredPosition = centred ? Vector2.zero : settingsPosition;
        FitPanel(settingsPanel, settingsPosition, centred, out _);
    }

    /// <summary>Scales the picker panel, and the preset list beside it, down together when they don't fit.</summary>
    private void FitPickerPanels()
    {
        if(pickerPanel == null) return;
        FitPanel(pickerPanel, pickerPanelPosition, false, out float scale);
        if(presetListPanel == null) return;

        presetListPanel.localScale = Vector3.one * scale;
        presetListPanel.anchoredPosition = new Vector2(
            pickerPanelPosition.x + (presetListPosition.x - pickerPanelPosition.x) * scale,
            pickerPanel.anchoredPosition.y);
    }

    /// <summary>
    /// Shrinks a fixed-size panel that would reach past its screen's safe area (short or very wide screens make
    /// the canvas shorter than the 1080 reference), and moves it up so it sits <see cref="panelFitMargin"/> from the top.
    /// A panel that fits keeps its scene position and size. <paramref name="home"/> is its scene position.
    /// </summary>
    private void FitPanel(RectTransform panel, Vector2 home, bool centred, out float scale)
    {
        scale = 1f;
        if(panel == null || !(panel.parent is RectTransform area)) return;

        Vector2 available = area.rect.size;
        Vector2 size = panel.rect.size;
        if(size.x <= 0f || size.y <= 0f || available.y <= 0f) return;

        float margin = panelFitMargin;
        float fitHeight = centred ? available.y - 2f * margin : available.y - Mathf.Max(margin, -home.y) - margin;
        float fitWidth = centred ? available.x - 2f * margin : available.x - home.x - margin;
        scale = Mathf.Clamp(Mathf.Min(fitHeight / size.y, fitWidth / size.x), 0.5f, 1f);

        if(!centred && scale < 1f)
        {
            // Not even moving it up to the margin would let it fit at full size: use the space from the margin down.
            float fromMargin = Mathf.Min((available.y - 2f * margin) / size.y, fitWidth / size.x);
            scale = Mathf.Clamp(fromMargin, 0.5f, 1f);
            panel.anchoredPosition = new Vector2(home.x, -margin);
        }
        else if(!centred)
        {
            panel.anchoredPosition = home;
        }
        panel.localScale = Vector3.one * scale;
    }

    private void RefreshTitle()
    {
        bool success = flow.ShowSuccessTitle;
        Sprite sprite = success ? successTitleSprite : titleSprite;

        if(titleImage != null)
        {
            titleImage.gameObject.SetActive(sprite != null);
            titleImage.sprite = sprite;
            titleImage.preserveAspect = true;
        }

        if(titleText != null)
        {
            titleText.gameObject.SetActive(sprite == null);
            titleText.text = success ? successTitleLabel : titleLabel;
        }

        if(titleGlow != null)
        {
            titleGlow.sprite = sprite;
            titleGlow.preserveAspect = true;
            if(!success || sprite == null) SetGlowAlpha(0f);
        }

        RefreshRoundStats();
        if(!success) ResetTitleEntrance();
    }

    /// <summary>Statistics line under the success title; hidden with the normal title or when no board was completed.</summary>
    private void RefreshRoundStats()
    {
        if(roundStatsText == null) return;
        bool show = flow.ShowSuccessTitle && gameManager != null && gameManager.TryGetLastRoundStats(out _);
        if(show && gameManager.TryGetLastRoundStats(out RoundStats stats))
        {
            roundStatsText.text = RoundStatsText.Format(settings.Language, stats);
        }
        roundStatsText.gameObject.SetActive(show);
        if(roundStatsPanel != null) roundStatsPanel.SetActive(show);
    }

    private RectTransform VisibleTitle =>
        titleImage != null && titleImage.gameObject.activeSelf ? titleImage.rectTransform
        : titleText != null ? titleText.rectTransform : null;

    /// <summary>Pop-in (0.82 → 1.06 → 1.0) and glow pulse of the success title, on unscaled time.</summary>
    private void AnimateTitleEntrance(float deltaTime)
    {
        titleEntranceElapsed += deltaTime;
        float pop = Mathf.Clamp01(titleEntranceElapsed / titlePopDuration);
        float scale = pop < 0.6f
            ? Mathf.Lerp(0.82f, 1.06f, 1f - (1f - pop / 0.6f) * (1f - pop / 0.6f))
            : Mathf.Lerp(1.06f, 1f, Mathf.SmoothStep(0f, 1f, (pop - 0.6f) / 0.4f));

        RectTransform title = VisibleTitle;
        if(title != null) title.localScale = Vector3.one * scale;

        float glow = Mathf.Clamp01(titleEntranceElapsed / titleGlowDuration);
        if(titleGlow != null)
        {
            titleGlow.rectTransform.localScale = Vector3.one * scale * titleGlowScale;
            SetGlowAlpha(titleGlow.sprite != null ? titleGlowAlpha * Mathf.Sin(Mathf.PI * glow) : 0f);
        }

        if(pop >= 1f && glow >= 1f) ResetTitleEntrance();
    }

    private void ResetTitleEntrance()
    {
        titleEntranceElapsed = -1f;
        if(titleImage != null) titleImage.rectTransform.localScale = Vector3.one;
        if(titleText != null) titleText.rectTransform.localScale = Vector3.one;
        if(titleGlow != null) titleGlow.rectTransform.localScale = Vector3.one * titleGlowScale;
        SetGlowAlpha(0f);
    }

    private void SetGlowAlpha(float alpha)
    {
        if(titleGlow == null) return;
        Color colour = style.accentColor;
        colour.a = alpha;
        titleGlow.color = colour;
        titleGlow.enabled = alpha > 0.001f;
    }

    /// <summary>Shows the <see cref="LocalizedText"/> entry <paramref name="key"/> as the status line, or hides it when null.</summary>
    private void SetStatus(string key)
    {
        statusKey = key;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if(statusText == null) return;
        bool visible = !string.IsNullOrEmpty(statusKey);
        statusText.text = visible ? LocalizedText.Get(settings.Language, statusKey) : string.Empty;
        statusText.gameObject.SetActive(visible);
    }

    /// <summary>Applies the accent colour and font to every control under this menu.</summary>
    private void ApplyStyle()
    {
        Color accent = style.accentColor;

        foreach(Button button in GetComponentsInChildren<Button>(true))
        {
            if(button.targetGraphic != null) button.targetGraphic.color = accent;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        foreach(Toggle toggle in GetComponentsInChildren<Toggle>(true))
        {
            if(toggle.graphic != null) toggle.graphic.color = accent;
        }

        foreach(Slider slider in GetComponentsInChildren<Slider>(true))
        {
            if(slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fill)) fill.color = accent;
        }

        if(style.font != null)
        {
            foreach(TMP_Text label in GetComponentsInChildren<TMP_Text>(true)) label.font = style.font;
        }

        if(picker != null) picker.SetColors(accent, style.invalidColor, style.pickerIdleColor);
    }

    private void ApplySafeAreaIfChanged()
    {
        if(safeAreaRoot == null) return;
        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if((safe == appliedSafeArea && size == appliedScreenSize) || size.x <= 0 || size.y <= 0) return;

        appliedSafeArea = safe;
        appliedScreenSize = size;
        safeAreaRoot.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
        safeAreaRoot.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;

        // The usable height changed: fit the fixed-size panels again.
        if(flow.State == MenuState.Settings) PlaceSettingsPanel(flow.IsInGame);
        FitPickerPanels();
    }

    private void PlaySound(SoundCue cue)
    {
        if(soundEffects != null) soundEffects.Play(cue);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if(button != null) button.onClick.AddListener(action);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if(target != null && target.activeSelf != active) target.SetActive(active);
    }
}
