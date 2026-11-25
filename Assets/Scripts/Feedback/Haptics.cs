using UnityEngine;

/// <summary>
/// Short, shaped vibrations. <see cref="Handheld.Vibrate"/> is a fixed ~0.5 s buzz, so on Android this talks to the
/// platform Vibrator directly for a crisp jolt and a rhythmic win pattern. Other platforms fall back to the plain buzz.
/// </summary>
public static class Haptics
{
    // A single short tap: the "no" when a revealed pair doesn't match.
    private const long JoltMilliseconds = 30;
    private const int JoltAmplitude = 255;

    // Fanfare rhythm "ta-ta-taaa": two short hits and one long held note.
    // Android waveform format: timings alternate off/on starting with an off delay; amplitude 0 = off.
    private static readonly long[] FanfareTimings = { 0, 70, 90, 70, 90, 420 };
    private static readonly int[] FanfareAmplitudes = { 0, 200, 0, 200, 0, 255 };

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static int sdkInt;
    private static bool initialized;
    private static bool available;
#endif

    public static void Jolt()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if(TryOneShot(JoltMilliseconds, JoltAmplitude)) return;
#endif
        FallbackVibrate();
    }

    public static void Fanfare()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if(TryWaveform(FanfareTimings, FanfareAmplitudes)) return;
#endif
        FallbackVibrate();
    }

    private static void FallbackVibrate()
    {
        // Keeping this reference also makes Unity add the VIBRATE permission to the Android manifest.
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool EnsureVibrator()
    {
        if(initialized) return available;
        initialized = true;
        try
        {
            using(AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                sdkInt = version.GetStatic<int>("SDK_INT");
            }
            using(AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            available = vibrator != null && vibrator.Call<bool>("hasVibrator");
        }
        catch(System.Exception e)
        {
            Debug.LogWarning($"Haptics: Android vibrator unavailable ({e.Message}).");
            available = false;
        }
        return available;
    }

    private static bool TryOneShot(long milliseconds, int amplitude)
    {
        if(!EnsureVibrator()) return false;
        try
        {
            if(sdkInt >= 26)
            {
                using(AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using(AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);
            }
            return true;
        }
        catch(System.Exception e)
        {
            Debug.LogWarning($"Haptics: one-shot vibration failed ({e.Message}).");
            return false;
        }
    }

    private static bool TryWaveform(long[] timings, int[] amplitudes)
    {
        if(!EnsureVibrator()) return false;
        try
        {
            if(sdkInt >= 26)
            {
                using(AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using(AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                vibrator.Call("vibrate", timings, -1);
            }
            return true;
        }
        catch(System.Exception e)
        {
            Debug.LogWarning($"Haptics: waveform vibration failed ({e.Message}).");
            return false;
        }
    }
#endif
}
