using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dev tooling: the clips each sound cue can be auditioned with, and which one is active.
/// A cue's choices are its shipping clip (if any), then its distinct candidates in order, then silence (null).
/// The shipping clip is active at the start; a cue without one starts on its first candidate (or silence if it has none).
/// </summary>
public sealed class SoundAuditionModel
{
    public sealed class CueChoices
    {
        private readonly List<AudioClip> choices;

        public SoundCue Cue { get; }
        public IReadOnlyList<AudioClip> Choices => choices;
        public int ActiveIndex { get; private set; }
        public int ShippingIndex { get; }

        /// <summary>The active clip; null means the cue is silent.</summary>
        public AudioClip Active => choices[ActiveIndex];
        public bool IsShipping => ActiveIndex == ShippingIndex;

        internal CueChoices(SoundCue cue, AudioClip shipping, IEnumerable<AudioClip> candidates)
        {
            Cue = cue;
            choices = BuildChoices(shipping, candidates);
            ShippingIndex = shipping != null ? 0 : choices.Count - 1;
            // Start on the shipping clip, or on the first candidate when the cue ships silent.
            ActiveIndex = 0;
        }

        /// <summary>Moves the active choice by <paramref name="delta"/>, wrapping at both ends.</summary>
        public void Step(int delta)
        {
            int count = choices.Count;
            ActiveIndex = ((ActiveIndex + delta) % count + count) % count;
        }

        /// <summary>Back to the starting choice: the shipping clip, or the first candidate if the cue ships silent.</summary>
        public void ResetToStart()
        {
            ActiveIndex = 0;
        }
    }

    private readonly CueChoices[] cues;

    public SoundAuditionModel(Func<SoundCue, AudioClip> shippingClip, Func<SoundCue, IEnumerable<AudioClip>> candidates)
    {
        Array values = Enum.GetValues(typeof(SoundCue));
        cues = new CueChoices[values.Length];
        foreach(SoundCue cue in values)
        {
            cues[(int)cue] = new CueChoices(cue, shippingClip?.Invoke(cue), candidates?.Invoke(cue));
        }
    }

    public IReadOnlyList<CueChoices> All => cues;

    public CueChoices this[SoundCue cue] => cues[(int)cue];

    /// <summary>The active clip of every cue (null for silence), in cue order.</summary>
    public IEnumerable<(SoundCue cue, AudioClip clip)> Picks()
    {
        foreach(CueChoices choices in cues) yield return (choices.Cue, choices.Active);
    }

    /// <summary>Shipping clip first, then distinct candidates in order, then silence (a trailing null).</summary>
    public static List<AudioClip> BuildChoices(AudioClip shipping, IEnumerable<AudioClip> candidates)
    {
        var result = new List<AudioClip>();
        if(shipping != null) result.Add(shipping);
        if(candidates != null)
        {
            foreach(AudioClip clip in candidates)
            {
                if(clip != null && !result.Contains(clip)) result.Add(clip);
            }
        }
        result.Add(null);
        return result;
    }
}
