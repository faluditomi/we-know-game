using System.Collections.Generic;
using NUnit.Framework;

public class GameSettingsTests
{
    private sealed class MemoryStorage : ISettingsStorage
    {
        public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
        public int Saves;
        public bool HasKey(string key) => Values.ContainsKey(key);
        public int GetInt(string key, int fallback) => Values.TryGetValue(key, out float v) ? (int)v : fallback;
        public float GetFloat(string key, float fallback) => Values.TryGetValue(key, out float v) ? v : fallback;
        public void SetInt(string key, int value) => Values[key] = value;
        public void SetFloat(string key, float value) => Values[key] = value;
        public void Save() => Saves++;
    }

    [Test]
    public void FreshInstallUsesDefaults()
    {
        GameSettings settings = new GameSettings(new MemoryStorage(), defaultVibration: false, defaultSoundVolume: 0.5f, defaultMusicVolume: 0.25f);
        Assert.IsFalse(settings.Vibration);
        Assert.AreEqual(0.5f, settings.SoundVolume);
        Assert.AreEqual(0.25f, settings.MusicVolume);
    }

    [Test]
    public void ValuesRoundTripThroughStorage()
    {
        MemoryStorage storage = new MemoryStorage();
        GameSettings first = new GameSettings(storage);
        first.Vibration = false;
        first.SoundVolume = 0.3f;
        first.MusicVolume = 0.7f;

        GameSettings second = new GameSettings(storage);
        Assert.IsFalse(second.Vibration);
        Assert.AreEqual(0.3f, second.SoundVolume, 0.0001f);
        Assert.AreEqual(0.7f, second.MusicVolume, 0.0001f);
        Assert.AreEqual(3, storage.Saves);
    }

    [Test]
    public void VolumesAreClamped()
    {
        MemoryStorage storage = new MemoryStorage();
        storage.Values[GameSettings.SoundVolumeKey] = 4f;
        GameSettings settings = new GameSettings(storage);
        Assert.AreEqual(1f, settings.SoundVolume);

        settings.MusicVolume = -2f;
        Assert.AreEqual(0f, settings.MusicVolume);
        settings.MusicVolume = float.NaN;
        Assert.AreEqual(0f, settings.MusicVolume);
    }

    [Test]
    public void InactivityTimeoutDefaultsTo60Seconds()
    {
        Assert.AreEqual(60, new GameSettings(new MemoryStorage()).InactivityTimeoutSeconds);
    }

    [TestCase(5, 15)]
    [TestCase(15, 15)]
    [TestCase(42, 40)]
    [TestCase(43, 45)]
    [TestCase(300, 300)]
    [TestCase(1000, 300)]
    public void InactivityTimeoutIsClampedAndSnappedToFiveSeconds(int requested, int expected)
    {
        GameSettings settings = new GameSettings(new MemoryStorage());
        settings.InactivityTimeoutSeconds = requested;
        Assert.AreEqual(expected, settings.InactivityTimeoutSeconds);
    }

    [Test]
    public void InactivityTimeoutRoundTripsAndSanitisesStoredValues()
    {
        MemoryStorage storage = new MemoryStorage();
        new GameSettings(storage).InactivityTimeoutSeconds = 90;
        Assert.AreEqual(90, new GameSettings(storage).InactivityTimeoutSeconds);

        storage.Values[GameSettings.InactivityTimeoutKey] = 2;
        Assert.AreEqual(15, new GameSettings(storage).InactivityTimeoutSeconds);
    }

    [Test]
    public void LanguageDefaultsToEnglish()
    {
        Assert.AreEqual(GameLanguage.English, new GameSettings(new MemoryStorage()).Language);
    }

    [Test]
    public void LanguageRoundTripsThroughStorage()
    {
        MemoryStorage storage = new MemoryStorage();
        new GameSettings(storage).Language = GameLanguage.Hungarian;
        Assert.AreEqual(GameLanguage.Hungarian, new GameSettings(storage).Language);
        Assert.AreEqual(1, storage.Saves);
    }

    [TestCase(-1)]
    [TestCase(2)]
    [TestCase(99)]
    public void UnknownStoredLanguageReadsAsEnglish(int stored)
    {
        MemoryStorage storage = new MemoryStorage();
        storage.Values[GameSettings.LanguageKey] = stored;
        Assert.AreEqual(GameLanguage.English, new GameSettings(storage).Language);
    }

    [Test]
    public void SettingAnUnknownLanguageStoresEnglish()
    {
        GameSettings settings = new GameSettings(new MemoryStorage());
        settings.Language = GameLanguage.Hungarian;
        settings.Language = (GameLanguage)42;
        Assert.AreEqual(GameLanguage.English, settings.Language);
    }

    [Test]
    public void LanguageRaisesTheEventOnlyWhenItChanges()
    {
        GameSettings settings = new GameSettings(new MemoryStorage());
        int raised = 0;
        settings.Changed += () => raised++;
        settings.Language = GameLanguage.English;   // unchanged default
        settings.Language = GameLanguage.Hungarian;
        settings.Language = GameLanguage.Hungarian; // unchanged
        Assert.AreEqual(1, raised);
    }

    [Test]
    public void ChangesRaiseTheEventOnlyWhenValuesChange()
    {
        GameSettings settings = new GameSettings(new MemoryStorage());
        int raised = 0;
        settings.Changed += () => raised++;
        settings.Vibration = true;   // unchanged default
        settings.Vibration = false;
        settings.SoundVolume = 1f;   // unchanged default
        settings.SoundVolume = 0.4f;
        Assert.AreEqual(2, raised);
    }
}
