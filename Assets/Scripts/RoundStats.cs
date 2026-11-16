using UnityEngine;

/// <summary>How a finished board went: shown on the success title, never saved.</summary>
public readonly struct RoundStats
{
    public readonly int Pairs;
    /// <summary>Play time in seconds, excluding time spent paused.</summary>
    public readonly float Seconds;
    public readonly int Misses;

    public RoundStats(int pairs, float seconds, int misses)
    {
        Pairs = Mathf.Max(0, pairs);
        Seconds = Mathf.Max(0f, seconds);
        Misses = Mathf.Max(0, misses);
    }
}

/// <summary>Builds the success-title statistics line, for example "8 pairs · 1:32 · 3 misses".</summary>
public static class RoundStatsText
{
    public static string Format(GameLanguage language, RoundStats stats)
    {
        string pairs = LocalizedText.Format(language, stats.Pairs == 1 ? LocalizedText.StatsPairsOne : LocalizedText.StatsPairsMany, stats.Pairs);
        string misses = LocalizedText.Format(language, stats.Misses == 1 ? LocalizedText.StatsMissesOne : LocalizedText.StatsMissesMany, stats.Misses);
        string separator = LocalizedText.Get(language, LocalizedText.StatsSeparator);
        return pairs + separator + FormatTime(stats.Seconds) + separator + misses;
    }

    /// <summary>Whole seconds as m:ss, rounded down.</summary>
    public static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(Mathf.Max(0f, seconds));
        return $"{total / 60}:{total % 60:00}";
    }
}
