using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>Measures sound clips (duration, audible length, peak, RMS, brightness) for the sound audition analyzer.</summary>
public static class SoundClipAnalysis
{
    public sealed class ClipStats
    {
        public AudioClip Clip;
        public string Path;
        public string Family;
        /// <summary>File length in seconds, including any silent tail.</summary>
        public float Duration;
        public bool Measured;
        /// <summary>Seconds until the sound fades below <see cref="TailDb"/> under its peak; the file length if unmeasured.</summary>
        public float AudibleDuration;
        public float PeakDb;
        /// <summary>RMS over the audible part, so padded silence doesn't make a clip look quieter.</summary>
        public float RmsDb;
        /// <summary>Spectral centroid in Hz: higher sounds brighter.</summary>
        public float BrightnessHz;
    }

    public const float SilenceDb = -120f;
    /// <summary>How far under the peak a clip's tail counts as silence.</summary>
    public const float TailDb = -40f;
    private const int FrameSize = 1024;
    private const int MaxFrames = 600;
    private static readonly Regex TrailingNumber = new Regex(@"^(.*?)[_\-\s]*\d+$", RegexOptions.Compiled);

    /// <summary>
    /// Every clip under the folder. When several clips share a name (one sound shipped in several formats or
    /// channel layouts), only the preferred variant is kept; see <see cref="VariantRank"/>.
    /// </summary>
    public static List<ClipStats> AnalyzeFolder(string folder)
    {
        var results = new List<ClipStats>();
        List<(AudioClip clip, string path)> clips = PreferredVariants(LoadClips(folder));
        for(int i = 0; i < clips.Count; i++)
        {
            if(EditorUtility.DisplayCancelableProgressBar("Analyzing clips", clips[i].path, (float)i / clips.Count)) break;
            results.Add(Analyze(clips[i].clip, clips[i].path));
        }
        EditorUtility.ClearProgressBar();
        return results;
    }

    public static List<(AudioClip clip, string path)> LoadClips(string folder)
    {
        var clips = new List<(AudioClip, string)>();
        foreach(string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if(clip != null) clips.Add((clip, path));
        }
        return clips;
    }

    /// <summary>
    /// Keeps one clip per name: the one with the lowest <see cref="VariantRank"/>. Same-named clips that tie
    /// on rank are all kept, so they stay ambiguous rather than being picked at random.
    /// </summary>
    public static List<(AudioClip clip, string path)> PreferredVariants(IEnumerable<(AudioClip clip, string path)> clips)
    {
        var byName = new Dictionary<string, List<(AudioClip clip, string path)>>(StringComparer.OrdinalIgnoreCase);
        foreach((AudioClip clip, string path) item in clips)
        {
            if(item.clip == null) continue;
            if(!byName.TryGetValue(item.clip.name, out List<(AudioClip, string)> list)) byName[item.clip.name] = list = new List<(AudioClip, string)>();
            list.Add(item);
        }

        var result = new List<(AudioClip clip, string path)>();
        foreach(List<(AudioClip clip, string path)> group in byName.Values)
        {
            int best = int.MaxValue;
            foreach((AudioClip _, string path) in group) best = Math.Min(best, VariantRank(path));
            foreach((AudioClip clip, string path) item in group)
            {
                if(VariantRank(item.path) == best) result.Add(item);
            }
        }
        return result;
    }

    /// <summary>
    /// Lower is preferred among same-named clips: mono before stereo (sound effects play in 2D),
    /// then .ogg, .wav, .mp3, anything else; within .wav, an "SD" folder before an "HD" one.
    /// </summary>
    public static int VariantRank(string path)
    {
        string normalized = (path ?? string.Empty).Replace('\\', '/');
        string lower = normalized.ToLowerInvariant();
        int rank = 0;
        if(lower.Contains("/stereo/")) rank += 100;

        string extension = System.IO.Path.GetExtension(lower);
        switch(extension)
        {
            case ".ogg": break;
            case ".wav": rank += 10; break;
            case ".mp3": rank += 20; break;
            default: rank += 30; break;
        }
        if(lower.Contains("(hd)")) rank += 1;
        return rank;
    }

    public static ClipStats Analyze(AudioClip clip, string path)
    {
        var stats = new ClipStats
        {
            Clip = clip,
            Path = path,
            Family = FamilyOf(clip.name),
            Duration = clip.length,
            AudibleDuration = clip.length
        };

        if(TryReadMono(clip, out float[] mono))
        {
            Measure(mono, clip.frequency, out stats.AudibleDuration, out stats.PeakDb, out stats.RmsDb, out stats.BrightnessHz);
            stats.Measured = true;
        }
        return stats;
    }

    /// <summary>
    /// The clip's name without its trailing number, lowercased: "click_003" → "click".
    /// Names built as "Pack - Sound - 1" keep only the sound: "JDSherbert - Ultimate UI SFX Pack - Popup Open - 1" → "popup open".
    /// </summary>
    public static string FamilyOf(string clipName)
    {
        if(string.IsNullOrEmpty(clipName)) return string.Empty;
        Match match = TrailingNumber.Match(clipName);
        string family = match.Success && match.Groups[1].Length > 0 ? match.Groups[1].Value : clipName;
        int separator = family.LastIndexOf(" - ", StringComparison.Ordinal);
        if(separator >= 0) family = family.Substring(separator + 3);
        return family.Trim().ToLowerInvariant();
    }

    public static float ToDb(float amplitude)
    {
        return amplitude > 1e-6f ? Mathf.Max(SilenceDb, 20f * Mathf.Log10(amplitude)) : SilenceDb;
    }

    /// <summary>
    /// Audible length (until the sound stays more than <see cref="TailDb"/> under its peak), peak and RMS in dBFS
    /// (RMS over the audible part), and brightness as the spectral centroid (Hz) of the non-silent frames.
    /// </summary>
    public static void Measure(float[] mono, int sampleRate, out float audibleSeconds, out float peakDb, out float rmsDb, out float brightnessHz)
    {
        float peak = 0f;
        foreach(float sample in mono) peak = Math.Max(peak, Math.Abs(sample));

        float tail = peak * Mathf.Pow(10f, TailDb / 20f);
        int end = mono.Length;
        while(end > 0 && Math.Abs(mono[end - 1]) <= tail) end--;

        double sumSquares = 0d;
        for(int i = 0; i < end; i++) sumSquares += mono[i] * mono[i];
        float rms = end > 0 ? (float)Math.Sqrt(sumSquares / end) : 0f;

        audibleSeconds = sampleRate > 0 ? (float)end / sampleRate : 0f;
        peakDb = ToDb(peak);
        rmsDb = ToDb(rms);
        brightnessHz = SpectralCentroid(mono, sampleRate);
    }

    private static float SpectralCentroid(float[] mono, int sampleRate)
    {
        if(mono.Length == 0 || sampleRate <= 0) return 0f;

        int hop = FrameSize / 2;
        int frameCount = Math.Max(1, (mono.Length - FrameSize) / hop + 1);
        int stride = Math.Max(1, frameCount / MaxFrames);

        var spectrum = new double[FrameSize / 2 + 1];
        var real = new double[FrameSize];
        var imaginary = new double[FrameSize];
        float silence = Mathf.Pow(10f, -60f / 20f);

        for(int frame = 0; frame < frameCount; frame += stride)
        {
            int start = frame * hop;
            double energy = 0d;
            for(int i = 0; i < FrameSize; i++)
            {
                int index = start + i;
                double sample = index < mono.Length ? mono[index] : 0d;
                energy += sample * sample;
                double window = 0.5d - 0.5d * Math.Cos(2d * Math.PI * i / (FrameSize - 1));
                real[i] = sample * window;
                imaginary[i] = 0d;
            }
            if(Math.Sqrt(energy / FrameSize) < silence) continue;

            Fft(real, imaginary);
            for(int k = 0; k < spectrum.Length; k++)
            {
                spectrum[k] += Math.Sqrt(real[k] * real[k] + imaginary[k] * imaginary[k]);
            }
        }

        double weighted = 0d;
        double total = 0d;
        for(int k = 0; k < spectrum.Length; k++)
        {
            weighted += spectrum[k] * k * sampleRate / FrameSize;
            total += spectrum[k];
        }
        return total > 0d ? (float)(weighted / total) : 0f;
    }

    // In-place iterative radix-2 FFT; the length must be a power of two.
    private static void Fft(double[] real, double[] imaginary)
    {
        int n = real.Length;
        for(int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for(; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if(i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }
        }

        for(int length = 2; length <= n; length <<= 1)
        {
            double angle = -2d * Math.PI / length;
            double stepReal = Math.Cos(angle);
            double stepImaginary = Math.Sin(angle);
            for(int i = 0; i < n; i += length)
            {
                double wReal = 1d;
                double wImaginary = 0d;
                for(int k = 0; k < length / 2; k++)
                {
                    int a = i + k;
                    int b = a + length / 2;
                    double tReal = real[b] * wReal - imaginary[b] * wImaginary;
                    double tImaginary = real[b] * wImaginary + imaginary[b] * wReal;
                    real[b] = real[a] - tReal;
                    imaginary[b] = imaginary[a] - tImaginary;
                    real[a] += tReal;
                    imaginary[a] += tImaginary;
                    double nextReal = wReal * stepReal - wImaginary * stepImaginary;
                    wImaginary = wReal * stepImaginary + wImaginary * stepReal;
                    wReal = nextReal;
                }
            }
        }
    }

    private static bool TryReadMono(AudioClip clip, out float[] mono)
    {
        mono = null;
        try
        {
            if(clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
            int channels = clip.channels;
            int frames = clip.samples;
            if(channels <= 0 || frames <= 0) return false;

            var data = new float[frames * channels];
            if(!clip.GetData(data, 0)) return false;

            mono = new float[frames];
            for(int frame = 0; frame < frames; frame++)
            {
                float sum = 0f;
                for(int channel = 0; channel < channels; channel++) sum += data[frame * channels + channel];
                mono[frame] = sum / channels;
            }
            return true;
        }
        catch(Exception exception)
        {
            Debug.LogWarning($"Sound audition: could not read '{clip.name}': {exception.Message}", clip);
            return false;
        }
    }
}
