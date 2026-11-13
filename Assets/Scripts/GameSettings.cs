using System;
using UnityEngine;

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
/// Sound and music volumes are kept for future audio; nothing plays yet.
/// </summary>
public sealed class GameSettings
{
    public const string VibrationKey = "settings.vibration";
    public const string SoundVolumeKey = "settings.soundVolume";
    public const string MusicVolumeKey = "settings.musicVolume";

    private readonly ISettingsStorage storage;
    private bool vibration;
    private float soundVolume;
    private float musicVolume;

    /// <summary>Raised after any setting changes.</summary>
    public event Action Changed;

    public GameSettings(ISettingsStorage storage, bool defaultVibration = true, float defaultSoundVolume = 1f, float defaultMusicVolume = 1f)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        vibration = storage.GetInt(VibrationKey, defaultVibration ? 1 : 0) != 0;
        soundVolume = Clamp01(storage.GetFloat(SoundVolumeKey, defaultSoundVolume));
        musicVolume = Clamp01(storage.GetFloat(MusicVolumeKey, defaultMusicVolume));
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
