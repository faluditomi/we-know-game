using System.Collections.Generic;
using NUnit.Framework;

public class BoardPresetsTests
{

    private const int Designs = 8; // largest valid board: 4 × 4

    private static BoardSize S(int columns, int rows) => new BoardSize(columns, rows);

    [Test]
    public void SortedByPairsThenColumns()
    {
        var presets = new BoardPresets();
        presets.TryAdd(S(4, 4), Designs);
        presets.TryAdd(S(4, 2), Designs);
        presets.TryAdd(S(2, 2), Designs);
        presets.TryAdd(S(2, 4), Designs);
        presets.TryAdd(S(3, 4), Designs);

        CollectionAssert.AreEqual(new[] { S(2, 2), S(2, 4), S(4, 2), S(3, 4), S(4, 4) }, presets.Sizes);
    }

    [Test]
    public void RejectsDuplicatesInvalidSizesAndAFullList()
    {
        var presets = new BoardPresets();
        Assert.AreEqual(PresetAddResult.Added, presets.TryAdd(S(2, 2), Designs));
        Assert.AreEqual(PresetAddResult.Duplicate, presets.TryAdd(S(2, 2), Designs));
        Assert.AreEqual(PresetAddResult.Invalid, presets.TryAdd(S(3, 3), Designs), "odd cell count");
        Assert.AreEqual(PresetAddResult.Invalid, presets.TryAdd(S(1, 2), Designs), "too small");
        Assert.AreEqual(PresetAddResult.Invalid, presets.TryAdd(S(6, 6), Designs), "more pairs than designs");

        var all = new[] { S(1, 4), S(4, 1), S(2, 3), S(3, 2), S(2, 4), S(4, 2), S(3, 4) };
        foreach(BoardSize size in all) presets.TryAdd(size, Designs);
        Assert.AreEqual(BoardPresets.MaxPresets, presets.Count);
        Assert.AreEqual(PresetAddResult.Full, presets.TryAdd(S(4, 4), Designs));
        Assert.AreEqual(PresetAddResult.Duplicate, presets.TryAdd(S(2, 2), Designs), "duplicates are reported before a full list");
    }

    [Test]
    public void UsableSkipsSizesThatNeedMoreDesigns()
    {
        var presets = new BoardPresets();
        presets.TryAdd(S(2, 2), Designs);
        presets.TryAdd(S(4, 4), Designs);

        CollectionAssert.AreEqual(new[] { S(2, 2) }, presets.Usable(4));
        CollectionAssert.AreEqual(new[] { S(2, 2), S(4, 4) }, presets.Usable(Designs));
        Assert.IsTrue(presets.Remove(S(2, 2)));
        Assert.IsFalse(presets.Remove(S(2, 2)));
        CollectionAssert.IsEmpty(presets.Usable(4));
    }

    [Test]
    public void EncodesColumnsAndRows()
    {
        Assert.AreEqual(304, S(3, 4).Encode());
        Assert.IsTrue(BoardSize.TryDecode(304, out BoardSize size));
        Assert.AreEqual(S(3, 4), size);
        Assert.IsFalse(BoardSize.TryDecode(0, out _));
        Assert.IsFalse(BoardSize.TryDecode(400, out _), "zero rows");
    }

    [Test]
    public void FreshInstallUsesFreeChoiceAndNoPresets()
    {
        var settings = new GameSettings(new MemorySettingsStorage());
        Assert.AreEqual(BoardSizeMode.FreeChoice, settings.BoardSizeMode);
        CollectionAssert.IsEmpty(settings.BoardPresets);
        Assert.IsFalse(settings.UsesBoardPresets(Designs));
    }

    [Test]
    public void PresetModeNeedsAUsablePreset()
    {
        var settings = new GameSettings(new MemorySettingsStorage());
        settings.BoardSizeMode = BoardSizeMode.Presets;
        Assert.IsFalse(settings.UsesBoardPresets(Designs), "an empty list falls back to free choice");

        settings.AddBoardPreset(S(4, 4), Designs);
        Assert.IsTrue(settings.UsesBoardPresets(Designs));
        Assert.IsFalse(settings.UsesBoardPresets(4), "no preset fits 4 designs");
    }

    [Test]
    public void ModeAndPresetsRoundTripThroughStorage()
    {
        var storage = new MemorySettingsStorage();
        var first = new GameSettings(storage);
        int changes = 0;
        first.Changed += () => changes++;
        first.BoardSizeMode = BoardSizeMode.Presets;
        Assert.AreEqual(PresetAddResult.Added, first.AddBoardPreset(S(3, 4), Designs));
        first.AddBoardPreset(S(2, 2), Designs);
        first.AddBoardPreset(S(4, 4), Designs);
        Assert.AreEqual(PresetAddResult.Duplicate, first.AddBoardPreset(S(4, 4), Designs));
        Assert.IsTrue(first.RemoveBoardPreset(S(4, 4)));
        Assert.AreEqual(5, changes, "mode, three adds and a remove; the duplicate changes nothing");

        var second = new GameSettings(storage);
        Assert.AreEqual(BoardSizeMode.Presets, second.BoardSizeMode);
        CollectionAssert.AreEqual(new[] { S(2, 2), S(3, 4) }, second.BoardPresets);
    }

    [Test]
    public void MalformedStoredPresetsAreDropped()
    {
        var storage = new MemorySettingsStorage();
        storage.SetInt(GameSettings.BoardSizeModeKey, 7);
        storage.SetInt(GameSettings.BoardPresetCountKey, 6);
        storage.SetInt(GameSettings.BoardPresetKeyPrefix + 0, 304);  // 3 × 4
        storage.SetInt(GameSettings.BoardPresetKeyPrefix + 1, 303);  // odd
        storage.SetInt(GameSettings.BoardPresetKeyPrefix + 2, 304);  // duplicate
        storage.SetInt(GameSettings.BoardPresetKeyPrefix + 3, -5);   // garbage
        storage.SetInt(GameSettings.BoardPresetKeyPrefix + 4, 202);  // 2 × 2
        // index 5 missing

        var settings = new GameSettings(storage);
        Assert.AreEqual(BoardSizeMode.FreeChoice, settings.BoardSizeMode, "unknown mode reads as free choice");
        CollectionAssert.AreEqual(new[] { S(2, 2), S(3, 4) }, settings.BoardPresets);
    }
}
