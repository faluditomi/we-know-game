using System;
using UnityEngine;

/// <summary>Moments in the game that play a sound effect.</summary>
public enum SoundCue
{
    TileFlip,
    TileFlipBack,
    Mismatch,
    Match,
    PairJoin,
    PairReturn,
    Victory,
    SuccessTitle,
    ButtonClick,
    ButtonBack,
    Pause,
    Resume,
    Toggle,
    SliderTick,
    DropdownSelect,
    PickerCell,
    BoardStart
}

/// <summary>
/// Plays short 2D sound effects at the player's Sound volume. A small pool of audio sources lets
/// cues overlap and gives each one its own slight pitch variation, so repeated taps don't sound mechanical.
/// Every cue is optional: one without a clip stays silent.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoundEffects : MonoBehaviour
{
    [Serializable]
    public sealed class Cue
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch offset either way, e.g. 0.05 = ±5%.")]
        [Range(0f, 0.3f)] public float pitchVariation;
    }

    [Header("Board")]
    [SerializeField] private Cue tileFlip = new Cue { pitchVariation = 0.06f };
    [SerializeField] private Cue tileFlipBack = new Cue();
    [SerializeField] private Cue mismatch = new Cue();
    [SerializeField] private Cue match = new Cue();
    [SerializeField] private Cue pairJoin = new Cue();
    [SerializeField] private Cue pairReturn = new Cue();
    [SerializeField] private Cue victory = new Cue();

    [Header("Menus")]
    [SerializeField] private Cue successTitle = new Cue();
    [SerializeField] private Cue buttonClick = new Cue();
    [SerializeField] private Cue buttonBack = new Cue();
    [SerializeField] private Cue pause = new Cue();
    [SerializeField] private Cue resume = new Cue();
    [SerializeField] private Cue toggle = new Cue();
    [SerializeField] private Cue sliderTick = new Cue();
    [SerializeField] private Cue dropdownSelect = new Cue();
    [SerializeField] private Cue pickerCell = new Cue { pitchVariation = 0.04f };
    [SerializeField] private Cue boardStart = new Cue();

    [Header("Playback")]
    [Tooltip("How many sounds can play at once.")]
    [Range(1, 16)] [SerializeField] private int voices = 6;
    [Tooltip("Shortest gap between two plays of the same cue, in seconds. Stops slider drags and fast taps from stacking up.")]
    [Min(0f)] [SerializeField] private float minRepeatInterval = 0.05f;

    private AudioSource[] sources = Array.Empty<AudioSource>();
    private int nextSource;
    private float[] lastPlayed;
    private GameSettings settings;

    private float MasterVolume => settings != null ? settings.SoundVolume : 1f;

    /// <summary>Plays at the player's Sound volume instead of full volume.</summary>
    public void UseSettings(GameSettings gameSettings)
    {
        settings = gameSettings;
    }

    private void Awake()
    {
        EnsureSources();
    }

    public void Play(SoundCue soundCue)
    {
        Cue cue = CueFor(soundCue);
        if(cue == null || cue.clip == null) return;

        float volume = cue.volume * MasterVolume;
        if(volume <= 0.0001f) return;

        int index = (int)soundCue;
        float now = Time.unscaledTime;
        if(lastPlayed[index] >= 0f && now - lastPlayed[index] < minRepeatInterval) return;
        lastPlayed[index] = now;

        EnsureSources();
        AudioSource source = sources[nextSource];
        nextSource = (nextSource + 1) % sources.Length;

        source.Stop();
        source.clip = cue.clip;
        source.volume = volume;
        source.pitch = 1f + (cue.pitchVariation > 0f ? UnityEngine.Random.Range(-cue.pitchVariation, cue.pitchVariation) : 0f);
        source.Play();
    }

    /// <summary>Stops every sound that is playing.</summary>
    public void StopAll()
    {
        foreach(AudioSource source in sources) if(source != null) source.Stop();
    }

    private void EnsureSources()
    {
        if(lastPlayed == null)
        {
            lastPlayed = new float[Enum.GetValues(typeof(SoundCue)).Length];
            for(int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -1f;
        }

        int count = Mathf.Max(1, voices);
        if(sources.Length == count) return;

        sources = new AudioSource[count];
        for(int i = 0; i < count; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            // Sounds keep playing while the game is paused (pause/resume clicks, settings over a paused board).
            source.ignoreListenerPause = true;
            sources[i] = source;
        }
        nextSource = 0;
    }

    private Cue CueFor(SoundCue soundCue)
    {
        switch(soundCue)
        {
            case SoundCue.TileFlip: return tileFlip;
            case SoundCue.TileFlipBack: return tileFlipBack;
            case SoundCue.Mismatch: return mismatch;
            case SoundCue.Match: return match;
            case SoundCue.PairJoin: return pairJoin;
            case SoundCue.PairReturn: return pairReturn;
            case SoundCue.Victory: return victory;
            case SoundCue.SuccessTitle: return successTitle;
            case SoundCue.ButtonClick: return buttonClick;
            case SoundCue.ButtonBack: return buttonBack;
            case SoundCue.Pause: return pause;
            case SoundCue.Resume: return resume;
            case SoundCue.Toggle: return toggle;
            case SoundCue.SliderTick: return sliderTick;
            case SoundCue.DropdownSelect: return dropdownSelect;
            case SoundCue.PickerCell: return pickerCell;
            case SoundCue.BoardStart: return boardStart;
            default: return null;
        }
    }
}
