using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Imports picks copied from the audition overlay on a device and applies them to SoundEffects.</summary>
public sealed class SoundAuditionImportWindow : EditorWindow
{
    private string text = string.Empty;
    private List<SoundAuditionPicks.Line> parsed;
    private string result;
    private Vector2 textScroll;
    private Vector2 resultScroll;

    [MenuItem("Tools/Sound Audition/Import Picks...", priority = 20)]
    private static void Open()
    {
        GetWindow<SoundAuditionImportWindow>("Import Sound Picks");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Paste the text from the overlay's 'Copy picks' (one 'Cue = clip' line per cue), check it, then apply. Applying changes the open scene's SoundEffects and can be undone.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        textScroll = EditorGUILayout.BeginScrollView(textScroll, GUILayout.MinHeight(140f), GUILayout.MaxHeight(260f));
        text = EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        if(EditorGUI.EndChangeCheck())
        {
            parsed = null;
            result = null;
        }

        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("Paste from clipboard"))
            {
                text = EditorGUIUtility.systemCopyBuffer ?? string.Empty;
                parsed = SoundAuditionEditor.ParsePicks(text);
                result = null;
                GUI.FocusControl(null);
            }
            if(GUILayout.Button("Check")) parsed = SoundAuditionEditor.ParsePicks(text);
        }

        if(parsed == null) return;

        int ok = 0;
        resultScroll = EditorGUILayout.BeginScrollView(resultScroll);
        foreach(SoundAuditionPicks.Line line in parsed)
        {
            if(line.IsOk) ok++;
            EditorGUILayout.LabelField((line.IsOk ? "OK    " : "ERROR ") + line.Message);
        }
        if(parsed.Count == 0) EditorGUILayout.LabelField("No pick lines found.");
        EditorGUILayout.EndScrollView();

        using(new EditorGUI.DisabledScope(ok == 0 || EditorApplication.isPlaying))
        {
            if(GUILayout.Button($"Apply {ok} pick(s) to SoundEffects")) result = SoundAuditionEditor.ApplyPicksToScene(parsed);
        }
        if(EditorApplication.isPlaying) EditorGUILayout.HelpBox("Leave play mode to apply.", MessageType.Warning);
        if(!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, MessageType.None);
    }
}
