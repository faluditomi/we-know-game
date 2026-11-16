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
    [Tooltip("Seconds the completed board stays visible before returning to the title.")]
    [Min(0f)] [SerializeField] private float completionReturnDelay = 1f;
    [SerializeField] private TMP_Text statusText;

    [Header("Title buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Picker")]
    [SerializeField] private GridSizePicker picker;
    [SerializeField] private Button pickerBackButton;

    [Header("Settings")]
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider musicSlider;
    [Tooltip("Inactivity timeout slider. Whole-number steps; each step is 5 seconds (3 = 15 s … 60 = 300 s).")]
    [SerializeField] private Slider inactivitySlider;
    [SerializeField] private TMP_Text inactivityValueText;
    [Tooltip("Language dropdown. Its options are filled from the supported languages at startup.")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Button settingsBackButton;

    [Header("Inactivity")]
    [Tooltip("Mouse or pen movement smaller than this (pixels) is ignored, so jitter cannot keep an abandoned game alive.")]
    [Min(0f)] [SerializeField] private float pointerMoveThreshold = 4f;

    [Header("Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button continueButton;
    [Tooltip("Opens the settings screen from the pause menu; its back action returns to the pause menu.")]
    [SerializeField] private Button pauseSettingsButton;
    [SerializeField] private Button mainMenuButton;

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
    private string statusKey;
    private Vector2 settingsAnchorMin, settingsAnchorMax, settingsPivot, settingsPosition;

    public MenuState State => flow.State;
    public GameSettings Settings => settings;

    private void Awake()
    {
        if(gameManager == null) gameManager = FindFirstObjectByType<GameManager>();

        settings = new GameSettings(new PlayerPrefsSettingsStorage(), gameManager == null || gameManager.BlockedInputHapticsDefault);
        if(gameManager != null) gameManager.UseSettings(settings);
        inactivity = new InactivityTimer(settings.InactivityTimeoutSeconds);
        settings.Changed += () => inactivity.TimeoutSeconds = settings.InactivityTimeoutSeconds;
        settings.Changed += OnSettingsChanged;

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

        if(picker != null) picker.SelectionConfirmed += OnBoardSelected;
        BindSettingsControls();

        if(settingsPanel != null)
        {
            settingsAnchorMin = settingsPanel.anchorMin;
            settingsAnchorMax = settingsPanel.anchorMax;
            settingsPivot = settingsPanel.pivot;
            settingsPosition = settingsPanel.anchoredPosition;
        }

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
    }

    // ---- Buttons -----------------------------------------------------------------------------

    private void OnPlay()
    {
        if(flow.OpenPicker()) Refresh();
    }

    private void OnOpenSettings()
    {
        if(flow.OpenSettings()) Refresh();
    }

    private void OnSettingsBack()
    {
        if(flow.CloseSettings()) Refresh();
    }

    private void OnPickerBack()
    {
        if(flow.ClosePicker()) Refresh();
    }

    private void OnBoardSelected(int columns, int rows)
    {
        if(flow.State != MenuState.Picker || gameManager == null) return;
        if(!gameManager.StartGame(columns, rows)) return;
        flow.StartGame();
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
        gameManager.SetPaused(true);
        Refresh();
    }

    private void OnContinue()
    {
        if(!flow.Resume()) return;
        gameManager.SetPaused(false);
        Refresh();
    }

    private void OnMainMenu()
    {
        if(!flow.Abandon()) return;
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
            case MenuState.Settings: OnSettingsBack(); break;
        }
    }

    // ---- Settings ----------------------------------------------------------------------------

    private void BindSettingsControls()
    {
        if(vibrationToggle != null)
        {
            vibrationToggle.SetIsOnWithoutNotify(settings.Vibration);
            vibrationToggle.onValueChanged.AddListener(value => settings.Vibration = value);
        }

        if(soundSlider != null)
        {
            soundSlider.minValue = 0f;
            soundSlider.maxValue = 1f;
            soundSlider.SetValueWithoutNotify(settings.SoundVolume);
            soundSlider.onValueChanged.AddListener(value => settings.SoundVolume = value);
        }

        if(musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.SetValueWithoutNotify(settings.MusicVolume);
            musicSlider.onValueChanged.AddListener(value => settings.MusicVolume = value);
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
            });
        }
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
        RefreshInactivityLabel();
        RefreshStatus();
    }

    // ---- Presentation ------------------------------------------------------------------------

    private void Refresh()
    {
        MenuState state = flow.State;
        bool inMenu = !flow.IsInGame;

        SetActive(menuOverlay, inMenu);
        SetActive(titleScreen, state == MenuState.Title || state == MenuState.Loading);
        SetActive(pickerScreen, state == MenuState.Picker);
        SetActive(settingsScreen, state == MenuState.Settings);
        if(state == MenuState.Settings) PlaceSettingsPanel(flow.IsInGame);
        SetActive(pauseScreen, state == MenuState.Paused);
        SetActive(gameHud, state == MenuState.Playing);

        if(playButton != null) playButton.interactable = flow.GameAvailable;
        if(picker != null && state == MenuState.Picker) picker.ResetSelection();
        if(background != null) background.SetVisible(inMenu);
        RefreshTitle();
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
