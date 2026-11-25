using UnityEngine;

/// <summary>
/// Draws the game's background: one camera-facing quad, far behind everything else, filled by the
/// procedural <c>WeKnow/Neon Backdrop</c> shader. The quad is a runtime child of the camera that is
/// never saved into the scene, resized whenever the view changes, so the same background serves the
/// board and every menu screen at any resolution or aspect ratio.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class NeonBackdrop : MonoBehaviour
{
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private const string QuadName = "Neon Backdrop";

    [Tooltip("Camera the backdrop is anchored to. Falls back to this object's camera, then the main camera.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Material using the WeKnow/Neon Backdrop shader. The component renders an instance of it, so the asset is never modified.")]
    [SerializeField] private Material backdropMaterial;
    [Tooltip("Distance from the camera to the backdrop. Keep it beyond the menu tile wall and the match presentation, and inside the far clip plane.")]
    [Min(1f)] [SerializeField] private float distance = 60f;
    [Tooltip("How far the quad reaches past the view frustum, as a fraction. A little margin hides the edge while the view resizes.")]
    [Min(1f)] [SerializeField] private float coverage = 1.04f;
    [Tooltip("Layer the backdrop is drawn on. 'Ignore Raycast' keeps it out of tile picking.")]
    [SerializeField] private int layer = 2;

    private Transform quad;
    private Material instance;
    private Vector2Int checkedScreenSize;
    private float checkedFieldOfView;
    private float checkedDistance;
    private float checkedCoverage;

    private Camera View
    {
        get
        {
            if(targetCamera != null) return targetCamera;
            targetCamera = GetComponent<Camera>();
            if(targetCamera == null) targetCamera = UnityEngine.Camera.main;
            return targetCamera;
        }
    }

    private void OnEnable()
    {
        EnsureQuad();
        Fit(true);
    }

    private void OnDisable()
    {
        Teardown();
    }

    private void LateUpdate()
    {
        if(quad == null) EnsureQuad();
        Fit(false);
    }

    private void OnValidate()
    {
        if(isActiveAndEnabled && quad != null) Fit(true);
    }

    private void EnsureQuad()
    {
        if(quad != null || backdropMaterial == null) return;
        Camera camera = View;
        if(camera == null) return;

        GameObject created = GameObject.CreatePrimitive(PrimitiveType.Quad);
        created.name = QuadName;
        // Runtime-only: the backdrop is rebuilt on enable instead of being serialized into the scene.
        created.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        created.layer = layer;

        Collider collider = created.GetComponent<Collider>();
        if(collider != null) Discard(collider);

        quad = created.transform;
        quad.SetParent(camera.transform, false);
        quad.localRotation = Quaternion.identity;

        instance = new Material(backdropMaterial);
        instance.hideFlags = HideFlags.DontSave;

        Renderer renderer = created.GetComponent<Renderer>();
        renderer.sharedMaterial = instance;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    /// <summary>Sizes the quad to cover the frustum at <see cref="distance"/>, and tells the shader the view's aspect.</summary>
    private void Fit(bool force)
    {
        if(quad == null || instance == null) return;
        Camera camera = View;
        if(camera == null) return;

        Vector2Int screen = new Vector2Int(Mathf.Max(1, camera.pixelWidth), Mathf.Max(1, camera.pixelHeight));
        if(!force
            && screen == checkedScreenSize
            && Mathf.Approximately(checkedFieldOfView, camera.fieldOfView)
            && Mathf.Approximately(checkedDistance, distance)
            && Mathf.Approximately(checkedCoverage, coverage)) return;

        checkedScreenSize = screen;
        checkedFieldOfView = camera.fieldOfView;
        checkedDistance = distance;
        checkedCoverage = coverage;

        float aspect = camera.aspect > 0f ? camera.aspect : screen.x / (float)screen.y;
        float far = camera.farClipPlane * 0.98f;
        float depth = Mathf.Clamp(distance, camera.nearClipPlane + 0.01f, far);
        float height = camera.orthographic
            ? camera.orthographicSize * 2f
            : 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        quad.localPosition = new Vector3(0f, 0f, depth);
        quad.localRotation = Quaternion.identity;
        quad.localScale = new Vector3(height * aspect * coverage, height * coverage, 1f);
        instance.SetFloat(AspectId, aspect);
    }

    private void Teardown()
    {
        if(quad != null) Discard(quad.gameObject);
        if(instance != null) Discard(instance);
        quad = null;
        instance = null;
    }

    private static void Discard(Object target)
    {
        if(target == null) return;
        if(Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
