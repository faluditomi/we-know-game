using NUnit.Framework;
using UnityEngine;

public class BoardFramingTests
{
    private static readonly Rect FullScreen = new Rect(0f, 0f, 1f, 1f);
    private const float Tolerance = 0.0001f;

    [TestCase(4f / 3f)]
    [TestCase(16f / 9f)]
    [TestCase(19.5f / 9f)]
    public void PerspectiveFitKeepsBoardInsideView(float aspect)
    {
        Vector2 board = new Vector2(5f, 5f);
        const float fov = 60f;
        BoardFraming.Result fit = BoardFraming.Fit(board, aspect, fov, false, FullScreen);

        float visibleHeight = BoardFraming.VisibleHeightAtDepth(fit.Distance, fov, false, 0f);
        float visibleWidth = visibleHeight * aspect;
        Assert.GreaterOrEqual(visibleHeight + Tolerance, board.y);
        Assert.GreaterOrEqual(visibleWidth + Tolerance, board.x);
        // Tight on at least one axis.
        Assert.IsTrue(Mathf.Abs(visibleHeight - board.y) < 0.001f || Mathf.Abs(visibleWidth - board.x) < 0.001f);
        Assert.AreEqual(Vector2.zero, fit.LateralOffset);
    }

    [Test]
    public void NarrowAspectIsWidthLimited()
    {
        Vector2 board = new Vector2(8f, 4f);
        BoardFraming.Result fit = BoardFraming.Fit(board, 1f, 60f, true, FullScreen);
        Assert.AreEqual(4f, fit.OrthographicSize, Tolerance);
    }

    [Test]
    public void SafeAreaShrinksAndRecentresBoard()
    {
        Vector2 board = new Vector2(4f, 4f);
        Rect safe = new Rect(0.1f, 0f, 0.9f, 1f); // notch on the left
        BoardFraming.Result full = BoardFraming.Fit(board, 2f, 60f, true, FullScreen);
        BoardFraming.Result inset = BoardFraming.Fit(board, 2f, 60f, true, safe);

        Assert.GreaterOrEqual(inset.OrthographicSize, full.OrthographicSize);
        // Safe centre is right of screen centre, so the camera shifts left (negative x).
        Assert.Less(inset.LateralOffset.x, 0f);
        Assert.AreEqual(0f, inset.LateralOffset.y, Tolerance);
    }

    [Test]
    public void PresentationDepthNeverCrossesNearPlane()
    {
        Assert.Greater(BoardFraming.SafePresentationDepth(0.1f, 0.3f, 1f), 1.3f);
        Assert.AreEqual(5f, BoardFraming.SafePresentationDepth(5f, 0.3f, 1f), Tolerance);
    }

    [Test]
    public void SpinningTilesStayClearOfTheBoard()
    {
        // Half-extent of 0.35 per unit of depth: a tile at depth d sweeps back to 1.35 d while spinning.
        float depth = BoardFraming.SpinClearDepth(10f, 4f, 0.35f);
        Assert.LessOrEqual(depth * 1.35f, 4f * 0.85f + Tolerance);
        Assert.AreEqual(2f, BoardFraming.SpinClearDepth(2f, 4f, 0.35f), Tolerance, "already clear: unchanged");
        Assert.AreEqual(3f, BoardFraming.SpinClearDepth(3f, 0f, 0.35f), Tolerance, "no board: unchanged");
    }

    [Test]
    public void InvalidInputReturnsDefault()
    {
        BoardFraming.Result fit = BoardFraming.Fit(Vector2.zero, 1.5f, 60f, false, FullScreen);
        Assert.AreEqual(0f, fit.Distance);
    }
}
