using UnityEngine;

/// <summary>
/// Owns the visual size of a tile instance. Scaling the tile root keeps every
/// descendant (including descendants added after this component is configured)
/// in the same proportion as the tile.
/// </summary>
[DisallowMultipleComponent]
public sealed class TileView : MonoBehaviour
{
    [Tooltip("The tile's width and height before TileView applies a size. Set these to the prefab's unscaled dimensions in its board parent's local X/Y plane.")]
    public Vector2 baselineSize = Vector2.one;
    private Vector3 baselineLocalScale;
    private int widthScaleAxis;
    private int heightScaleAxis;
    private bool baselineCaptured;

    private void Awake()
    {
        CaptureBaseline();
    }

    /// <summary>
    /// Resizes this tile to the requested board-local width and height.
    /// The scale is always calculated from the captured prefab baseline, so
    /// repeated calls do not accumulate scale.
    /// </summary>
    public void SetSize(float width, float height)
    {
        if(!IsPositiveFinite(width) || !IsPositiveFinite(height))
        {
            Debug.LogError($"Cannot set tile size to ({width}, {height}). Both dimensions must be finite and greater than zero.");
            return;
        }

        if(!IsPositiveFinite(baselineSize.x) || !IsPositiveFinite(baselineSize.y))
        {
            Debug.LogError("TileView baseline size must contain finite values greater than zero.");
            return;
        }

        CaptureBaseline();

        Vector3 scale = baselineLocalScale;
        scale[widthScaleAxis] = baselineLocalScale[widthScaleAxis] * (width / baselineSize.x);
        scale[heightScaleAxis] = baselineLocalScale[heightScaleAxis] * (height / baselineSize.y);
        transform.localScale = scale;
    }

    private void CaptureBaseline()
    {
        if(baselineCaptured) return;
        baselineLocalScale = transform.localScale;
        ResolveSizingAxes(out widthScaleAxis, out heightScaleAxis);
        baselineCaptured = true;
    }

    private void ResolveSizingAxes(out int widthAxis, out int heightAxis)
    {
        Vector3[] localAxes = { Vector3.right, Vector3.up, Vector3.forward };
        float bestScore = float.NegativeInfinity;
        widthAxis = 0;
        heightAxis = 1;
        
        for(int candidateWidthAxis = 0; candidateWidthAxis < localAxes.Length; candidateWidthAxis++)
        {
            Vector3 widthDirectionInParent = transform.localRotation * localAxes[candidateWidthAxis];
            float widthAlignment = Mathf.Abs(Vector3.Dot(widthDirectionInParent, Vector3.right));

            for(int candidateHeightAxis = 0; candidateHeightAxis < localAxes.Length; candidateHeightAxis++)
            {
                if(candidateWidthAxis == candidateHeightAxis) continue;
                Vector3 heightDirectionInParent = transform.localRotation * localAxes[candidateHeightAxis];
                float heightAlignment = Mathf.Abs(Vector3.Dot(heightDirectionInParent, Vector3.up));
                float score = widthAlignment + heightAlignment;

                if(score > bestScore)
                {
                    bestScore = score;
                    widthAxis = candidateWidthAxis;
                    heightAxis = candidateHeightAxis;
                }
            }
        }
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
