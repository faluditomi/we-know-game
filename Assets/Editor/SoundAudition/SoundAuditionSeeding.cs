using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

/// <summary>
/// Seeds each cue's audition candidates from simple rules: clip name families, a duration limit and an order.
/// Each rule also proposes the clip the audition starts on, which seeding moves to the front.
/// The table is a starting point; edit it to taste. Seeding never removes candidates or touches shipping clips.
/// </summary>
public static class SoundAuditionSeeding
{
    public const int MaxSeededPerCue = 6;

    public enum Order
    {
        /// <summary>Shortest first: frequent, background cues.</summary>
        Shortest,
        /// <summary>Lowest RMS first: cues that must not feel harsh.</summary>
        Quietest,
        /// <summary>Highest spectral centroid first: bright, positive cues.</summary>
        Brightest,
        /// <summary>Longest first: rare, celebratory cues.</summary>
        Longest
    }

    public readonly struct Rule
    {
        public readonly SoundCue Cue;
        /// <summary>The clip the audition starts on for this cue; seeding puts it first.</summary>
        public readonly string Proposed;
        public readonly string[] Families;
        public readonly float MaxDuration;
        public readonly Order Order;

        public Rule(SoundCue cue, string proposed, float maxDuration, Order order, params string[] families)
        {
            Cue = cue;
            Proposed = proposed;
            MaxDuration = maxDuration;
            Order = order;
            Families = families;
        }
    }

    // Proposed clips were picked from the Kenney names and clip lengths, not by ear. Mirrored cues
    // (join/return, pause/resume) use the matching maximize/minimize pair so they sound like opposites.
    // Families: Kenney Interface Sounds (click, tick, glass, ...) and JDSherbert Ultimate UI SFX
    // (cancel, cursor, error, popup open, popup close, select, swipe). Durations are audible lengths.
    public static readonly Rule[] Rules =
    {
        // Board: heard constantly, so short and unobtrusive.
        new Rule(SoundCue.TileFlip, "click_002", 0.5f, Order.Shortest, "click", "cursor", "tick", "select", "pluck", "toggle"),
        new Rule(SoundCue.TileFlipBack, "back_001", 0.5f, Order.Shortest, "back", "cursor", "swipe", "tick", "click", "drop", "toggle"),
        new Rule(SoundCue.Mismatch, "drop_002", 0.8f, Order.Quietest, "drop", "cancel", "error", "pluck", "back", "close"),
        new Rule(SoundCue.Match, "confirmation_001", 1.0f, Order.Brightest, "confirmation", "popup open", "select", "glass", "pluck", "maximize", "bong"),
        new Rule(SoundCue.PairJoin, "maximize_003", 0.9f, Order.Brightest, "maximize", "swipe", "popup open", "glass", "pluck", "open"),
        new Rule(SoundCue.PairReturn, "minimize_003", 0.9f, Order.Quietest, "minimize", "swipe", "popup close", "drop", "close", "back"),
        new Rule(SoundCue.Victory, "confirmation_002", 3.0f, Order.Longest, "confirmation", "popup open", "select", "glass", "maximize", "bong"),

        // Menus.
        new Rule(SoundCue.SuccessTitle, "glass_004", 2.0f, Order.Longest, "glass", "popup open", "confirmation", "maximize", "open"),
        new Rule(SoundCue.ButtonClick, "select_001", 0.5f, Order.Shortest, "select", "cursor", "click", "tick", "toggle"),
        new Rule(SoundCue.ButtonBack, "back_002", 0.6f, Order.Shortest, "back", "cancel", "popup close", "close", "minimize", "click"),
        new Rule(SoundCue.Pause, "minimize_007", 0.8f, Order.Quietest, "minimize", "popup close", "close", "toggle", "switch"),
        new Rule(SoundCue.Resume, "maximize_007", 0.8f, Order.Brightest, "maximize", "popup open", "open", "toggle", "switch"),
        new Rule(SoundCue.Toggle, "toggle_001", 0.5f, Order.Shortest, "toggle", "cursor", "switch", "click"),
        new Rule(SoundCue.SliderTick, "tick_002", 0.3f, Order.Shortest, "tick", "cursor", "click", "glitch"),
        new Rule(SoundCue.DropdownSelect, "select_007", 0.6f, Order.Shortest, "select", "popup open", "cursor", "open", "click", "toggle"),
        new Rule(SoundCue.PickerCell, "tick_004", 0.4f, Order.Shortest, "tick", "cursor", "select", "click", "pluck"),
        new Rule(SoundCue.BoardStart, "maximize_004", 1.2f, Order.Brightest, "maximize", "popup open", "swipe", "open", "confirmation", "question")
    };

    /// <summary>Appends up to <see cref="MaxSeededPerCue"/> new candidates per cue. Returns a per-cue report.</summary>
    public static string Seed(SoundAuditionCandidates asset, IReadOnlyList<SoundClipAnalysis.ClipStats> stats)
    {
        Undo.RecordObject(asset, "Seed Sound Shortlists");
        var report = new StringBuilder();
        int total = 0;

        foreach(Rule rule in Rules)
        {
            int added = 0;
            SoundClipAnalysis.ClipStats proposed = stats.FirstOrDefault(s => s.Clip != null && string.Equals(s.Clip.name, rule.Proposed, System.StringComparison.OrdinalIgnoreCase));
            if(proposed != null)
            {
                if(asset.MoveToFront(rule.Cue, proposed.Clip)) added++;
            }
            else
            {
                report.Append(rule.Cue).Append(": proposed clip '").Append(rule.Proposed).Append("' not found\n");
            }

            foreach(SoundClipAnalysis.ClipStats clip in Shortlist(rule, stats))
            {
                if(added >= MaxSeededPerCue) break;
                if(asset.Add(rule.Cue, clip.Clip)) added++;
            }
            total += added;
            report.Append(rule.Cue).Append(": +").Append(added).Append(", starts on ").Append(rule.Proposed);
            if(added == 0) report.Append("  (no new matches; widen the rule?)");
            report.Append('\n');
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return $"Added {total} candidate(s).\n{report}";
    }

    /// <summary>Matching clips, sorted per family by the rule's order, then interleaved across families for variety.</summary>
    public static List<SoundClipAnalysis.ClipStats> Shortlist(Rule rule, IReadOnlyList<SoundClipAnalysis.ClipStats> stats)
    {
        var perFamily = new List<List<SoundClipAnalysis.ClipStats>>();
        foreach(string family in rule.Families)
        {
            IEnumerable<SoundClipAnalysis.ClipStats> matches = stats.Where(s =>
                s.Measured && s.Clip != null && s.Family == family && s.AudibleDuration <= rule.MaxDuration);
            perFamily.Add(Sort(matches, rule.Order).ToList());
        }

        var result = new List<SoundClipAnalysis.ClipStats>();
        for(int round = 0; perFamily.Any(list => round < list.Count); round++)
        {
            foreach(List<SoundClipAnalysis.ClipStats> list in perFamily)
            {
                if(round < list.Count) result.Add(list[round]);
            }
        }
        return result;
    }

    private static IEnumerable<SoundClipAnalysis.ClipStats> Sort(IEnumerable<SoundClipAnalysis.ClipStats> clips, Order order)
    {
        switch(order)
        {
            case Order.Quietest: return clips.OrderBy(s => s.RmsDb);
            case Order.Brightest: return clips.OrderByDescending(s => s.BrightnessHz);
            case Order.Longest: return clips.OrderByDescending(s => s.AudibleDuration);
            default: return clips.OrderBy(s => s.AudibleDuration);
        }
    }
}
