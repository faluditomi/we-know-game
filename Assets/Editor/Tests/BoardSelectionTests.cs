using NUnit.Framework;
using UnityEngine;

public class BoardSelectionTests
{
    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(2, 2)]
    [TestCase(4, 2)]
    [TestCase(5, 3)]
    [TestCase(8, 4)]
    [TestCase(12, 4)]
    [TestCase(13, 5)]
    public void MaxSquareSizeFitsTheDesignCount(int designs, int expected)
    {
        Assert.AreEqual(expected, BoardSelection.MaxSquareSize(designs));
    }

    [TestCase(2, 2)]
    [TestCase(2, 3)]
    [TestCase(3, 4)]
    [TestCase(4, 4)]
    [TestCase(1, 4)]
    public void EvenSelectionsWithinTheLimitAreValid(int columns, int rows)
    {
        Assert.AreEqual(BoardSelectionResult.Valid, BoardSelection.Validate(columns, rows, 8));
    }

    [TestCase(1, 1)]
    [TestCase(1, 2)]
    [TestCase(1, 3)]
    public void SelectionsBelowTwoPairsAreTooSmall(int columns, int rows)
    {
        Assert.AreEqual(BoardSelectionResult.TooSmall, BoardSelection.Validate(columns, rows, 8));
    }

    [TestCase(3, 3)]
    [TestCase(1, 5)]
    public void OddSelectionsAreRejected(int columns, int rows)
    {
        Assert.AreEqual(BoardSelectionResult.OddCount, BoardSelection.Validate(columns, rows, 8));
    }

    [Test]
    public void SelectionsNeedingMorePairsThanDesignsAreRejected()
    {
        Assert.AreEqual(BoardSelectionResult.TooManyPairs, BoardSelection.Validate(4, 4, 7));
    }

    [Test]
    public void EmptyDimensionsAreOutOfRange()
    {
        Assert.AreEqual(BoardSelectionResult.OutOfRange, BoardSelection.Validate(0, 4, 8));
    }

    [Test]
    public void EveryInvalidResultHasATranslatedMessage()
    {
        foreach(BoardSelectionResult result in System.Enum.GetValues(typeof(BoardSelectionResult)))
        {
            string key = BoardSelection.Describe(result);
            if(result == BoardSelectionResult.Valid)
            {
                Assert.IsEmpty(key);
                continue;
            }

            CollectionAssert.Contains(LocalizedText.AllKeys, key, result.ToString());
            Assert.IsTrue(LocalizedText.HasTranslation(GameLanguage.English, key), $"{result} has no English text");
            Assert.IsTrue(LocalizedText.HasTranslation(GameLanguage.Hungarian, key), $"{result} has no Hungarian text");
        }
    }

    [TestCase(4f / 3f)]
    [TestCase(16f / 9f)]
    [TestCase(19.5f / 9f)]
    public void WideAndTallBoardsFitOnBothAxes(float aspect)
    {
        foreach(Vector2 board in new[] { new Vector2(4.6f, 1.2f), new Vector2(1.2f, 4.6f), new Vector2(3.4f, 4.6f) })
        {
            BoardFraming.Result fit = BoardFraming.Fit(board, aspect, 60f, false, new Rect(0f, 0f, 1f, 1f));
            float visibleHeight = BoardFraming.VisibleHeightAtDepth(fit.Distance, 60f, false, 0f);
            Assert.GreaterOrEqual(visibleHeight + 0.0001f, board.y, $"height {board} @ {aspect}");
            Assert.GreaterOrEqual(visibleHeight * aspect + 0.0001f, board.x, $"width {board} @ {aspect}");
        }
    }
}
