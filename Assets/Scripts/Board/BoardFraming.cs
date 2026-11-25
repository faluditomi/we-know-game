using UnityEngine;

/// <summary>
/// Pure camera-framing math. Fits a front-facing board into the safe area of the
/// viewport for either a perspective or an orthographic camera.
/// </summary>
public static class BoardFraming
{
    public struct Result
    {
        /// <summary>Perspective: distance from the board plane. Orthographic: unused (0).</summary>
        public float Distance;

        /// <summary>Orthographic: required orthographic size. Perspective: unused (0).</summary>
        public float OrthographicSize;

        /// <summary>Offset in camera right/up units at the board plane that centres the board in the safe area.</summary>
        public Vector2 LateralOffset;
    }

    /// <param name="boardSize">Board width/height in world units, including padding.</param>
    /// <param name="aspect">Viewport width / height.</param>
    /// <param name="verticalFieldOfView">Degrees. Ignored when <paramref name="orthographic"/> is true.</param>
    /// <param name="safeArea">Safe area in normalised viewport coordinates (0..1).</param>
    public static Result Fit(Vector2 boardSize, float aspect, float verticalFieldOfView, bool orthographic, Rect safeArea)
    {
        Result result = default;
        if(aspect <= 0f || boardSize.x <= 0f || boardSize.y <= 0f) return result;

        float safeWidth = Mathf.Clamp(safeArea.width, 0.05f, 1f);
        float safeHeight = Mathf.Clamp(safeArea.height, 0.05f, 1f);

        // Required visible half-height of the whole viewport so that the board fits in the safe rect.
        float halfHeightForHeight = boardSize.y * 0.5f / safeHeight;
        float halfHeightForWidth = boardSize.x * 0.5f / (safeWidth * aspect);
        float visibleHalfHeight = Mathf.Max(halfHeightForHeight, halfHeightForWidth);

        if(orthographic)
        {
            result.OrthographicSize = visibleHalfHeight;
        }
        else
        {
            float halfFov = Mathf.Clamp(verticalFieldOfView, 1f, 179f) * 0.5f * Mathf.Deg2Rad;
            result.Distance = visibleHalfHeight / Mathf.Tan(halfFov);
        }

        // Shift the camera so the board centre lands on the safe-area centre.
        float visibleHalfWidth = visibleHalfHeight * aspect;
        Vector2 safeCentre = safeArea.center;
        result.LateralOffset = new Vector2(
            -(safeCentre.x - 0.5f) * 2f * visibleHalfWidth,
            -(safeCentre.y - 0.5f) * 2f * visibleHalfHeight);
        return result;
    }

    /// <summary>World height visible at <paramref name="depth"/> from the camera.</summary>
    public static float VisibleHeightAtDepth(float depth, float verticalFieldOfView, bool orthographic, float orthographicSize)
    {
        if(orthographic) return orthographicSize * 2f;
        float halfFov = Mathf.Clamp(verticalFieldOfView, 1f, 179f) * 0.5f * Mathf.Deg2Rad;
        return 2f * Mathf.Max(0f, depth) * Mathf.Tan(halfFov);
    }

    /// <summary>
    /// Smallest presentation depth at which a tile of the given world half-extent cannot cross the near plane.
    /// </summary>
    public static float SafePresentationDepth(float requestedDepth, float nearClipPlane, float tileHalfExtent)
    {
        return Mathf.Max(requestedDepth, nearClipPlane + Mathf.Max(0f, tileHalfExtent) + 0.05f);
    }

    /// <summary>
    /// Deepest presentation depth at which a tile spinning in place stays well in front of the board.
    /// A spinning tile swings its half-extent behind its centre, and that half-extent grows with depth
    /// because presented tiles keep a fixed on-screen size: <paramref name="halfExtentPerDepth"/> is that
    /// ratio. The swept volume ends at <paramref name="clearance"/> of the board distance, leaving room
    /// behind it for the dimmer.
    /// </summary>
    public static float SpinClearDepth(float requestedDepth, float boardDistance, float halfExtentPerDepth, float clearance = 0.85f)
    {
        if(boardDistance <= 0f) return requestedDepth;
        return Mathf.Min(requestedDepth, boardDistance * clearance / (1f + Mathf.Max(0f, halfExtentPerDepth)));
    }
}
