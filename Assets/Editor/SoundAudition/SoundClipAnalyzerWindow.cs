using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Lists every clip under Assets/Sounds with its measurements, previews clips, exports CSV and seeds shortlists.</summary>
public sealed class SoundClipAnalyzerWindow : EditorWindow
{
    private enum Column
    {
        Name,
        Duration,
        Audible,
        Peak,
        Rms,
        Brightness
    }

    private List<SoundClipAnalysis.ClipStats> stats = new List<SoundClipAnalysis.ClipStats>();
    private Column sortColumn = Column.Name;
    private bool descending;
    private string filter = string.Empty;
    private string message;
    private Vector2 scroll;

    [MenuItem("Tools/Sound Audition/Clip Analyzer", priority = 10)]
    private static void Open()
    {
        GetWindow<SoundClipAnalyzerWindow>("Clip Analyzer");
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        ClipPreview.Stop();
    }

    private void Refresh()
    {
        stats = SoundClipAnalysis.AnalyzeFolder(SoundAuditionEditor.SoundsFolder);
        message = $"{stats.Count} clip(s), {stats.Count(s => !s.Measured)} unreadable.";
    }

    private void OnGUI()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if(GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f))) Refresh();
            if(GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(80f))) ExportCsv();
            if(GUILayout.Button("Seed shortlists", EditorStyles.toolbarButton, GUILayout.Width(100f))) Seed();
            if(GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(45f))) ClipPreview.Stop();
            GUILayout.FlexibleSpace();
            filter = EditorGUILayout.TextField(filter, EditorStyles.toolbarSearchField, GUILayout.Width(180f));
        }

        if(!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.None);

        using(new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(32f);
            HeaderButton("Clip", Column.Name, 200f);
            HeaderButton("Duration s", Column.Duration, 80f);
            HeaderButton("Audible s", Column.Audible, 80f);
            HeaderButton("Peak dBFS", Column.Peak, 80f);
            HeaderButton("RMS dBFS", Column.Rms, 80f);
            HeaderButton("Brightness Hz", Column.Brightness, 100f);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach(SoundClipAnalysis.ClipStats clip in Sorted())
        {
            if(clip.Clip == null) continue;
            if(filter.Length > 0 && clip.Clip.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("▶", GUILayout.Width(28f))) ClipPreview.Play(clip.Clip);
                if(GUILayout.Button(clip.Clip.name, EditorStyles.label, GUILayout.Width(200f))) EditorGUIUtility.PingObject(clip.Clip);
                GUILayout.Label(clip.Duration.ToString("0.000", CultureInfo.InvariantCulture), GUILayout.Width(80f));
                GUILayout.Label(clip.Measured ? clip.AudibleDuration.ToString("0.000", CultureInfo.InvariantCulture) : "n/a", GUILayout.Width(80f));
                GUILayout.Label(clip.Measured ? clip.PeakDb.ToString("0.0", CultureInfo.InvariantCulture) : "n/a", GUILayout.Width(80f));
                GUILayout.Label(clip.Measured ? clip.RmsDb.ToString("0.0", CultureInfo.InvariantCulture) : "n/a", GUILayout.Width(80f));
                GUILayout.Label(clip.Measured ? clip.BrightnessHz.ToString("0", CultureInfo.InvariantCulture) : "n/a", GUILayout.Width(100f));
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void HeaderButton(string label, Column column, float width)
    {
        string arrow = sortColumn == column ? (descending ? " ▼" : " ▲") : "";
        if(GUILayout.Button(label + arrow, EditorStyles.miniButton, GUILayout.Width(width)))
        {
            if(sortColumn == column) descending = !descending;
            else
            {
                sortColumn = column;
                descending = false;
            }
        }
    }

    private IEnumerable<SoundClipAnalysis.ClipStats> Sorted()
    {
        // Unreadable clips always sort last on measured columns.
        Func<SoundClipAnalysis.ClipStats, float> key;
        switch(sortColumn)
        {
            case Column.Duration: key = s => s.Duration; break;
            case Column.Audible: key = s => s.AudibleDuration; break;
            case Column.Peak: key = s => s.PeakDb; break;
            case Column.Rms: key = s => s.RmsDb; break;
            case Column.Brightness: key = s => s.BrightnessHz; break;
            default:
                IEnumerable<SoundClipAnalysis.ClipStats> byName = stats.OrderBy(s => s.Clip != null ? s.Clip.name : "", StringComparer.OrdinalIgnoreCase);
                return descending ? byName.Reverse() : byName;
        }

        bool measuredOnly = sortColumn != Column.Duration;
        IOrderedEnumerable<SoundClipAnalysis.ClipStats> ordered = stats.OrderBy(s => measuredOnly && !s.Measured ? 1 : 0);
        return descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
    }

    private void ExportCsv()
    {
        string path = EditorUtility.SaveFilePanel("Export clip measurements", "", "sound-clips.csv", "csv");
        if(string.IsNullOrEmpty(path)) return;

        var csv = new StringBuilder("name,family,path,duration_s,audible_s,peak_dbfs,rms_dbfs,brightness_hz\n");
        foreach(SoundClipAnalysis.ClipStats clip in Sorted())
        {
            if(clip.Clip == null) continue;
            csv.Append(Csv(clip.Clip.name)).Append(',')
                .Append(clip.Family).Append(',')
                .Append(Csv(clip.Path)).Append(',')
                .Append(clip.Duration.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                .Append(clip.Measured ? clip.AudibleDuration.ToString("0.000", CultureInfo.InvariantCulture) : "").Append(',')
                .Append(clip.Measured ? clip.PeakDb.ToString("0.0", CultureInfo.InvariantCulture) : "").Append(',')
                .Append(clip.Measured ? clip.RmsDb.ToString("0.0", CultureInfo.InvariantCulture) : "").Append(',')
                .Append(clip.Measured ? clip.BrightnessHz.ToString("0", CultureInfo.InvariantCulture) : "").Append('\n');
        }
        File.WriteAllText(path, csv.ToString());
        message = "Exported " + path;
    }

    // Pack clip names and folders contain spaces and parentheses; quote fields with commas or quotes.
    private static string Csv(string value)
    {
        if(string.IsNullOrEmpty(value) || (value.IndexOf(',') < 0 && value.IndexOf('"') < 0)) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private void Seed()
    {
        SoundAuditionCandidates asset = SoundAuditionEditor.FindOrCreateCandidates();
        string report = SoundAuditionSeeding.Seed(asset, stats);
        message = report;
        Debug.Log("Sound audition shortlists seeded into " + AssetDatabase.GetAssetPath(asset) + "\n" + report, asset);
    }

    /// <summary>Plays clips in the Editor through Unity's internal preview API, falling back to a hidden AudioSource.</summary>
    private static class ClipPreview
    {
        private static readonly MethodInfo PlayMethod;
        private static readonly MethodInfo StopMethod;
        private static AudioSource fallbackSource;

        static ClipPreview()
        {
            Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if(audioUtil == null) return;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            PlayMethod = audioUtil.GetMethod("PlayPreviewClip", flags, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            StopMethod = audioUtil.GetMethod("StopAllPreviewClips", flags, null, Type.EmptyTypes, null);
        }

        public static void Play(AudioClip clip)
        {
            Stop();
            if(PlayMethod != null)
            {
                try
                {
                    PlayMethod.Invoke(null, new object[] { clip, 0, false });
                    return;
                }
                catch(Exception)
                {
                    // Fall through to the AudioSource fallback.
                }
            }

            if(fallbackSource == null)
            {
                var gameObject = new GameObject("SoundAuditionPreview") { hideFlags = HideFlags.HideAndDontSave };
                fallbackSource = gameObject.AddComponent<AudioSource>();
            }
            fallbackSource.clip = clip;
            fallbackSource.Play();
        }

        public static void Stop()
        {
            if(StopMethod != null)
            {
                try { StopMethod.Invoke(null, null); }
                catch(Exception) { /* ignore */ }
            }
            if(fallbackSource != null) fallbackSource.Stop();
        }
    }
}
