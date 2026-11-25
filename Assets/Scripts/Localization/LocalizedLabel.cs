using TMPro;
using UnityEngine;

/// <summary>
/// Shows a <see cref="LocalizedText"/> entry in the TMP label on the same GameObject.
/// <see cref="MenuController"/> finds every label in the scene and applies the current language.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedLabel : MonoBehaviour
{
    [Tooltip("LocalizedText key, for example \"Play\" or \"Settings\".")]
    [SerializeField] private string key;

    public string Key => key;

    public void Apply(GameLanguage language)
    {
        if(string.IsNullOrEmpty(key)) return;
        if(TryGetComponent(out TMP_Text label)) label.text = LocalizedText.Get(language, key);
    }
}
