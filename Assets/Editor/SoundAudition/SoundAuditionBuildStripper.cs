using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Removes the sound audition overlay from release (non-development) player builds. It runs before the build
/// collects scene dependencies, so the candidates asset and candidate-only clips stay out of the build.
/// </summary>
internal sealed class SoundAuditionBuildStripper : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if(report == null) return; // entering play mode, not a player build
        if((report.summary.options & BuildOptions.Development) != 0) return;

        int removed = 0;
        foreach(GameObject root in scene.GetRootGameObjects())
        {
            foreach(SoundAudition audition in root.GetComponentsInChildren<SoundAudition>(true))
            {
                if(audition == null) continue; // already gone with a removed parent
                Object.DestroyImmediate(audition.gameObject);
                removed++;
            }
        }

        if(removed > 0) Debug.Log($"Sound audition: removed {removed} overlay object(s) from '{scene.name}' for the release build.");
    }
}
