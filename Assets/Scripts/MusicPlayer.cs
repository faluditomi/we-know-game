using UnityEngine;

/// <summary>
/// Plays the background music track on a loop, from launch, through menus and boards, fading in at the start.
/// Its loudness is the track volume times the player's Music volume setting; changing the setting applies at once.
/// The music keeps playing while the game is paused.
/// </summary>
[DisallowMultipleComponent]
public sealed class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip track;
    [Tooltip("Track loudness at full Music volume, so the music can sit under the sound effects.")]
    [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;
    [Tooltip("Seconds to fade in when the game starts.")]
    [Min(0f)] [SerializeField] private float fadeInSeconds = 2f;

    private AudioSource source;
    private GameSettings settings;
    private float fade = 1f;

    /// <summary>Follows the player's Music volume instead of playing at full volume.</summary>
    public void UseSettings(GameSettings gameSettings)
    {
        if(settings != null) settings.Changed -= ApplyVolume;
        settings = gameSettings;
        if(settings != null) settings.Changed += ApplyVolume;
        ApplyVolume();
    }

    private void Awake()
    {
        EnsureSource();
    }

    private void Start()
    {
        if(track == null) return;

        EnsureSource();
        source.clip = track;
        fade = fadeInSeconds > 0f ? 0f : 1f;
        ApplyVolume();
        source.Play();
    }

    private void Update()
    {
        if(fade >= 1f) return;

        // Unscaled, so the fade finishes even if the game is paused right away.
        fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeInSeconds));
        ApplyVolume();
    }

    private void OnDestroy()
    {
        if(settings != null) settings.Changed -= ApplyVolume;
    }

    private void ApplyVolume()
    {
        if(source == null) return;
        float musicVolume = settings != null ? settings.MusicVolume : 1f;
        source.volume = volume * musicVolume * fade;
    }

    private void EnsureSource()
    {
        if(source != null) return;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.priority = 0; // never dropped for sound effects
        source.ignoreListenerPause = true;
    }
}
