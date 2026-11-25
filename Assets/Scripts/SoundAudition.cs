using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Dev tooling: an in-game overlay for choosing sound effects in context. Each cue can be stepped through
/// its shipping clip, its candidates from <see cref="SoundAuditionCandidates"/> and silence while playing.
/// Works in Editor play mode and in development builds; release builds strip this object
/// (see SoundAuditionBuildStripper) and compile the component to an empty shell.
/// Editor keys: F8 opens/closes, [ and ] step the last played cue, \ replays it.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoundAudition : MonoBehaviour
{
    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    // Serialized in every build so the scene's serialization layout never depends on the build type.
#pragma warning disable CS0414 // unused in release builds, where the component is an empty shell
    [SerializeField] private SoundEffects soundEffects;
    [SerializeField] private SoundAuditionCandidates candidates;
    [Tooltip("Screen corner for the button that opens the overlay. The open overlay sits on the same edge.")]
    [SerializeField] private Corner corner = Corner.TopLeft;
#pragma warning restore CS0414

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>SessionState key the Editor reads picks from after leaving play mode.</summary>
    public const string PendingPicksKey = "SoundAudition.PendingPicks";

    private const float BaseDpi = 160f;
    private const float Margin = 8f;
    private const float Pad = 6f;
    private const float Row = 44f;
    private const float SmallButton = 48f;
    private const float PanelMaxWidth = 520f;
    private const float DragThreshold = 8f;

    private SoundAuditionModel model;
    private bool open;
    private bool listOpen;
    private bool hasLastCue;
    private SoundCue lastCue;
    private Vector2 listScroll;
    private string toast;
    private float toastUntil;

    private float scale = 1f;
    private Rect blockRect;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle boldStyle;
    private GUIStyle smallStyle;
    private GUIStyle panelStyle;
    private Texture2D panelTexture;

    private bool dragArmed;
    private float dragLastY;
    private float dragDistance;

    private GameObject blockerRoot;
    private RectTransform blocker;

    private void Start()
    {
        if(soundEffects == null) soundEffects = FindFirstObjectByType<SoundEffects>();
        if(soundEffects == null)
        {
            Debug.LogWarning("SoundAudition: no SoundEffects in the scene, audition disabled.", this);
            enabled = false;
            return;
        }

        model = new SoundAuditionModel(soundEffects.ShippingClip, cue => candidates != null ? candidates.For(cue) : null);
        ApplyStartingChoices();
        soundEffects.Played += OnPlayed;
        CreateBlocker();
    }

    private void OnDestroy()
    {
        if(soundEffects != null)
        {
            soundEffects.Played -= OnPlayed;
            soundEffects.ClearAuditionClips();
        }
        if(blockerRoot != null) Destroy(blockerRoot);
        if(panelTexture != null) Destroy(panelTexture);
    }

    private void OnPlayed(SoundCue cue)
    {
        lastCue = cue;
        hasLastCue = true;
    }

    private void Update()
    {
        if(model == null) return;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if(keyboard != null)
        {
            if(keyboard.f8Key.wasPressedThisFrame) open = !open;
            if(open && hasLastCue)
            {
                if(keyboard.leftBracketKey.wasPressedThisFrame) Step(lastCue, -1);
                if(keyboard.rightBracketKey.wasPressedThisFrame) Step(lastCue, 1);
                if(keyboard.backslashKey.wasPressedThisFrame) Preview(lastCue);
            }
        }
#endif

        UpdateBlocker();
    }

    // ---- Actions ---------------------------------------------------------------------------

    private void Step(SoundCue cue, int delta)
    {
        SoundAuditionModel.CueChoices choices = model[cue];
        choices.Step(delta);
        soundEffects.SetAuditionClip(cue, choices.Active);
        Preview(cue);
    }

    private void Preview(SoundCue cue)
    {
        if(model[cue].Active == null) ShowToast($"{cue} is silent");
        soundEffects.Play(cue);
    }

    private void ResetAll()
    {
        foreach(SoundAuditionModel.CueChoices choices in model.All) choices.ResetToStart();
        ApplyStartingChoices();
        ShowToast("All cues back to their starting clips");
    }

    // Cues that ship silent start on their first candidate, so the game has to play that instead of nothing.
    private void ApplyStartingChoices()
    {
        soundEffects.ClearAuditionClips();
        foreach(SoundAuditionModel.CueChoices choices in model.All)
        {
            if(!choices.IsShipping) soundEffects.SetAuditionClip(choices.Cue, choices.Active);
        }
    }

    private void CopyPicks()
    {
        string text = SoundAuditionPicks.Format(model.Picks());
        GUIUtility.systemCopyBuffer = text;
        Debug.Log("Sound audition picks:\n" + text);
        ShowToast("Picks copied to the clipboard");
    }

#if UNITY_EDITOR
    private void ApplyPicks()
    {
        UnityEditor.SessionState.SetString(PendingPicksKey, SoundAuditionPicks.Format(model.Picks()));
        ShowToast("Picks will be applied when play mode ends");
    }
#endif

    private void ShowToast(string message)
    {
        toast = message;
        toastUntil = Time.unscaledTime + 2.5f;
    }

    // ---- Input blocker ---------------------------------------------------------------------

    // IMGUI doesn't stop uGUI or the board from receiving the same tap, so an invisible uGUI image
    // covers whatever the overlay draws. GameManager already ignores board taps over uGUI.
    private void CreateBlocker()
    {
        blockerRoot = new GameObject("SoundAuditionBlocker", typeof(RectTransform));
        blockerRoot.transform.SetParent(transform, false);
        Canvas canvas = blockerRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        blockerRoot.AddComponent<GraphicRaycaster>();

        var imageObject = new GameObject("Block", typeof(RectTransform));
        imageObject.transform.SetParent(blockerRoot.transform, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        blocker = image.rectTransform;
        blocker.anchorMin = Vector2.zero;
        blocker.anchorMax = Vector2.zero;
        blocker.pivot = Vector2.zero;
    }

    private void UpdateBlocker()
    {
        if(blocker == null) return;

        bool active = blockRect.width > 0f && blockRect.height > 0f;
        if(blocker.gameObject.activeSelf != active) blocker.gameObject.SetActive(active);
        if(!active) return;

        // GUI space is top-down and scaled; the overlay canvas is bottom-up in pixels.
        float height = blockRect.height * scale;
        blocker.anchoredPosition = new Vector2(blockRect.x * scale, Screen.height - blockRect.y * scale - height);
        blocker.sizeDelta = new Vector2(blockRect.width * scale, height);
    }

    // ---- Drawing ---------------------------------------------------------------------------

    private void OnGUI()
    {
        if(model == null) return;

        scale = Screen.dpi > 0f ? Mathf.Max(1f, Screen.dpi / BaseDpi) : 1f;
        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        Rect safe = SafeArea();
        if(open) DrawOpen(safe);
        else DrawClosed(safe);

        GUI.matrix = previousMatrix;
    }

    private bool LeftCorner => corner == Corner.TopLeft || corner == Corner.BottomLeft;
    private bool TopCorner => corner == Corner.TopLeft || corner == Corner.TopRight;

    private Rect SafeArea()
    {
        Rect area = Screen.safeArea;
        float top = Screen.height - area.yMax;
        return new Rect(area.x / scale, top / scale, area.width / scale, area.height / scale);
    }

    private void DrawClosed(Rect safe)
    {
        float x = LeftCorner ? safe.x + Margin : safe.xMax - Margin - Row;
        float y = TopCorner ? safe.y + Margin : safe.yMax - Margin - Row;
        var rect = new Rect(x, y, Row, Row);
        blockRect = rect;

        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        if(GUI.Button(rect, "SFX", buttonStyle)) open = true;
        GUI.color = previousColor;
    }

    private void DrawOpen(Rect safe)
    {
        int cueCount = model.All.Count;
        float width = Mathf.Min(safe.width - 2f * Margin, PanelMaxWidth);
        float listHeight = listOpen ? Mathf.Min(cueCount * Row, safe.height * 0.55f) : 0f;
        float height = Pad + Row * 4f + (listOpen ? Pad + listHeight : 0f) + Pad;

        float x = LeftCorner ? safe.x + Margin : safe.xMax - Margin - width;
        float y = TopCorner ? safe.y + Margin : safe.yMax - Margin - height;
        var panel = new Rect(x, y, width, height);
        blockRect = panel;

        GUI.Box(panel, GUIContent.none, panelStyle);

        float left = panel.x + Pad;
        float inner = panel.width - 2f * Pad;
        float rowY = panel.y + Pad;

        // Header: close, title / toast.
        if(GUI.Button(new Rect(left, rowY, SmallButton, Row), "X", buttonStyle)) open = false;
        string title = Time.unscaledTime < toastUntil ? toast : "Sound audition";
        GUI.Label(new Rect(left + SmallButton + Pad, rowY, inner - SmallButton - Pad, Row), title, boldStyle);
        rowY += Row;

        // Last played cue.
        bool previousEnabled = GUI.enabled;
        if(hasLastCue)
        {
            SoundAuditionModel.CueChoices choices = model[lastCue];
            GUI.Label(new Rect(left, rowY, inner, Row), $"Last: {lastCue}\n{ChoiceText(choices)}", labelStyle);
        }
        else
        {
            GUI.Label(new Rect(left, rowY, inner, Row), "Play the game; the last sound cue shows here.", labelStyle);
        }
        rowY += Row;

        GUI.enabled = previousEnabled && hasLastCue;
        float third = (inner - 3f * Pad) / 4f;
        if(GUI.Button(new Rect(left, rowY, third, Row), "<", buttonStyle)) Step(lastCue, -1);
        if(GUI.Button(new Rect(left + (third + Pad), rowY, third, Row), ">", buttonStyle)) Step(lastCue, 1);
        if(GUI.Button(new Rect(left + 2f * (third + Pad), rowY, third, Row), "Play", buttonStyle)) Preview(lastCue);
        GUI.enabled = previousEnabled;
        if(GUI.Button(new Rect(left + 3f * (third + Pad), rowY, third, Row), listOpen ? "Hide list" : "All cues", buttonStyle)) listOpen = !listOpen;
        rowY += Row;

        // Picks.
#if UNITY_EDITOR
        const int actionCount = 3;
#else
        const int actionCount = 2;
#endif
        float actionWidth = (inner - (actionCount - 1) * Pad) / actionCount;
        float actionX = left;
        if(GUI.Button(new Rect(actionX, rowY, actionWidth, Row), "Copy picks", buttonStyle)) CopyPicks();
        actionX += actionWidth + Pad;
#if UNITY_EDITOR
        if(GUI.Button(new Rect(actionX, rowY, actionWidth, Row), "Apply picks", buttonStyle)) ApplyPicks();
        actionX += actionWidth + Pad;
#endif
        if(GUI.Button(new Rect(actionX, rowY, actionWidth, Row), "Reset all", buttonStyle)) ResetAll();
        rowY += Row;

        if(listOpen) DrawList(new Rect(left, rowY + Pad, inner, listHeight));
    }

    private void DrawList(Rect view)
    {
        int cueCount = model.All.Count;
        float contentHeight = cueCount * Row;
        float scrollbar = contentHeight > view.height ? 16f : 0f;
        var content = new Rect(0f, 0f, view.width - scrollbar, contentHeight);

        HandleTouchScroll(view, contentHeight);
        listScroll = GUI.BeginScrollView(view, listScroll, content, false, scrollbar > 0f);

        float buttonsWidth = 3f * SmallButton + 2f * Pad;
        for(int i = 0; i < cueCount; i++)
        {
            SoundAuditionModel.CueChoices choices = model.All[i];
            float rowY = i * Row;
            bool isLast = hasLastCue && choices.Cue == lastCue;

            GUI.Label(new Rect(0f, rowY, content.width - buttonsWidth - Pad, Row),
                $"{(isLast ? "> " : "")}{choices.Cue}\n{ChoiceText(choices)}", labelStyle);

            float buttonX = content.width - buttonsWidth;
            if(GUI.Button(new Rect(buttonX, rowY + 2f, SmallButton, Row - 4f), "<", buttonStyle)) Step(choices.Cue, -1);
            buttonX += SmallButton + Pad;
            if(GUI.Button(new Rect(buttonX, rowY + 2f, SmallButton, Row - 4f), ">", buttonStyle)) Step(choices.Cue, 1);
            buttonX += SmallButton + Pad;
            if(GUI.Button(new Rect(buttonX, rowY + 2f, SmallButton, Row - 4f), "Play", smallStyle)) Preview(choices.Cue);
        }

        GUI.EndScrollView();
    }

    // IMGUI scroll views don't scroll by dragging on touch screens.
    private void HandleTouchScroll(Rect view, float contentHeight)
    {
        Event current = Event.current;
        switch(current.type)
        {
            case EventType.MouseDown:
                dragArmed = view.Contains(current.mousePosition);
                dragLastY = current.mousePosition.y;
                dragDistance = 0f;
                break;
            case EventType.MouseDrag:
                if(!dragArmed) break;
                float delta = current.mousePosition.y - dragLastY;
                dragLastY = current.mousePosition.y;
                dragDistance += Mathf.Abs(delta);
                if(dragDistance < DragThreshold) break;

                listScroll.y = Mathf.Clamp(listScroll.y - delta, 0f, Mathf.Max(0f, contentHeight - view.height));
                GUIUtility.hotControl = 0; // a drag is a scroll, not a button press
                current.Use();
                break;
            case EventType.MouseUp:
                dragArmed = false;
                break;
        }
    }

    private static string ChoiceText(SoundAuditionModel.CueChoices choices)
    {
        string name = choices.Active != null ? ShortName(choices.Active.name) : "(silent)";
        string shipping = choices.IsShipping ? "  [shipping]" : "";
        return $"{name}   {choices.ActiveIndex + 1} / {choices.Choices.Count}{shipping}";
    }

    // "Pack - Sound - 1" style names are too long for a phone: show "Sound - 1".
    private static string ShortName(string clipName)
    {
        int last = clipName.LastIndexOf(" - ", System.StringComparison.Ordinal);
        if(last <= 0) return clipName;
        int previous = clipName.LastIndexOf(" - ", last - 1, System.StringComparison.Ordinal);
        return previous >= 0 ? clipName.Substring(previous + 3) : clipName;
    }

    private void EnsureStyles()
    {
        if(buttonStyle != null) return;

        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
        smallStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
        labelStyle.normal.textColor = Color.white;
        boldStyle = new GUIStyle(labelStyle) { fontSize = 15, fontStyle = FontStyle.Bold };

        panelTexture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        panelTexture.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.08f, 0.88f));
        panelTexture.Apply();
        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
    }
#endif
}
