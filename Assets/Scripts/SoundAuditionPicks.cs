using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Dev tooling: sound audition picks as plain text, one <c>CueName = clip_name</c> line per cue,
/// so picks made on a phone can be copied and imported in the Editor.
/// </summary>
public static class SoundAuditionPicks
{
    /// <summary>Clip name written for a silent cue.</summary>
    public const string None = "(none)";
    private const string Header = "# Sound audition picks";

    public enum Status
    {
        Ok,
        Malformed,
        UnknownCue,
        UnknownClip,
        AmbiguousClip
    }

    public readonly struct Line
    {
        public readonly int Number;
        public readonly string Text;
        public readonly Status Status;
        public readonly SoundCue Cue;
        public readonly string ClipName;
        /// <summary>The resolved clip; null for silence or when the line has an error.</summary>
        public readonly AudioClip Clip;

        public Line(int number, string text, Status status, SoundCue cue, string clipName, AudioClip clip)
        {
            Number = number;
            Text = text;
            Status = status;
            Cue = cue;
            ClipName = clipName;
            Clip = clip;
        }

        public bool IsOk => Status == Status.Ok;
        public bool IsSilence => IsOk && Clip == null;

        public string Message
        {
            get
            {
                switch(Status)
                {
                    case Status.Ok: return IsSilence ? $"{Cue}: silent" : $"{Cue}: {ClipName}";
                    case Status.Malformed: return $"Line {Number}: expected 'Cue = clip', got '{Text}'";
                    case Status.UnknownCue: return $"Line {Number}: unknown cue '{Text.Split('=')[0].Trim()}'";
                    case Status.UnknownClip: return $"Line {Number}: no clip named '{ClipName}'";
                    case Status.AmbiguousClip: return $"Line {Number}: more than one clip named '{ClipName}'";
                    default: return Text;
                }
            }
        }
    }

    public static string Format(IEnumerable<(SoundCue cue, AudioClip clip)> picks)
    {
        var builder = new StringBuilder();
        builder.Append(Header).Append('\n');
        foreach((SoundCue cue, AudioClip clip) in picks)
        {
            builder.Append(cue).Append(" = ").Append(clip != null ? clip.name : None).Append('\n');
        }
        return builder.ToString();
    }

    /// <summary>
    /// Parses picks text. Blank lines and lines starting with '#' are skipped.
    /// <paramref name="findClips"/> returns every clip with the given name.
    /// </summary>
    public static List<Line> Parse(string text, Func<string, IReadOnlyList<AudioClip>> findClips)
    {
        var lines = new List<Line>();
        if(string.IsNullOrEmpty(text)) return lines;

        string[] rows = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for(int i = 0; i < rows.Length; i++)
        {
            string row = rows[i].Trim();
            int number = i + 1;
            if(row.Length == 0 || row.StartsWith("#", StringComparison.Ordinal)) continue;

            int separator = row.IndexOf('=');
            if(separator <= 0)
            {
                lines.Add(new Line(number, row, Status.Malformed, default, null, null));
                continue;
            }

            string cueName = row.Substring(0, separator).Trim();
            string clipName = row.Substring(separator + 1).Trim();
            if(clipName.Length == 0)
            {
                lines.Add(new Line(number, row, Status.Malformed, default, null, null));
                continue;
            }

            if(!TryParseCue(cueName, out SoundCue cue))
            {
                lines.Add(new Line(number, row, Status.UnknownCue, default, clipName, null));
                continue;
            }

            if(string.Equals(clipName, None, StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(new Line(number, row, Status.Ok, cue, None, null));
                continue;
            }

            IReadOnlyList<AudioClip> matches = findClips != null ? findClips(clipName) : null;
            int count = matches?.Count ?? 0;
            if(count == 0) lines.Add(new Line(number, row, Status.UnknownClip, cue, clipName, null));
            else if(count > 1) lines.Add(new Line(number, row, Status.AmbiguousClip, cue, clipName, null));
            else lines.Add(new Line(number, row, Status.Ok, cue, clipName, matches[0]));
        }
        return lines;
    }

    private static bool TryParseCue(string name, out SoundCue cue)
    {
        // Enum.TryParse accepts numbers too; only real cue names count.
        foreach(SoundCue value in Enum.GetValues(typeof(SoundCue)))
        {
            if(string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase))
            {
                cue = value;
                return true;
            }
        }
        cue = default;
        return false;
    }
}
