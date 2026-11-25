using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dev tooling: candidate clips to audition for each sound cue. Kept apart from <see cref="SoundEffects"/>
/// so candidate clips never reach release builds (only the stripped-out <see cref="SoundAudition"/> references this asset).
/// </summary>
public sealed class SoundAuditionCandidates : ScriptableObject
{
    [Serializable]
    public sealed class CueCandidates
    {
        public SoundCue cue;
        public List<AudioClip> clips = new List<AudioClip>();
    }

    [SerializeField] private List<CueCandidates> cues = new List<CueCandidates>();

    /// <summary>Every candidate for the cue, in list order (entries for the same cue are joined).</summary>
    public List<AudioClip> For(SoundCue cue)
    {
        var result = new List<AudioClip>();
        foreach(CueCandidates entry in cues)
        {
            if(entry == null || entry.cue != cue || entry.clips == null) continue;
            foreach(AudioClip clip in entry.clips)
            {
                if(clip != null) result.Add(clip);
            }
        }
        return result;
    }

    public bool Contains(SoundCue cue, AudioClip clip)
    {
        return clip != null && For(cue).Contains(clip);
    }

    /// <summary>Makes the clip the cue's first candidate, moving it if it is already listed. Returns whether it was newly added.</summary>
    public bool MoveToFront(SoundCue cue, AudioClip clip)
    {
        if(clip == null) return false;

        bool existed = false;
        CueCandidates first = null;
        foreach(CueCandidates entry in cues)
        {
            if(entry == null || entry.cue != cue) continue;
            if(entry.clips == null) entry.clips = new List<AudioClip>();
            if(first == null) first = entry;
            existed |= entry.clips.RemoveAll(c => c == clip) > 0;
        }

        if(first == null)
        {
            first = new CueCandidates { cue = cue };
            cues.Add(first);
        }
        first.clips.Insert(0, clip);
        return !existed;
    }

    /// <summary>Appends the clip to the cue's candidates unless it is already there. Returns whether it was added.</summary>
    public bool Add(SoundCue cue, AudioClip clip)
    {
        if(clip == null || Contains(cue, clip)) return false;

        CueCandidates entry = cues.Find(e => e != null && e.cue == cue);
        if(entry == null)
        {
            entry = new CueCandidates { cue = cue };
            cues.Add(entry);
        }
        if(entry.clips == null) entry.clips = new List<AudioClip>();
        entry.clips.Add(clip);
        return true;
    }
}
