using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Live menu background: a tilted wall of real tiles that drifts slowly while random tiles flip
/// to show their logo and flip back. Plays a travelling flip wave on request (success title).
/// Built from the tile prefab and data the <see cref="GameManager"/> already loaded, on a
/// non-raycastable layer, and shown only while a menu is visible. The background seen through the
/// gaps between tiles is the scene-wide <see cref="NeonBackdrop"/>, not a plane of this wall's own.
/// </summary>
[DisallowMultipleComponent]
public sealed class TileWallBackground : MonoBehaviour
{
    private sealed class WallTile
    {
        public TileController Controller;
        public Transform Transform;
        public int Column;
        public int Row;
        public float Angle;
        public float Target;
        public float HoldUntil;
    }

    [Header("Placement")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Decorative stand-in for the gameplay tile: the same look on simplified meshes with the static parts merged into one renderer. The wall spawns hundreds of tiles, so this is what keeps it affordable. Leave empty to spawn the gameplay prefab instead.")]
    [SerializeField] private GameObject wallTilePrefab;
    [Tooltip("Minimum wall size. The wall grows beyond this to cover the whole view at the current aspect ratio.")]
    [Min(1)] [SerializeField] private int columns = 13;
    [Min(1)] [SerializeField] private int rows = 9;
    [Tooltip("Extra tiles kept beyond the visible edge, so drifting tiles wrap around off-screen.")]
    [Min(0f)] [SerializeField] private float coverageMargin = 1f;
    [Tooltip("Upper bound on the wall size, for extreme aspect ratios.")]
    [Min(1)] [SerializeField] private int maxColumns = 40;
    [Min(1)] [SerializeField] private int maxRows = 28;
    [Min(0f)] [SerializeField] private float spacing = 0.14f;
    [Min(0.1f)] [SerializeField] private float tileScale = 1.3f;
    [Tooltip("Distance from the camera to the wall centre. Keep it beyond the match presentation depth.")]
    [Min(1f)] [SerializeField] private float distance = 9f;
    [Tooltip("Viewport point (0–1) the wall centre sits on. The menu column covers the left side.")]
    [SerializeField] private Vector2 viewportAnchor = new Vector2(0.58f, 0.5f);
    [Tooltip("Wall rotation relative to the camera, so it recedes in perspective.")]
    [SerializeField] private Vector3 tiltEuler = new Vector3(15f, 12f, 7f);

    [Header("Motion")]
    [Tooltip("Drift in wall units per second along the wall's right/up axes.")]
    [SerializeField] private Vector2 driftVelocity = new Vector2(0.18f, 0.07f);
    [Min(1f)] [SerializeField] private float flipDegreesPerSecond = 450f;
    [Tooltip("Share of the wall showing a logo at any moment while idle. Raise it for more tiles opening and closing.")]
    [Range(0f, 0.6f)] [SerializeField] private float idleRevealedShare = 0.12f;
    [Tooltip("Seconds between starting one flip and the next, while fewer tiles than the share above are open.")]
    [SerializeField] private Vector2 idleFlipInterval = new Vector2(0.03f, 0.12f);
    [Tooltip("Seconds a revealed tile shows its logo before flipping back.")]
    [SerializeField] private Vector2 revealHold = new Vector2(1.2f, 3f);

    [Header("Celebration wave")]
    [Tooltip("Screen point the waves start from, in viewport units: (0, 0) is the bottom-left corner, (1, 1) the top-right.")]
    [SerializeField] private Vector2 waveOrigin = Vector2.zero;
    [Tooltip("Seconds a wave takes to spread from the origin corner to the opposite corner of the screen.")]
    [Min(0.1f)] [SerializeField] private float waveSweepSeconds = 1.5f;
    [Tooltip("Seconds between one wave and the next.")]
    [Min(0.1f)] [SerializeField] private float wavePeriod = 1.6f;
    [Range(0.1f, 0.9f)] [SerializeField] private float waveRevealPortion = 0.5f;

    private readonly List<WallTile> tiles = new List<WallTile>();
    private Transform wallRoot;
    private Vector2 drift;
    private float nextIdleFlip;
    private float celebrateStart;
    private float celebrateUntil = -1f;
    private bool visible = true;
    private GameObject sourcePrefab;
    private IReadOnlyList<Tile> sourceDesigns;
    private int gridColumns;
    private int gridRows;
    private Vector2 gridCentre;
    private Vector2Int checkedScreenSize;

    public bool IsBuilt => tiles.Count > 0;
    public Vector2Int GridSize => new Vector2Int(gridColumns, gridRows);

    private float Step => 1f + spacing;
    private float Now => Time.unscaledTime;
    private bool Celebrating => Now < celebrateUntil;

    /// <summary>Spawns the wall from the loaded tile prefab and designs, sized to cover the view. Safe to call once.</summary>
    public void Build(GameObject tilePrefab, IReadOnlyList<Tile> designs)
    {
        if(IsBuilt || tilePrefab == null || designs == null || designs.Count == 0) return;
        if(targetCamera == null) targetCamera = Camera.main;

        sourcePrefab = wallTilePrefab != null ? wallTilePrefab : tilePrefab;
        sourceDesigns = designs;
        Spawn(RequiredArea());
    }

    /// <summary>(Re)creates the wall so it covers <paramref name="area"/> (wall-local units).</summary>
    private void Spawn(Rect area)
    {
        if(wallRoot != null)
        {
            Destroy(wallRoot.gameObject);
            tiles.Clear();
        }

        gridColumns = Mathf.Clamp(Mathf.CeilToInt(area.width / Step), columns, Mathf.Max(columns, maxColumns));
        gridRows = Mathf.Clamp(Mathf.CeilToInt(area.height / Step), rows, Mathf.Max(rows, maxRows));
        gridCentre = area.center;
        checkedScreenSize = new Vector2Int(Screen.width, Screen.height);

        wallRoot = new GameObject("Tile Wall").transform;
        wallRoot.SetParent(transform, false);
        wallRoot.localScale = Vector3.one * tileScale;
        int wallLayer = gameObject.layer;

        for(int row = 0; row < gridRows; row++)
        {
            for(int column = 0; column < gridColumns; column++)
            {
                GameObject tileObject = Instantiate(sourcePrefab, wallRoot, false);
                PrepareDecorativeTile(tileObject, wallLayer);
                TileController controller = tileObject.GetComponent<TileController>();
                if(controller == null)
                {
                    Destroy(tileObject);
                    continue;
                }

                controller.Setup(sourceDesigns[Random.Range(0, sourceDesigns.Count)]);
                tileObject.transform.localPosition = GridPosition(column, row, Vector2.zero);
                controller.CaptureBoardPose();

                WallTile tile = new WallTile { Controller = controller, Transform = tileObject.transform, Column = column, Row = row };
                if(Random.value < idleRevealedShare)
                {
                    tile.Angle = tile.Target = 180f;
                    tile.HoldUntil = Now + Random.Range(0f, revealHold.y);
                }
                controller.SetFlipAngle(tile.Angle);
                tiles.Add(tile);
            }
        }

        ScheduleIdleFlip();
        SetVisible(visible);
    }

    /// <summary>Shows or hides the wall. Hidden walls do not animate.</summary>
    public void SetVisible(bool show)
    {
        visible = show;
        if(wallRoot == null) return;
        if(show) EnsureCoverage();
        wallRoot.gameObject.SetActive(show);
        if(show) PlaceInFrontOfCamera();
    }

    /// <summary>Rebuilds the wall when the view (aspect ratio) now shows an area the current tiles do not cover.</summary>
    private void EnsureCoverage()
    {
        checkedScreenSize = new Vector2Int(Screen.width, Screen.height);
        Rect needed = RequiredArea();
        Rect covered = new Rect(gridCentre - new Vector2(gridColumns, gridRows) * Step * 0.5f, new Vector2(gridColumns, gridRows) * Step);
        if(covered.Contains(needed.min) && covered.Contains(needed.max)) return;
        Spawn(needed);
        PlaceInFrontOfCamera();
    }

    /// <summary>
    /// Wall-local area (in unscaled tile units) the view can see, plus <see cref="coverageMargin"/> tiles on
    /// every side, found by casting viewport rays onto the wall plane. The area is usually lopsided because
    /// the tilted wall recedes, so the grid is centred on it rather than on the wall origin.
    /// </summary>
    private Rect RequiredArea()
    {
        float margin = coverageMargin * Step;
        Rect fallback = new Rect(-columns * Step * 0.5f, -rows * Step * 0.5f, columns * Step, rows * Step);
        if(targetCamera == null) return fallback;

        GetWallPose(out Vector3 centre, out Quaternion rotation);
        Plane plane = new Plane(rotation * Vector3.forward, centre);
        Quaternion toWall = Quaternion.Inverse(rotation);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        const int samples = 4;
        for(int i = 0; i <= samples; i++)
        {
            for(int j = 0; j <= samples; j++)
            {
                Ray ray = targetCamera.ViewportPointToRay(new Vector3(i / (float)samples, j / (float)samples, 0f));
                // A view ray that never reaches the wall: cover as much as allowed.
                if(!plane.Raycast(ray, out float enter)) return new Rect(-maxColumns * Step * 0.5f, -maxRows * Step * 0.5f, maxColumns * Step, maxRows * Step);
                Vector3 local = toWall * (ray.GetPoint(enter) - centre) / tileScale;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
        }

        return Rect.MinMaxRect(min.x - margin, min.y - margin, max.x + margin, max.y + margin);
    }

    /// <summary>Plays a travelling flip wave across the wall for <paramref name="duration"/> seconds.</summary>
    public void Celebrate(float duration)
    {
        celebrateStart = Now;
        celebrateUntil = Now + Mathf.Max(0f, duration);
    }

    private void Update()
    {
        if(!visible || !IsBuilt) return;

        // A rotation or window resize can reveal more of the plane than the wall covers.
        if(Screen.width != checkedScreenSize.x || Screen.height != checkedScreenSize.y)
        {
            EnsureCoverage();
            PlaceInFrontOfCamera();
        }

        float dt = Time.unscaledDeltaTime;
        drift += driftVelocity * dt;
        Vector2 extent = new Vector2(gridColumns * Step, gridRows * Step);
        drift = new Vector2(Mathf.Repeat(drift.x, extent.x), Mathf.Repeat(drift.y, extent.y));

        bool celebrating = Celebrating;
        if(!celebrating && celebrateUntil > 0f)
        {
            // Wave just ended: everything flips back face down, then idle flips resume.
            celebrateUntil = -1f;
            foreach(WallTile tile in tiles) tile.Target = 0f;
            ScheduleIdleFlip();
        }

        if(!celebrating && Now >= nextIdleFlip)
        {
            int open = 0;
            foreach(WallTile tile in tiles) if(tile.Target > 0f) open++;
            if(open < Mathf.RoundToInt(tiles.Count * idleRevealedShare)) StartIdleFlip();
            else ScheduleIdleFlip();
        }

        float maxStep = flipDegreesPerSecond * dt;
        foreach(WallTile tile in tiles)
        {
            Vector3 position = GridPosition(tile.Column, tile.Row, drift);
            tile.Transform.localPosition = position;

            if(celebrating)
            {
                float phase = (Now - celebrateStart) - WaveDistance(tile.Transform.position) * waveSweepSeconds;
                tile.Target = phase >= 0f && Mathf.Repeat(phase, wavePeriod) < wavePeriod * waveRevealPortion ? 180f : 0f;
            }
            else if(tile.Target > 0f && Now >= tile.HoldUntil)
            {
                tile.Target = 0f;
            }

            if(!Mathf.Approximately(tile.Angle, tile.Target))
            {
                tile.Angle = Mathf.MoveTowards(tile.Angle, tile.Target, maxStep);
                tile.Controller.SetFlipAngle(tile.Angle);
            }
        }
    }

    private void StartIdleFlip()
    {
        ScheduleIdleFlip();
        for(int attempt = 0; attempt < 8; attempt++)
        {
            WallTile tile = tiles[Random.Range(0, tiles.Count)];
            if(tile.Target > 0f || tile.Angle > 0f) continue;
            tile.Target = 180f;
            tile.HoldUntil = Now + 180f / flipDegreesPerSecond + Random.Range(revealHold.x, revealHold.y);
            return;
        }
    }

    private void ScheduleIdleFlip()
    {
        nextIdleFlip = Now + Random.Range(idleFlipInterval.x, idleFlipInterval.y);
    }

    /// <summary>
    /// On-screen distance of a tile from <see cref="waveOrigin"/>, where 1 is the screen's diagonal, so waves
    /// spread outward from that screen corner. Uses the tile's current screen position rather than its grid index,
    /// because drifting tiles wrap around and index order no longer matches where they are.
    /// </summary>
    private float WaveDistance(Vector3 worldPosition)
    {
        if(targetCamera == null) return 0f;
        Vector3 viewport = targetCamera.WorldToViewportPoint(worldPosition);
        Vector2 aspect = new Vector2(Mathf.Max(0.01f, targetCamera.aspect), 1f);
        Vector2 offset = Vector2.Scale(new Vector2(viewport.x, viewport.y) - waveOrigin, aspect);
        return offset.magnitude / aspect.magnitude;
    }

    /// <summary>Wall-local tile position with drift applied, wrapped so the wall never runs out of tiles.</summary>
    private Vector3 GridPosition(int column, int row, Vector2 offset)
    {
        float width = gridColumns * Step;
        float height = gridRows * Step;
        float x = Mathf.Repeat(column * Step + offset.x, width) - width * 0.5f + Step * 0.5f;
        float y = Mathf.Repeat(row * Step + offset.y, height) - height * 0.5f + Step * 0.5f;
        return new Vector3(gridCentre.x + x, gridCentre.y + y, 0f);
    }

    private void PlaceInFrontOfCamera()
    {
        if(targetCamera == null) targetCamera = Camera.main;
        if(targetCamera == null || wallRoot == null) return;
        GetWallPose(out Vector3 position, out Quaternion rotation);
        wallRoot.SetPositionAndRotation(position, rotation);
    }

    /// <summary>Where the wall centre sits and how it is turned, relative to the camera's current pose.</summary>
    private void GetWallPose(out Vector3 position, out Quaternion rotation)
    {
        position = targetCamera.ViewportToWorldPoint(new Vector3(viewportAnchor.x, viewportAnchor.y, distance));
        rotation = targetCamera.transform.rotation * Quaternion.Euler(tiltEuler);
    }

    /// <summary>
    /// Strips gameplay-only parts from a wall tile: colliders, shadows, the tile layer, and the side
    /// quads (they only hide tile colours from players during a game) to keep the wall cheap.
    /// </summary>
    private static void PrepareDecorativeTile(GameObject tileObject, int layer)
    {
        foreach(Collider collider in tileObject.GetComponentsInChildren<Collider>(true)) Destroy(collider);
        foreach(Transform child in tileObject.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
        foreach(Renderer renderer in tileObject.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
