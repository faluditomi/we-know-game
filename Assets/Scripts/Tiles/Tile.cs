using UnityEngine;

[CreateAssetMenu(fileName = "Tile", menuName = "Scriptable Objects/Tile")]
public class Tile : ScriptableObject
{
    public string tileName;
    [Tooltip("English description shown when the pair is matched. Also used when a language has no description of its own.")]
    public string foundDescription;
    [Tooltip("Hungarian description. Leave empty to show the English one.")]
    public string foundDescriptionHungarian;
    public Sprite backLogo;
    [Tooltip("Hungarian version of the logo, for logos with text in them. Leave empty to show the main logo.")]
    public Sprite backLogoHungarian;
    public Color tileColour;

    /// <summary>The description in <paramref name="language"/>, or the English one when that language has none.</summary>
    public string GetDescription(GameLanguage language)
    {
        string localized = language == GameLanguage.Hungarian ? foundDescriptionHungarian : null;
        return string.IsNullOrWhiteSpace(localized) ? foundDescription : localized;
    }

    /// <summary>The logo for <paramref name="language"/>, or <see cref="backLogo"/> when that language has none.</summary>
    public Sprite GetLogo(GameLanguage language)
    {
        return language == GameLanguage.Hungarian && backLogoHungarian != null ? backLogoHungarian : backLogo;
    }
}
