/// <summary>Outcome of validating a board size chosen in the grid size picker.</summary>
public enum BoardSelectionResult
{
    Valid,
    OutOfRange,
    TooSmall,
    OddCount,
    TooManyPairs
}

/// <summary>
/// Pure board-size rules shared by the grid size picker and board generation.
/// Kept free of Unity types so it can be unit-tested.
/// </summary>
public static class BoardSelection
{
    /// <summary>Smallest playable board: two pairs.</summary>
    public const int MinCells = 4;

    /// <summary>
    /// Side length of the largest square picker grid whose cell count does not exceed
    /// twice the number of tile designs. Returns 0 when no valid board is possible.
    /// </summary>
    public static int MaxSquareSize(int designCount)
    {
        int maxCells = designCount * 2;
        if(maxCells < MinCells) return 0;

        int size = 1;
        while((size + 1) * (size + 1) <= maxCells) size++;
        return size;
    }

    public static BoardSelectionResult Validate(int columns, int rows, int designCount)
    {
        if(columns < 1 || rows < 1) return BoardSelectionResult.OutOfRange;

        int cells = columns * rows;
        if(cells < MinCells) return BoardSelectionResult.TooSmall;
        if(cells % 2 != 0) return BoardSelectionResult.OddCount;
        if(cells / 2 > designCount) return BoardSelectionResult.TooManyPairs;
        return BoardSelectionResult.Valid;
    }

    /// <summary>Short player-facing explanation for a selection result.</summary>
    public static string Describe(BoardSelectionResult result)
    {
        switch(result)
        {
            case BoardSelectionResult.Valid: return string.Empty;
            case BoardSelectionResult.TooSmall: return "Too small: pick at least 4 tiles";
            case BoardSelectionResult.OddCount: return "Pick an even number of tiles";
            case BoardSelectionResult.TooManyPairs: return "Not enough tile designs for that many pairs";
            default: return "Pick a board inside the grid";
        }
    }
}
