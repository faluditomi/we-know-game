using UnityEngine;

/// <summary>
/// Frame-rate target. Without one, Unity runs mobile builds at 30 FPS; this runs at the screen's refresh rate
/// (60, 90, 120 Hz …), or at most 60 with the battery saver on.
/// </summary>
public static class FrameRate
{
    public const int BatterySaverCap = 60;

    public static void Apply(bool batterySaver)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Target(ScreenRefreshRate(), batterySaver);
    }

    /// <summary>The target for a screen refreshing at <paramref name="refreshRate"/> Hz (0 or less when unknown).</summary>
    public static int Target(int refreshRate, bool batterySaver)
    {
        int screen = refreshRate > 0 ? refreshRate : BatterySaverCap;
        return batterySaver ? Mathf.Min(screen, BatterySaverCap) : screen;
    }

    private static int ScreenRefreshRate()
    {
        return Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
    }
}
