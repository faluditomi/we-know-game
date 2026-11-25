using System;
using System.Collections.Generic;

/// <summary>A board size: columns × rows.</summary>
public readonly struct BoardSize : IEquatable<BoardSize>
{
    public readonly int Columns;
    public readonly int Rows;

    public BoardSize(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
    }

    public int Cells => Columns * Rows;
    public int Pairs => Cells / 2;

    /// <summary>Stored form: columns × 100 + rows, e.g. 3 × 4 → 304.</summary>
    public int Encode() => Columns * 100 + Rows;

    public static bool TryDecode(int encoded, out BoardSize size)
    {
        size = new BoardSize(encoded / 100, encoded % 100);
        return encoded > 0 && size.Columns > 0 && size.Rows > 0;
    }

    public bool Equals(BoardSize other) => Columns == other.Columns && Rows == other.Rows;
    public override bool Equals(object obj) => obj is BoardSize other && Equals(other);
    public override int GetHashCode() => Columns * 397 ^ Rows;
    public override string ToString() => $"{Columns} × {Rows}";
}

public enum PresetAddResult
{
    Added,
    Duplicate,
    Full,
    Invalid
}

/// <summary>
/// The preset board sizes players can pick from in preset mode: de-duplicated, at most
/// <see cref="MaxPresets"/>, and always sorted by pair count, then by columns.
/// Kept free of Unity types so it can be unit-tested.
/// </summary>
public sealed class BoardPresets
{
    public const int MaxPresets = 8;

    private readonly List<BoardSize> sizes = new List<BoardSize>();

    public IReadOnlyList<BoardSize> Sizes => sizes;
    public int Count => sizes.Count;

    /// <summary>Adds a size that is a valid board for <paramref name="designCount"/> tile designs.</summary>
    public PresetAddResult TryAdd(BoardSize size, int designCount)
    {
        if(!IsUsable(size, designCount)) return PresetAddResult.Invalid;
        if(sizes.Contains(size)) return PresetAddResult.Duplicate;
        if(sizes.Count >= MaxPresets) return PresetAddResult.Full;

        sizes.Add(size);
        sizes.Sort(Compare);
        return PresetAddResult.Added;
    }

    /// <summary>
    /// Adds a stored size, checking only that it is a well-formed board (no tile-design limit, which isn't known
    /// at load). Returns false for malformed, duplicate or excess entries.
    /// </summary>
    public bool TryAddStored(BoardSize size)
    {
        return TryAdd(size, int.MaxValue / 2) == PresetAddResult.Added;
    }

    public bool Remove(BoardSize size) => sizes.Remove(size);

    public void Clear() => sizes.Clear();

    /// <summary>The sizes that are valid boards for <paramref name="designCount"/> tile designs, in list order.</summary>
    public List<BoardSize> Usable(int designCount)
    {
        var usable = new List<BoardSize>();
        foreach(BoardSize size in sizes)
        {
            if(IsUsable(size, designCount)) usable.Add(size);
        }
        return usable;
    }

    /// <summary>True when at least one size is a valid board for <paramref name="designCount"/> tile designs. Doesn't allocate.</summary>
    public bool HasUsable(int designCount)
    {
        foreach(BoardSize size in sizes)
        {
            if(IsUsable(size, designCount)) return true;
        }
        return false;
    }

    public static bool IsUsable(BoardSize size, int designCount)
    {
        return BoardSelection.Validate(size.Columns, size.Rows, designCount) == BoardSelectionResult.Valid;
    }

    /// <summary>Fewer pairs first; for equal pairs, fewer columns first.</summary>
    public static int Compare(BoardSize a, BoardSize b)
    {
        int byPairs = a.Pairs.CompareTo(b.Pairs);
        return byPairs != 0 ? byPairs : a.Columns.CompareTo(b.Columns);
    }
}
