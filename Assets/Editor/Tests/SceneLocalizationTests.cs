using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Catches menu labels that would stay English when the player picks another language.</summary>
public class SceneLocalizationTests
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";

    /// <summary>Texts that are intentionally not <see cref="LocalizedLabel"/>s: the English title, the pause glyph, and texts written from code.</summary>
    private static readonly HashSet<string> AllowList = new HashSet<string>
    {
        "Menu Canvas/SafeArea/Title Screen/Title Text",
        "Menu Canvas/SafeArea/Title Screen/Status Text",
        "Menu Canvas/SafeArea/Title Screen/Round Stats Panel/Round Stats",
        "Menu Canvas/SafeArea/Game HUD/Pause Button/Label",
        "Menu Canvas/SafeArea/Settings Screen/Panel/Inactivity Value",
        "Menu Canvas/SafeArea/Picker Screen/Panel/Message",
        // Preset sizes, written from code into each copy of these templates.
        "Menu Canvas/SafeArea/Picker Screen/Panel/Preset Buttons/Preset Button Template/Label",
        "Menu Canvas/SafeArea/Picker Screen/Preset List Panel/Preset Row Template/Label",
    };

    [Test]
    public void EveryMenuTextIsLocalizedOrAllowListed()
    {
        Scene scene = SceneManager.GetSceneByPath(MainScenePath);
        bool openedHere = !scene.isLoaded;
        if(openedHere) scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
        try
        {
            var missing = new List<string>();
            foreach(GameObject root in scene.GetRootGameObjects())
            {
                foreach(TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    // Dropdown captions and items show the languages' own names, filled from code.
                    if(text.GetComponentInParent<TMP_Dropdown>(true) != null) continue;
                    string path = PathOf(text.transform);
                    if(AllowList.Contains(path)) continue;

                    LocalizedLabel label = text.GetComponent<LocalizedLabel>();
                    if(label == null || string.IsNullOrEmpty(label.Key)) missing.Add(path);
                    else if(System.Array.IndexOf(LocalizedText.AllKeys, label.Key) < 0) missing.Add($"{path} (unknown key \"{label.Key}\")");
                }
            }

            Assert.IsEmpty(missing, "Texts without a LocalizedLabel:\n" + string.Join("\n", missing));
        }
        finally
        {
            if(openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static string PathOf(Transform transform)
    {
        string path = transform.name;
        for(Transform parent = transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
        return path;
    }
}
