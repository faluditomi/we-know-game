using System.Collections.Generic;

/// <summary>In-memory <see cref="ISettingsStorage"/> for settings tests. Ints are kept as floats, like PlayerPrefs reads them back.</summary>
internal sealed class MemorySettingsStorage : ISettingsStorage
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
