using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor side of the sound audition tooling: scene setup and writing picks into SoundEffects.</summary>
public static class SoundAuditionEditor
{
    public const string SoundsFolder = "Assets/Sounds";
    public const string CandidatesPath = SoundsFolder + "/SoundAuditionCandidates.asset";
    private const string MenuRoot = "Tools/Sound Audition/";

    [MenuItem(MenuRoot + "Set Up In Scene", priority = 0)]
    private static void SetUpInScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Sound Audition", "Leave play mode first.", "OK");
            return;
        }

        SoundEffects soundEffects = UnityEngine.Object.FindFirstObjectByType<SoundEffects>(FindObjectsInactive.Include);
        if(soundEffects == null)
        {
            EditorUtility.DisplayDialog("Sound Audition", "No SoundEffects in the open scene. Open MainScene first.", "OK");
            return;
        }

        SoundAuditionCandidates candidates = FindOrCreateCandidates();

        SoundAudition audition = UnityEngine.Object.FindFirstObjectByType<SoundAudition>(FindObjectsInactive.Include);
        if(audition == null)
        {
            var gameObject = new GameObject("SoundAudition");
            MoveToSceneOf(gameObject, soundEffects.gameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Set Up Sound Audition");
            audition = gameObject.AddComponent<SoundAudition>();
        }

        var serialized = new SerializedObject(audition);
        serialized.FindProperty("soundEffects").objectReferenceValue = soundEffects;
        serialized.FindProperty("candidates").objectReferenceValue = candidates;
        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(audition.gameObject.scene);
        Selection.activeObject = audition;
        Debug.Log("Sound audition is set up. Save the scene, then press Play and F8 (or tap SFX) to audition.", audition);
    }

    private static void MoveToSceneOf(GameObject gameObject, GameObject other)
    {
        if(gameObject.scene != other.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, other.scene);
    }

    /// <summary>The project's candidates asset, created under Assets/Sounds if there is none.</summary>
    public static SoundAuditionCandidates FindOrCreateCandidates()
    {
        string[] guids = AssetDatabase.FindAssets("t:" + nameof(SoundAuditionCandidates));
        foreach(string guid in guids)
        {
            var existing = AssetDatabase.LoadAssetAtPath<SoundAuditionCandidates>(AssetDatabase.GUIDToAssetPath(guid));
            if(existing != null) return existing;
        }

        var created = ScriptableObject.CreateInstance<SoundAuditionCandidates>();
        AssetDatabase.CreateAsset(created, CandidatesPath);
        AssetDatabase.SaveAssets();
        Debug.Log("Created " + CandidatesPath, created);
        return created;
    }

    /// <summary>
    /// Every AudioClip under Assets/Sounds, grouped by clip name (case-insensitive). Where one sound ships in
    /// several formats, only its preferred variant is listed (see <see cref="SoundClipAnalysis.VariantRank"/>).
    /// </summary>
    public static Dictionary<string, List<AudioClip>> ClipsByName()
    {
        var clips = new Dictionary<string, List<AudioClip>>(StringComparer.OrdinalIgnoreCase);
        foreach((AudioClip clip, string _) in SoundClipAnalysis.PreferredVariants(SoundClipAnalysis.LoadClips(SoundsFolder)))
        {
            if(!clips.TryGetValue(clip.name, out List<AudioClip> list)) clips[clip.name] = list = new List<AudioClip>();
            if(!list.Contains(clip)) list.Add(clip);
        }
        return clips;
    }

    public static List<SoundAuditionPicks.Line> ParsePicks(string text)
    {
        Dictionary<string, List<AudioClip>> clips = ClipsByName();
        return SoundAuditionPicks.Parse(text, name => clips.TryGetValue(name, out List<AudioClip> list) ? list : null);
    }

    /// <summary>The SoundEffects field that holds a cue, e.g. TileFlip → tileFlip.</summary>
    public static string CueFieldName(SoundCue cue)
    {
        string name = cue.ToString();
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// Writes the valid lines' clips into the open scene's SoundEffects with Undo. Lines with errors are skipped.
    /// Returns a short report.
    /// </summary>
    public static string ApplyPicksToScene(IEnumerable<SoundAuditionPicks.Line> lines)
    {
        if(EditorApplication.isPlaying) return "Leave play mode first; play-mode changes are discarded.";

        SoundEffects soundEffects = UnityEngine.Object.FindFirstObjectByType<SoundEffects>(FindObjectsInactive.Include);
        if(soundEffects == null) return "No SoundEffects in the open scene.";

        var serialized = new SerializedObject(soundEffects);
        var report = new StringBuilder();
        int changed = 0;
        int skipped = 0;
        foreach(SoundAuditionPicks.Line line in lines)
        {
            if(!line.IsOk)
            {
                skipped++;
                report.Append("Skipped: ").Append(line.Message).Append('\n');
                continue;
            }

            SerializedProperty property = serialized.FindProperty(CueFieldName(line.Cue) + ".clip");
            if(property == null)
            {
                skipped++;
                report.Append("Skipped: SoundEffects has no field for ").Append(line.Cue).Append('\n');
                continue;
            }
            if(property.objectReferenceValue == line.Clip) continue;

            property.objectReferenceValue = line.Clip;
            changed++;
            report.Append(line.Message).Append('\n');
        }

        if(changed > 0)
        {
            Undo.SetCurrentGroupName("Apply Sound Picks");
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(soundEffects.gameObject.scene);
        }

        return $"Changed {changed} cue(s), skipped {skipped} line(s).\n{report}";
    }
}

/// <summary>Applies picks saved by the overlay's "Apply picks" once play mode ends (play-mode changes are discarded).</summary>
[InitializeOnLoad]
internal static class SoundAuditionPlayModeApplier
{
    static SoundAuditionPlayModeApplier()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if(state != PlayModeStateChange.EnteredEditMode) return;

        string text = SessionState.GetString(SoundAudition.PendingPicksKey, string.Empty);
        if(string.IsNullOrEmpty(text)) return;
        SessionState.EraseString(SoundAudition.PendingPicksKey);

        string report = SoundAuditionEditor.ApplyPicksToScene(SoundAuditionEditor.ParsePicks(text));
        Debug.Log("Sound audition picks applied (Undo reverts them). Save the scene to keep them.\n" + report);
    }
}
