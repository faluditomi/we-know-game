using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player-facing UI strings per <see cref="GameLanguage"/>. A missing translation falls back to English;
/// an unknown key is shown as the key itself (and logged once in the Editor). The game title is not here:
/// it stays English in every language.
/// </summary>
public static class LocalizedText
{
    public const string Play = "Play";
    public const string Settings = "Settings";
    public const string Exit = "Exit";
    public const string Back = "Back";
    public const string ChooseBoard = "ChooseBoard";
    public const string PickerHint = "PickerHint";
    /// <summary>Format: {0} columns, {1} rows, {2} pairs.</summary>
    public const string PickerSize = "PickerSize";
    public const string TooSmall = "TooSmall";
    public const string OddCount = "OddCount";
    public const string TooManyPairs = "TooManyPairs";
    public const string OutOfGrid = "OutOfGrid";
    public const string Paused = "Paused";
    public const string Continue = "Continue";
    public const string MainMenu = "MainMenu";
    public const string Vibration = "Vibration";
    public const string Sound = "Sound";
    public const string Music = "Music";
    public const string Inactivity = "Inactivity";
    public const string Language = "Language";
    /// <summary>Format: {0} seconds.</summary>
    public const string SecondsValue = "SecondsValue";
    public const string TapToContinue = "TapToContinue";
    public const string LoadFailed = "LoadFailed";
    public const string InactivityHint = "InactivityHint";
    /// <summary>Format: {0} pairs (count of 1).</summary>
    public const string StatsPairsOne = "StatsPairsOne";
    /// <summary>Format: {0} pairs.</summary>
    public const string StatsPairsMany = "StatsPairsMany";
    /// <summary>Format: {0} misses (count of 1).</summary>
    public const string StatsMissesOne = "StatsMissesOne";
    /// <summary>Format: {0} misses.</summary>
    public const string StatsMissesMany = "StatsMissesMany";
    public const string StatsSeparator = "StatsSeparator";

    /// <summary>Every key, for validation and tests.</summary>
    public static readonly string[] AllKeys =
    {
        Play, Settings, Exit, Back, ChooseBoard, PickerHint, PickerSize, TooSmall, OddCount, TooManyPairs,
        OutOfGrid, Paused, Continue, MainMenu, Vibration, Sound, Music, Inactivity, Language, SecondsValue,
        TapToContinue, LoadFailed, InactivityHint,
        StatsPairsOne, StatsPairsMany, StatsMissesOne, StatsMissesMany, StatsSeparator
    };

    private static readonly Dictionary<string, string> English = new Dictionary<string, string>
    {
        { Play, "PLAY" },
        { Settings, "SETTINGS" },
        { Exit, "EXIT" },
        { Back, "BACK" },
        { ChooseBoard, "CHOOSE YOUR BOARD" },
        { PickerHint, "Drag across the grid to choose your board" },
        { PickerSize, "{0} × {1}  ·  {2} pairs" },
        { TooSmall, "Too small: pick at least 4 tiles" },
        { OddCount, "Pick an even number of tiles" },
        { TooManyPairs, "Not enough tile designs for that many pairs" },
        { OutOfGrid, "Pick a board inside the grid" },
        { Paused, "PAUSED" },
        { Continue, "CONTINUE" },
        { MainMenu, "MAIN MENU" },
        { Vibration, "VIBRATION" },
        { Sound, "SOUND" },
        { Music, "MUSIC" },
        { Inactivity, "INACTIVITY" },
        { Language, "LANGUAGE" },
        { SecondsValue, "{0} s" },
        { TapToContinue, "Tap anywhere to continue" },
        { LoadFailed, "The game could not be loaded." },
        { InactivityHint, "If nobody touches the game for this long, it returns to the title screen." },
        { StatsPairsOne, "{0} pair" },
        { StatsPairsMany, "{0} pairs" },
        { StatsMissesOne, "{0} miss" },
        { StatsMissesMany, "{0} misses" },
        { StatsSeparator, " · " },
    };

    private static readonly Dictionary<string, string> Hungarian = new Dictionary<string, string>
    {
        { Play, "JÁTÉK" },
        { Settings, "BEÁLLÍTÁSOK" },
        { Exit, "KILÉPÉS" },
        { Back, "VISSZA" },
        { ChooseBoard, "VÁLASSZ TÁBLÁT" },
        { PickerHint, "Húzd végig az ujjad a rácson a tábla méretezéséhez" },
        { PickerSize, "{0} × {1}  ·  {2} pár" },
        { TooSmall, "Túl kicsi: válassz legalább 4 csempét" },
        { OddCount, "Páros számú csempét válassz" },
        { TooManyPairs, "Nincs elég csempe mintánk ennyi párhoz" },
        { OutOfGrid, "Maradj a rácson belül" },
        { Paused, "SZÜNET" },
        { Continue, "FOLYTATÁS" },
        { MainMenu, "FŐMENÜ" },
        { Vibration, "REZGÉS" },
        { Sound, "HANG" },
        { Music, "ZENE" },
        { Inactivity, "TÉTLENSÉG" },
        { Language, "NYELV" },
        { SecondsValue, "{0} mp" },
        { TapToContinue, "Koppints bárhová a folytatáshoz" },
        { LoadFailed, "A játékot nem sikerült betölteni." },
        { InactivityHint, "Ha ennyi ideig senki nem érinti meg a játékot, visszatér a kezdőképernyőre." },
        { StatsPairsOne, "{0} pár" },
        { StatsPairsMany, "{0} pár" },
        { StatsMissesOne, "{0} tévesztés" },
        { StatsMissesMany, "{0} tévesztés" },
        { StatsSeparator, " · " },
    };

#if UNITY_EDITOR
    private static readonly HashSet<string> reportedMissingKeys = new HashSet<string>();
#endif

    /// <summary>The text for <paramref name="key"/> in <paramref name="language"/>.</summary>
    public static string Get(GameLanguage language, string key)
    {
        string text = Lookup(TableFor(language), English, key);
        if(text != null) return text;

#if UNITY_EDITOR
        if(reportedMissingKeys.Add(key ?? string.Empty)) Debug.LogWarning($"LocalizedText: unknown key \"{key}\".");
#endif
        return key ?? string.Empty;
    }

    /// <summary><see cref="Get"/> with <see cref="string.Format(string, object[])"/> applied.</summary>
    public static string Format(GameLanguage language, string key, params object[] args)
    {
        return string.Format(Get(language, key), args);
    }

    /// <summary>Whether <paramref name="language"/> has its own entry for <paramref name="key"/>.</summary>
    public static bool HasTranslation(GameLanguage language, string key)
    {
        return key != null && TableFor(language).ContainsKey(key);
    }

    /// <summary>
    /// Text for <paramref name="key"/> from <paramref name="table"/>, else from <paramref name="fallback"/>,
    /// else null. Blank entries count as missing.
    /// </summary>
    public static string Lookup(IReadOnlyDictionary<string, string> table, IReadOnlyDictionary<string, string> fallback, string key)
    {
        if(key == null) return null;
        if(table != null && table.TryGetValue(key, out string text) && !string.IsNullOrWhiteSpace(text)) return text;
        if(fallback != null && fallback.TryGetValue(key, out text) && !string.IsNullOrWhiteSpace(text)) return text;
        return null;
    }

    private static Dictionary<string, string> TableFor(GameLanguage language)
    {
        switch(language)
        {
            case GameLanguage.Hungarian: return Hungarian;
            default: return English;
        }
    }
}
