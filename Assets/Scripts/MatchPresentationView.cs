using UnityEngine;

/// <summary>
/// Screen-space backdrop for a correct match: a dimmer behind the presented tiles and a
/// tap-to-continue prompt. The description itself is shown on the matched tiles
/// (see <see cref="TileController.SetPresentationFace"/>).
///
/// Expected hierarchy:
/// <code>
/// Canvas (Screen Space - Camera, so the dimmer sits between the board and the presented tiles)
/// └── MatchPresentation  [MatchPresentationView, CanvasGroup]
///     ├── Dimmer          [Image]
///     └── SafeArea        [RectTransform, stretched]
///         └── Prompt      [CanvasGroup] → TMP "Tap anywhere to continue"
/// </code>
/// </summary>
[DisallowMultipleComponent]
public sealed class MatchPresentationView : MonoBehaviour
{
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform safeAreaRoot;
    [SerializeField] private CanvasGroup promptGroup;
    [Min(0.01f)] [SerializeField] private float fadeDuration = 0.2f;

    private Canvas canvas;
    private Rect appliedSafeArea;
    private Vector2Int appliedScreenSize;
    private bool initialised;
    private float targetAlpha;
    private float currentAlpha;

    public bool IsValid => rootGroup != null;
    public bool IsFullyHidden => currentAlpha <= 0f && targetAlpha <= 0f;

    private void Awake()
    {
        Initialise();
        HideImmediate();
    }

    private void Update()
    {
        ApplySafeAreaIfChanged();

        if(!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.unscaledDeltaTime / fadeDuration);
            if(rootGroup != null) rootGroup.alpha = currentAlpha;
        }
    }

    public void Show()
    {
        Initialise();
        gameObject.SetActive(true);
        SetPromptVisible(false);
        targetAlpha = 1f;
        if(rootGroup != null)
        {
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = false;
        }
        ApplySafeAreaIfChanged(true);
    }

    public void SetPromptVisible(bool visible)
    {
        if(promptGroup == null) return;
        promptGroup.alpha = visible ? 1f : 0f;
    }

    public void Hide()
    {
        targetAlpha = 0f;
        SetPromptVisible(false);
        if(rootGroup != null) rootGroup.blocksRaycasts = false;
    }

    public void HideImmediate()
    {
        targetAlpha = 0f;
        currentAlpha = 0f;
        if(rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }
        SetPromptVisible(false);
    }

    /// <summary>
    /// When the canvas renders through a camera, place it between the presented tiles and the board so
    /// the dimmer covers the board but not the presented tiles.
    /// </summary>
    public void ConfigureDepth(Camera gameplayCamera, float presentationDepth, float boardDistance)
    {
        Initialise();
        if(canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera) return;
        if(canvas.worldCamera == null) canvas.worldCamera = gameplayCamera;
        float far = boardDistance > presentationDepth ? boardDistance : presentationDepth + 1f;
        canvas.planeDistance = Mathf.Lerp(presentationDepth, far, 0.5f);
    }

    private void Initialise()
    {
        if(initialised) return;
        if(!IsValid)
        {
            Debug.LogError("MatchPresentationView is missing its root CanvasGroup.", this);
            return;
        }

        canvas = GetComponentInParent<Canvas>();
        if(canvas != null) canvas = canvas.rootCanvas;
        initialised = true;
    }

    private void ApplySafeAreaIfChanged(bool force = false)
    {
        if(safeAreaRoot == null) return;
        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if(!force && safe == appliedSafeArea && size == appliedScreenSize) return;
        if(size.x <= 0 || size.y <= 0) return;

        appliedSafeArea = safe;
        appliedScreenSize = size;
        safeAreaRoot.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
        safeAreaRoot.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
    }
}
