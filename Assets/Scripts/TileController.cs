using TMPro;
using UnityEngine;

/// <summary>
/// Owns a single tile's data, eligibility and visual state. Animation is driven
/// frame-by-frame by <see cref="GameManager"/> through the pose methods below, so
/// one owning sequence can be cancelled and every tile restored to a known state.
/// </summary>
[DisallowMultipleComponent]
public sealed class TileController : MonoBehaviour, IMatchTile
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int ClipRectId = Shader.PropertyToID("_ClipRect");
    private static readonly int HueId = Shader.PropertyToID("_Hue");
    private const string ClipRectKeyword = "UNITY_UI_CLIP_RECT";
    private const float RevealedAngle = 180f;
    private const float SeamOverlapFraction = 0.02f;

    [Header("Visual references (wire in Prefab Mode)")]
    [Tooltip("Child that flips, shakes and spins. Falls back to this transform when unassigned.")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer bodyRenderer;
    [Tooltip("Renderer that shows the tile's logo when it is revealed / matched.")]
    [SerializeField] private Renderer logoRenderer;
    [Tooltip("Largest width and height the logo may cover, as a fraction of the logo quad's prefab size. The logo keeps its aspect ratio inside this box.")]
    [SerializeField] private Vector2 logoBounds = Vector2.one;
    [Tooltip("Renderers on the face-down side that are hidden while the description half is shown (the face-down logo).")]
    [SerializeField] private Renderer[] faceDownRenderers = new Renderer[0];
    [Tooltip("3D TextMeshPro on the face-down side. Its rect must be twice the face width: each tile of a found pair shows one clipped half, so the joined pair reads as the full description.")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Hologram panel behind the revealed logo. Takes a faint hue from the tile's own colour, so each design stays recognisable without the frame giving the pair away.")]
    [SerializeField] private Renderer screenRenderer;
    [Range(0f, 1f)]
    [Tooltip("How much of the tile's colour reaches the hologram panel. Low values keep dark logo artwork readable.")]
    [SerializeField] private float screenHueStrength = 0.3f;

    [Header("Pair seam")]
    [Tooltip("Bezel parts swapped for seam-clipped meshes while a matched pair is joined, so the two tiles read as one wide panel. Leave empty on decorative tiles.")]
    [SerializeField] private SeamClipPart[] seamClipParts = new SeamClipPart[0];
    [Tooltip("Pivot holding the bezel that sits on this tile's left edge, with its origin on the cut plane. Scaled to nothing along X while that edge is the seam, so the bezel retracts instead of popping.")]
    [SerializeField] private Transform seamCapLeft;
    [Tooltip("Same for the right edge.")]
    [SerializeField] private Transform seamCapRight;
    [Tooltip("Half-width of the clipped tile, in tile units. The pair joins at twice this, so the cut bezel runs meet exactly. Must match the clip plane the seam meshes were generated with.")]
    [SerializeField] private float seamHalfWidth = 0.41f;

    /// <summary>One bezel part and the two meshes that replace it at the seam.</summary>
    [System.Serializable]
    public sealed class SeamClipPart
    {
        public MeshFilter target;
        [Tooltip("The unclipped mesh, restored when the tile is back on the board.")]
        public Mesh full;
        [Tooltip("Used when this tile is the left half of a joined pair: its right-hand bezel is gone.")]
        public Mesh keepLeft;
        [Tooltip("Used when this tile is the right half of a joined pair: its left-hand bezel is gone.")]
        public Mesh keepRight;
    }

    private Tile tile;
    private bool referencesResolved;
    private bool poseCaptured;

    private Vector3 boardLocalPosition;
    private Quaternion boardLocalRotation;
    private Vector3 boardLocalScale;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;
    private Vector3 flipAxis;
    private Vector3 shakeDirection;

    private Color bodyBaseColor = Color.white;
    private Color logoBaseColor = Color.white;
    private MaterialPropertyBlock bodyBlock;
    private MaterialPropertyBlock logoBlock;
    private MaterialPropertyBlock screenBlock;
    private bool seamClipped;
    private bool seamLeftHalf = true;
    private Vector3 logoBaseLocalScale = Vector3.one;
    private Vector3 descriptionBaseLocalPosition;
    private Material descriptionMaterial;

    private float flipAngle;
    private Vector3 sequenceStartPosition;
    private Vector3 sequenceStartScale;
    private float sequenceStartAngle;
    private GameLanguage language;

    public Tile Data => tile;
    public object MatchIdentity => tile;
    public string Description => tile != null ? tile.GetDescription(language) : string.Empty;
    public bool IsRevealed { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsAnimating { get; set; }
    public bool IsSelectable => tile != null && !IsMatched && !IsRevealed && !IsAnimating && isActiveAndEnabled;
    public Vector3 BoardLocalScale => boardLocalScale;
    public float FlipAngle => flipAngle;

    private Transform Visual => visualRoot != null ? visualRoot : transform;

    private void Awake()
    {
        ResolveReferences();
    }

    /// <summary>Rewrites the description in <paramref name="displayLanguage"/>, e.g. after the language changed mid-game.</summary>
    public void SetLanguage(GameLanguage displayLanguage)
    {
        if(language == displayLanguage) return;
        language = displayLanguage;
        if(tile != null && descriptionText != null) descriptionText.text = tile.GetDescription(language);
    }

    /// <summary>Shows <paramref name="tileData"/>; its description is shown in <paramref name="displayLanguage"/>.</summary>
    public void Setup(Tile tileData, GameLanguage displayLanguage = GameLanguage.English)
    {
        ResolveReferences();
        tile = tileData;
        language = displayLanguage;
        transform.name = tileData.tileName;
        IsMatched = false;
        IsRevealed = false;
        IsAnimating = false;

        // The frame keeps its own teal: colouring it per design would show which
        // face-down tiles pair up. The tile's colour goes to the hologram panel instead.
        bodyBaseColor = ReadBaseColor(bodyRenderer, Color.white);
        logoBaseColor = ReadBaseColor(logoRenderer, Color.white);

        if(logoRenderer != null && tileData.backLogo != null)
        {
            ApplyLogo(tileData.backLogo);
        }

        ApplyScreenHue(tileData.tileColour);

        if(descriptionText != null) descriptionText.text = tileData.GetDescription(language);

        ApplyTint(0f, Color.white);
        SetPresentationFace(false);
    }

    /// <summary>Shows the sprite's own rect on the logo quad, scaled to keep its aspect ratio inside <see cref="logoBounds"/>.</summary>
    private void ApplyLogo(Sprite logo)
    {
        Texture2D texture = logo.texture;
        Rect rect = logo.textureRect;
        logoBlock.SetTexture(BaseMapId, texture);
        logoBlock.SetVector(BaseMapStId, new Vector4(
            rect.width / texture.width, rect.height / texture.height,
            rect.x / texture.width, rect.y / texture.height));

        float aspect = rect.height > 0f ? rect.width / rect.height : 1f;
        Vector2 bounds = new Vector2(Mathf.Max(0.01f, logoBounds.x), Mathf.Max(0.01f, logoBounds.y));
        Vector2 size = aspect >= bounds.x / bounds.y
            ? new Vector2(bounds.x, bounds.x / aspect)
            : new Vector2(bounds.y * aspect, bounds.y);

        logoRenderer.transform.localScale = new Vector3(
            logoBaseLocalScale.x * size.x, logoBaseLocalScale.y * size.y, logoBaseLocalScale.z);
    }

    /// <summary>Tints the hologram panel with a washed-out version of the tile's colour.</summary>
    private void ApplyScreenHue(Color tileColour)
    {
        if(screenRenderer == null) return;
        Color hue = Color.Lerp(Color.white, tileColour, Mathf.Clamp01(screenHueStrength));
        hue.a = 1f;
        screenBlock.SetColor(HueId, hue);
        screenRenderer.SetPropertyBlock(screenBlock);
    }

    /// <summary>Stores the generated board pose. Call after placement and sizing.</summary>
    public void CaptureBoardPose()
    {
        boardLocalPosition = transform.localPosition;
        boardLocalRotation = transform.localRotation;
        boardLocalScale = transform.localScale;

        Transform visual = Visual;
        visualBaseLocalPosition = visual == transform ? boardLocalPosition : visual.localPosition;
        visualBaseLocalRotation = visual == transform ? boardLocalRotation : visual.localRotation;

        // Flip about the board's vertical axis and shake along its horizontal axis,
        // both expressed in the visual root's parent space.
        Transform board = transform.parent;
        Vector3 boardUp = board != null ? board.up : Vector3.up;
        Vector3 boardRight = board != null ? board.right : Vector3.right;
        Transform visualParent = visual.parent;
        Quaternion toParent = visualParent != null ? Quaternion.Inverse(visualParent.rotation) : Quaternion.identity;
        flipAxis = (toParent * boardUp).normalized;
        shakeDirection = boardRight.normalized;
        poseCaptured = true;
        flipAngle = 0f;
    }

    // ---- Reveal / conceal ------------------------------------------------------------------

    public void SetRevealed(bool revealed) => IsRevealed = revealed;

    /// <summary>Applies a flip angle in degrees: 0 = face-down, 180 = logo towards the camera.</summary>
    public void SetFlipAngle(float degrees)
    {
        if(!poseCaptured) return;
        flipAngle = degrees;
        Visual.localRotation = Quaternion.AngleAxis(degrees, flipAxis) * visualBaseLocalRotation;
    }

    // ---- Incorrect feedback ----------------------------------------------------------------

    /// <param name="worldOffset">Shake offset in world units along the board's horizontal axis.</param>
    public void SetMismatchFeedback(float worldOffset, float tintWeight, Color tint)
    {
        if(!poseCaptured) return;
        Transform visual = Visual;
        Vector3 world = shakeDirection * worldOffset;
        Vector3 local = visual.parent != null ? visual.parent.InverseTransformVector(world) : world;
        visual.localPosition = visualBaseLocalPosition + local;
        ApplyTint(tintWeight, tint);
    }

    public void ClearMismatchFeedback()
    {
        if(!poseCaptured) return;
        Visual.localPosition = visualBaseLocalPosition;
        ApplyTint(0f, Color.white);
    }

    // ---- Correct-match presentation --------------------------------------------------------

    /// <summary>Captures the current pose as the start of a presentation or return movement.</summary>
    public void BeginSequenceSegment()
    {
        sequenceStartPosition = transform.position;
        sequenceStartScale = transform.localScale;
        sequenceStartAngle = flipAngle;
    }

    /// <summary>
    /// Swaps the face-down logo for one half of the description. The left tile of a pair shows the
    /// left half and the right tile the right half, so the two tiles placed edge to edge read as one text.
    /// </summary>
    public void SetPresentationFace(bool visible, bool leftHalf = true)
    {
        if(faceDownRenderers != null)
        {
            foreach(Renderer faceDown in faceDownRenderers) if(faceDown != null) faceDown.enabled = !visible;
        }

        SetSeamClip(visible, leftHalf);

        if(descriptionText == null) return;
        if(visible) ConfigureDescriptionHalf(leftHalf);
        descriptionText.gameObject.SetActive(visible);
        if(visible) descriptionText.ForceMeshUpdate();
    }

    /// <summary>
    /// Swaps the bezel meshes for their seam-clipped variants, removing the bezel on the
    /// side where this tile meets its partner. With both tiles clipped and joined at
    /// <see cref="SeamJoinWidth"/>, their remaining frame runs are collinear and touch,
    /// so the pair reads as one wide panel instead of two tiles side by side.
    /// </summary>
    private void SetSeamClip(bool clipped, bool leftHalf)
    {
        if(seamClipParts == null || seamClipParts.Length == 0) return;
        if(seamClipped == clipped && !clipped) return;
        seamClipped = clipped;

        foreach(SeamClipPart part in seamClipParts)
        {
            if(part == null || part.target == null) continue;
            Mesh mesh = !clipped ? part.full : (leftHalf ? part.keepLeft : part.keepRight);
            if(mesh != null) part.target.sharedMesh = mesh;
        }

        // The clipped mesh is missing the bezel on the seam side. The matching cap puts
        // that geometry back as a separate object, so it can be retracted on screen
        // while the pair closes instead of vanishing the moment the match is made.
        seamLeftHalf = leftHalf;
        Transform inner = leftHalf ? seamCapRight : seamCapLeft;
        Transform outer = leftHalf ? seamCapLeft : seamCapRight;
        if(outer != null) outer.gameObject.SetActive(false);
        if(inner != null)
        {
            inner.gameObject.SetActive(clipped);
            inner.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Retracts the bezel on the seam side: 0 leaves it whole, 1 collapses it into the cut
    /// plane. Driven by the join so the bezel is exactly gone as the two tiles meet, and
    /// run backwards on the way out so it grows again as they separate.
    /// </summary>
    public void SetSeamRetract(float retract)
    {
        Transform inner = seamLeftHalf ? seamCapRight : seamCapLeft;
        if(inner == null || !inner.gameObject.activeSelf) return;
        inner.localScale = new Vector3(Mathf.Clamp01(1f - retract), 1f, 1f);
    }

    /// <summary>
    /// Distance between the centres of a joined pair, in tile units. Smaller than one tile
    /// because each tile gives up the bezel on its inner side.
    /// </summary>
    public float SeamJoinWidth => seamClipParts != null && seamClipParts.Length > 0 ? seamHalfWidth * 2f : 1f;

    private void ConfigureDescriptionHalf(bool leftHalf)
    {
        RectTransform rect = descriptionText.rectTransform;
        // The readable strip is the joined pair minus the two bezels they gave up, so the
        // text box always spans exactly what the player can see.
        rect.sizeDelta = new Vector2(SeamJoinWidth * 2f, rect.sizeDelta.y);
        float width = rect.rect.width;
        float height = rect.rect.height;
        float faceWidth = width * 0.5f;

        // Shift the double-width text so the seam between its halves lies on this tile's inner edge.
        Vector3 position = descriptionBaseLocalPosition;
        position.x += leftHalf ? faceWidth * 0.5f : -faceWidth * 0.5f;
        rect.localPosition = position;

        // Clip in the text's local space to the half that lies over this tile's face.
        if(descriptionMaterial == null)
        {
            descriptionMaterial = descriptionText.fontMaterial;
            descriptionMaterial.EnableKeyword(ClipRectKeyword);
        }

        float pivotOffsetX = (0.5f - rect.pivot.x) * width;
        float pivotOffsetY = (0.5f - rect.pivot.y) * height;
        // Each half reaches slightly past the seam so the anti-aliased clip edges overlap instead of leaving a gap.
        float seamOverlap = faceWidth * SeamOverlapFraction;
        Vector4 clip = leftHalf
            ? new Vector4(-faceWidth, -height * 0.5f, seamOverlap, height * 0.5f)
            : new Vector4(-seamOverlap, -height * 0.5f, faceWidth, height * 0.5f);
        clip += new Vector4(pivotOffsetX, pivotOffsetY, pivotOffsetX, pivotOffsetY);
        descriptionMaterial.SetVector(ClipRectId, clip);
    }

    private void OnDestroy()
    {
        if(descriptionMaterial != null) Destroy(descriptionMaterial);
    }

    /// <summary>
    /// Moves from the captured segment start towards <paramref name="targetWorld"/>, scaling up and spinning
    /// so the presentation face (on the face-down side) ends towards the camera.
    /// </summary>
    public void SetPresentationProgress(float t, Vector3 targetWorld, float scaleMultiplier, int spins)
    {
        transform.position = Vector3.LerpUnclamped(sequenceStartPosition, targetWorld, t);
        transform.localScale = Vector3.LerpUnclamped(sequenceStartScale, boardLocalScale * scaleMultiplier, t);
        float endAngle = 360f * Mathf.Max(1, spins);
        SetFlipAngle(Mathf.LerpUnclamped(sequenceStartAngle, endAngle, t));
    }

    public void SetPresentationPosition(Vector3 world)
    {
        transform.position = world;
    }

    /// <summary>Moves from the captured segment start back to the generated board pose, ending logo-up.</summary>
    public void SetReturnProgress(float t)
    {
        Transform board = transform.parent;
        Vector3 boardWorld = board != null ? board.TransformPoint(boardLocalPosition) : boardLocalPosition;
        transform.position = Vector3.LerpUnclamped(sequenceStartPosition, boardWorld, t);
        transform.localScale = Vector3.LerpUnclamped(sequenceStartScale, boardLocalScale, t);
        float startAngle = Mathf.Repeat(sequenceStartAngle, 360f);
        SetFlipAngle(Mathf.LerpUnclamped(startAngle, RevealedAngle, t));
    }

    // ---- Final / reset states --------------------------------------------------------------

    /// <summary>Snaps to the exact board pose, logo-up, and makes the tile permanently unselectable.</summary>
    public void MarkMatched()
    {
        IsMatched = true;
        IsRevealed = true;
        IsAnimating = false;
        RestoreBoardPose(RevealedAngle);
    }

    /// <summary>
    /// Restores a consistent visual state after cancellation: matched tiles logo-up, all others face-down,
    /// original colours, exact board pose.
    /// </summary>
    public void ResetVisualState()
    {
        IsAnimating = false;
        IsRevealed = IsMatched;
        RestoreBoardPose(IsMatched ? RevealedAngle : 0f);
    }

    private void RestoreBoardPose(float angle)
    {
        if(!poseCaptured) return;
        transform.localPosition = boardLocalPosition;
        transform.localRotation = boardLocalRotation;
        transform.localScale = boardLocalScale;
        if(Visual != transform) Visual.localPosition = visualBaseLocalPosition;
        SetFlipAngle(angle);
        ApplyTint(0f, Color.white);
        SetPresentationFace(false);
    }

    // ---- Internals -------------------------------------------------------------------------

    private void ApplyTint(float weight, Color tint)
    {
        if(bodyRenderer != null)
        {
            bodyBlock.SetColor(BaseColorId, Color.Lerp(bodyBaseColor, tint, weight));
            bodyRenderer.SetPropertyBlock(bodyBlock);
        }

        if(logoRenderer != null)
        {
            logoBlock.SetColor(BaseColorId, Color.Lerp(logoBaseColor, logoBaseColor * tint, weight));
            logoRenderer.SetPropertyBlock(logoBlock);
        }
    }

    private static Color ReadBaseColor(Renderer target, Color fallback)
    {
        if(target == null) return fallback;
        Material shared = target.sharedMaterial;
        return shared != null && shared.HasProperty(BaseColorId) ? shared.GetColor(BaseColorId) : fallback;
    }

    /// <summary>
    /// Uses the serialized references. Missing ones are resolved once by the legacy child names and
    /// cached, so the tile keeps working until the prefab is re-wired in Prefab Mode.
    /// </summary>
    private void ResolveReferences()
    {
        if(referencesResolved) return;
        referencesResolved = true;
        bodyBlock = new MaterialPropertyBlock();
        logoBlock = new MaterialPropertyBlock();
        screenBlock = new MaterialPropertyBlock();

        bool usedFallback = false;

        if(bodyRenderer == null)
        {
            bodyRenderer = FindRenderer(Constants.GameObjectNames.TileBody);
            usedFallback |= bodyRenderer != null;
        }

        if(logoRenderer == null)
        {
            logoRenderer = FindRenderer(Constants.GameObjectNames.TileBackFace);
            usedFallback |= logoRenderer != null;
        }

        if(faceDownRenderers == null || faceDownRenderers.Length == 0)
        {
            faceDownRenderers = FindRenderers(Constants.GameObjectNames.TileFrontFaceLogo);
            usedFallback |= faceDownRenderers.Length > 0;
        }

        if(descriptionText == null)
        {
            Transform child = FindDeepChild(transform, Constants.GameObjectNames.TileDescriptionText);
            descriptionText = child != null ? child.GetComponent<TMP_Text>() : null;
            usedFallback |= descriptionText != null;
        }

        // Caps only exist while a pair is joined; the full mesh covers that region otherwise.
        if(seamCapLeft != null) seamCapLeft.gameObject.SetActive(false);
        if(seamCapRight != null) seamCapRight.gameObject.SetActive(false);

        if(logoRenderer != null) logoBaseLocalScale = logoRenderer.transform.localScale;
        if(descriptionText != null) descriptionBaseLocalPosition = descriptionText.rectTransform.localPosition;

        if(bodyRenderer == null || logoRenderer == null)
        {
            Debug.LogError($"{name}: TileController is missing its body or logo renderer.", this);
        }
        else if(usedFallback)
        {
            Debug.LogWarning($"{name}: TileController references were resolved by child name. Assign them in Prefab Mode.", this);
        }
    }

    private Renderer FindRenderer(string childName)
    {
        Transform child = FindDeepChild(transform, childName);
        return child != null ? child.GetComponent<Renderer>() : null;
    }

    private Renderer[] FindRenderers(params string[] childNames)
    {
        System.Collections.Generic.List<Renderer> found = new System.Collections.Generic.List<Renderer>();
        foreach(string childName in childNames)
        {
            Renderer r = FindRenderer(childName);
            if(r != null) found.Add(r);
        }
        return found.ToArray();
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach(Transform child in parent)
        {
            if(child.name == childName) return child;
            Transform nested = FindDeepChild(child, childName);
            if(nested != null) return nested;
        }
        return null;
    }
}
