using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>What PLAY offers: the free grid picker, or the preset board sizes.</summary>
public enum BoardSizeMode
{
    FreeChoice = 0,
    Presets = 1
}

/// <summary>Key/value storage for primitive settings values.</summary>
public interface ISettingsStorage
{
    bool HasKey(string key);
    int GetInt(string key, int fallback);
    float GetFloat(string key, float fallback);
    void SetInt(string key, int value);
    void SetFloat(string key, float value);
    void Save();
}

/// <summary>Stores settings in Unity's <see cref="PlayerPrefs"/>.</summary>
public sealed class PlayerPrefsSettingsStorage : ISettingsStorage
{
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
    public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
    public void Save() => PlayerPrefs.Save();
}

/// <summary>
/// Player settings persisted as primitive values. Changes are stored and announced immediately.
/// The board size mode and preset sizes decide what PLAY offers (see <see cref="BoardPresets"/>).
/// The sound volume scales every <see cref="SoundEffects"/> cue; the music volume scales the <see cref="MusicPlayer"/> track.
/// </summary>
public sealed class GameSettings
{
    public const string VibrationKey = "settings.vibration";
    public const string SoundVolumeKey = "settings.soundVolume";
    public const string MusicVolumeKey = "settings.musicVolume";
    public const string InactivityTimeoutKey = "settings.inactivityTimeout";
    public const string LanguageKey = "settings.language";
    public const string BatterySaverKey = "settings.batterySaver";
    public const string BoardSizeModeKey = "settings.boardSizeMode";
    public const string BoardPresetCountKey = "settings.boardPresets.count";
    /// <summary>Followed by the preset index; each value is <see cref="BoardSize.Encode"/>.</summary>
    public const string BoardPresetKeyPrefix = "settings.boardPresets.";

    public const int DefaultInactivityTimeoutSeconds = 60;
    public const int MinInactivityTimeoutSeconds = 15;
    public const int MaxInactivityTimeoutSeconds = 300;
    public const int InactivityTimeoutStepSeconds = 5;

    private readonly ISettingsStorage storage;
    private bool vibration;
    private float soundVolume;
    private float musicVolume;
    private int inactivityTimeoutSeconds;
    private GameLanguage language;
    private bool batterySaver;
    private BoardSizeMode boardSizeMode;
    private readonly BoardPresets boardPresets = new BoardPresets();

    /// <summary>Raised after any setting changes.</summary>
    public event Action Changed;

    public GameSettings(ISettingsStorage storage, bool defaultVibration = true, float defaultSoundVolume = 1f, float defaultMusicVolume = 1f)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        vibration = storage.GetInt(VibrationKey, defaultVibration ? 1 : 0) != 0;
        soundVolume = Clamp01(storage.GetFloat(SoundVolumeKey, defaultSoundVolume));
        musicVolume = Clamp01(storage.GetFloat(MusicVolumeKey, defaultMusicVolume));
        inactivityTimeoutSeconds = ClampTimeout(storage.GetInt(InactivityTimeoutKey, DefaultInactivityTimeoutSeconds));
        int storedLanguage = storage.GetInt(LanguageKey, (int)GameLanguage.English);
        language = GameLanguages.IsDefined(storedLanguage) ? (GameLanguage)storedLanguage : GameLanguage.English;
        batterySaver = storage.GetInt(BatterySaverKey, 0) != 0;
        boardSizeMode = storage.GetInt(BoardSizeModeKey, 0) == (int)BoardSizeMode.Presets ? BoardSizeMode.Presets : BoardSizeMode.FreeChoice;
        LoadBoardPresets();
    }

    /// <summary>Whether PLAY offers the free grid picker or the preset sizes. Free choice on first launch.</summary>
    public BoardSizeMode BoardSizeMode
    {
        get => boardSizeMode;
        set
        {
            BoardSizeMode sanitised = value == BoardSizeMode.Presets ? BoardSizeMode.Presets : BoardSizeMode.FreeChoice;
            if(boardSizeMode == sanitised) return;
            boardSizeMode = sanitised;
            storage.SetInt(BoardSizeModeKey, (int)sanitised);
            Commit();
        }
    }

    /// <summary>Preset board sizes, sorted. Empty on first launch.</summary>
    public IReadOnlyList<BoardSize> BoardPresets => boardPresets.Sizes;

    /// <summary>Preset sizes that are valid boards for <paramref name="designCount"/> tile designs.</summary>
    public List<BoardSize> UsableBoardPresets(int designCount) => boardPresets.Usable(designCount);

    /// <summary>True when PLAY should show the preset choice: preset mode with at least one usable size.</summary>
    public bool UsesBoardPresets(int designCount) => boardSizeMode == BoardSizeMode.Presets && boardPresets.Usable(designCount).Count > 0;

    /// <summary>Adds a preset size and saves the list when it was added.</summary>
    public PresetAddResult AddBoardPreset(BoardSize size, int designCount)
    {
        PresetAddResult result = boardPresets.TryAdd(size, designCount);
        if(result == PresetAddResult.Added) SaveBoardPresets();
        return result;
    }

    /// <summary>Removes a preset size and saves the list. Returns false when it wasn't in the list.</summary>
    public bool RemoveBoardPreset(BoardSize size)
    {
        if(!boardPresets.Remove(size)) return false;
        SaveBoardPresets();
        return true;
    }

    private void LoadBoardPresets()
    {
        // Malformed, duplicate and excess entries are dropped.
        int count = storage.GetInt(BoardPresetCountKey, 0);
        for(int i = 0; i < count && i < 64; i++)
        {
            if(BoardSize.TryDecode(storage.GetInt(BoardPresetKeyPrefix + i, 0), out BoardSize size)) boardPresets.TryAddStored(size);
        }
    }

    private void SaveBoardPresets()
    {
        IReadOnlyList<BoardSize> sizes = boardPresets.Sizes;
        storage.SetInt(BoardPresetCountKey, sizes.Count);
        for(int i = 0; i < sizes.Count; i++) storage.SetInt(BoardPresetKeyPrefix + i, sizes[i].Encode());
        Commit();
    }

    /// <summary>Caps the frame rate at 60 instead of the screen's full refresh rate. Off on first launch.</summary>
    public bool BatterySaver
    {
        get => batterySaver;
        set
        {
            if(batterySaver == value) return;
            batterySaver = value;
            storage.SetInt(BatterySaverKey, value ? 1 : 0);
            Commit();
        }
    }

    /// <summary>Display language. English on first launch; unknown stored values read as English.</summary>
    public GameLanguage Language
    {
        get => language;
        set
        {
            GameLanguage sanitised = GameLanguages.IsDefined((int)value) ? value : GameLanguage.English;
            if(language == sanitised) return;
            language = sanitised;
            storage.SetInt(LanguageKey, (int)sanitised);
            Commit();
        }
    }

    /// <summary>
    /// Seconds without input during a game before it returns to the title. 15–300 in 5-second steps.
    /// </summary>
    public int InactivityTimeoutSeconds
    {
        get => inactivityTimeoutSeconds;
        set
        {
            int clamped = ClampTimeout(value);
            if(inactivityTimeoutSeconds == clamped) return;
            inactivityTimeoutSeconds = clamped;
            storage.SetInt(InactivityTimeoutKey, clamped);
            Commit();
        }
    }

    /// <summary>Clamps to the allowed range and snaps to the nearest 5-second step.</summary>
    public static int ClampTimeout(int seconds)
    {
        int step = InactivityTimeoutStepSeconds;
        int snapped = (int)System.Math.Round(seconds / (double)step, System.MidpointRounding.AwayFromZero) * step;
        return snapped < MinInactivityTimeoutSeconds ? MinInactivityTimeoutSeconds
            : snapped > MaxInactivityTimeoutSeconds ? MaxInactivityTimeoutSeconds
            : snapped;
    }

    public bool Vibration
    {
        get => vibration;
        set
        {
            if(vibration == value) return;
            vibration = value;
            storage.SetInt(VibrationKey, value ? 1 : 0);
            Commit();
        }
    }

    /// <summary>Sound effects volume, 0–1.</summary>
    public float SoundVolume
    {
        get => soundVolume;
        set
        {
            float clamped = Clamp01(value);
            if(soundVolume == clamped) return;
            soundVolume = clamped;
            storage.SetFloat(SoundVolumeKey, clamped);
            Commit();
        }
    }

    /// <summary>Music volume, 0–1.</summary>
    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            float clamped = Clamp01(value);
            if(musicVolume == clamped) return;
            musicVolume = clamped;
            storage.SetFloat(MusicVolumeKey, clamped);
            Commit();
        }
    }

    private void Commit()
    {
        storage.Save();
        Changed?.Invoke();
    }

    private static float Clamp01(float value)
    {
        if(float.IsNaN(value)) return 0f;
        return value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
