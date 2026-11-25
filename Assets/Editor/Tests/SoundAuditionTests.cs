using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SoundAuditionTests
{
    private readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach(Object obj in created) if(obj != null) Object.DestroyImmediate(obj);
        created.Clear();
    }

    private AudioClip Clip(string name)
    {
        AudioClip clip = AudioClip.Create(name, 441, 1, 44100, false);
        created.Add(clip);
        return clip;
    }

    // ---- Choices ---------------------------------------------------------------------------

    [Test]
    public void ChoicesAreShippingThenCandidatesThenSilence()
    {
        AudioClip a = Clip("a"), b = Clip("b"), c = Clip("c");
        var model = new SoundAuditionModel(_ => a, _ => new[] { b, c });
        SoundAuditionModel.CueChoices choices = model[SoundCue.TileFlip];

        CollectionAssert.AreEqual(new[] { a, b, c, null }, choices.Choices);
        Assert.AreSame(a, choices.Active);
        Assert.IsTrue(choices.IsShipping);
    }

    [Test]
    public void DuplicateCandidatesAppearOnce()
    {
        AudioClip a = Clip("a"), b = Clip("b");
        List<AudioClip> choices = SoundAuditionModel.BuildChoices(a, new[] { a, b, null, b });
        CollectionAssert.AreEqual(new[] { a, b, null }, choices);
    }

    [Test]
    public void CueWithNoClipsOffersOnlySilence()
    {
        var model = new SoundAuditionModel(_ => null, _ => null);
        SoundAuditionModel.CueChoices choices = model[SoundCue.Victory];

        Assert.AreEqual(1, choices.Choices.Count);
        Assert.IsNull(choices.Active);
        choices.Step(1);
        Assert.IsNull(choices.Active);
    }

    [Test]
    public void CueWithoutShippingClipStartsOnFirstCandidate()
    {
        AudioClip b = Clip("b"), c = Clip("c");
        var model = new SoundAuditionModel(_ => null, _ => new[] { b, c });
        SoundAuditionModel.CueChoices choices = model[SoundCue.Match];

        CollectionAssert.AreEqual(new[] { b, c, null }, choices.Choices);
        Assert.AreSame(b, choices.Active);
        Assert.IsFalse(choices.IsShipping, "the shipping choice of a silent cue is silence");
        choices.Step(-1);
        Assert.IsNull(choices.Active);
        Assert.IsTrue(choices.IsShipping);
        choices.ResetToStart();
        Assert.AreSame(b, choices.Active);
    }

    [Test]
    public void SteppingWrapsBothWays()
    {
        AudioClip a = Clip("a"), b = Clip("b");
        var model = new SoundAuditionModel(_ => a, _ => new[] { b });
        SoundAuditionModel.CueChoices choices = model[SoundCue.Mismatch];

        choices.Step(-1);
        Assert.IsNull(choices.Active, "previous from the first choice wraps to silence");
        choices.Step(1);
        Assert.AreSame(a, choices.Active, "next from the last choice wraps to the first");
        choices.Step(5);
        Assert.AreEqual(5 % 3, choices.ActiveIndex);
        choices.ResetToStart();
        Assert.AreSame(a, choices.Active);
    }

    [Test]
    public void ModelCoversEveryCue()
    {
        var model = new SoundAuditionModel(_ => null, _ => null);
        Assert.AreEqual(Enum.GetValues(typeof(SoundCue)).Length, model.All.Count);
        foreach(SoundCue cue in Enum.GetValues(typeof(SoundCue))) Assert.AreEqual(cue, model[cue].Cue);
    }

    // ---- Picks text ------------------------------------------------------------------------

    [Test]
    public void PicksRoundTrip()
    {
        AudioClip tick = Clip("tick_002"), glass = Clip("glass_001");
        var clips = new Dictionary<string, AudioClip> { { tick.name, tick }, { glass.name, glass } };

        string text = SoundAuditionPicks.Format(new[] { (SoundCue.TileFlip, tick), (SoundCue.Match, glass), (SoundCue.Mismatch, (AudioClip)null) });
        StringAssert.Contains("TileFlip = tick_002", text);
        StringAssert.Contains("Mismatch = (none)", text);

        List<SoundAuditionPicks.Line> lines = SoundAuditionPicks.Parse(text, name => clips.TryGetValue(name, out AudioClip c) ? new[] { c } : null);
        Assert.AreEqual(3, lines.Count);
        Assert.IsTrue(lines.TrueForAll(l => l.IsOk));
        Assert.AreSame(tick, lines[0].Clip);
        Assert.AreEqual(SoundCue.Match, lines[1].Cue);
        Assert.IsTrue(lines[2].IsSilence);
    }

    [Test]
    public void PicksParseReportsErrors()
    {
        AudioClip one = Clip("dup"), two = Clip("dup");
        string text = "# comment\n\nNotACue = tick_001\nTileFlip = missing_clip\nMatch = dup\njust some words\nVictory =\n1 = tick\r\nbuttonclick = (NONE)";

        List<SoundAuditionPicks.Line> lines = SoundAuditionPicks.Parse(text, name => name == "dup" ? new[] { one, two } : null);

        Assert.AreEqual(SoundAuditionPicks.Status.UnknownCue, lines[0].Status);
        Assert.AreEqual(SoundAuditionPicks.Status.UnknownClip, lines[1].Status);
        Assert.AreEqual(SoundAuditionPicks.Status.AmbiguousClip, lines[2].Status);
        Assert.AreEqual(SoundAuditionPicks.Status.Malformed, lines[3].Status);
        Assert.AreEqual(SoundAuditionPicks.Status.Malformed, lines[4].Status);
        Assert.AreEqual(SoundAuditionPicks.Status.UnknownCue, lines[5].Status, "numbers are not cue names");
        Assert.IsTrue(lines[6].IsSilence);
        Assert.AreEqual(SoundCue.ButtonClick, lines[6].Cue);
        Assert.AreEqual(4, lines[1].Number);
    }

    // ---- SoundEffects hook -----------------------------------------------------------------

    private SoundEffects CreateSoundEffects()
    {
        var gameObject = new GameObject("SoundEffectsTest");
        created.Add(gameObject);
        return gameObject.AddComponent<SoundEffects>();
    }

    [Test]
    public void SoundEffectsPlaysShippingClipUnlessOverridden()
    {
        SoundEffects soundEffects = CreateSoundEffects();
        AudioClip shipping = Clip("shipping"), audition = Clip("audition");
        var serialized = new SerializedObject(soundEffects);
        serialized.FindProperty("tileFlip.clip").objectReferenceValue = shipping;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Assert.AreSame(shipping, soundEffects.EffectiveClip(SoundCue.TileFlip));
        Assert.IsFalse(soundEffects.HasAuditionClip(SoundCue.TileFlip));

        soundEffects.SetAuditionClip(SoundCue.TileFlip, audition);
        Assert.AreSame(audition, soundEffects.EffectiveClip(SoundCue.TileFlip));
        Assert.AreSame(shipping, soundEffects.ShippingClip(SoundCue.TileFlip));

        soundEffects.SetAuditionClip(SoundCue.TileFlip, null);
        Assert.IsNull(soundEffects.EffectiveClip(SoundCue.TileFlip), "a null override is silence");

        soundEffects.ClearAuditionClips();
        Assert.AreSame(shipping, soundEffects.EffectiveClip(SoundCue.TileFlip));
    }

    [Test]
    public void SoundEffectsReportsSilentCuesAsPlayed()
    {
        SoundEffects soundEffects = CreateSoundEffects();
        var played = new List<SoundCue>();
        soundEffects.Played += played.Add;

        soundEffects.Play(SoundCue.Victory);

        CollectionAssert.AreEqual(new[] { SoundCue.Victory }, played);
    }

    [Test]
    public void EveryCueHasASoundEffectsField()
    {
        var serialized = new SerializedObject(CreateSoundEffects());
        foreach(SoundCue cue in Enum.GetValues(typeof(SoundCue)))
        {
            Assert.IsNotNull(serialized.FindProperty(SoundAuditionEditor.CueFieldName(cue) + ".clip"), cue.ToString());
        }
    }

    // ---- Analyzer --------------------------------------------------------------------------

    [Test]
    public void MeasuresASineWave()
    {
        const int rate = 44100;
        var samples = new float[rate / 2];
        for(int i = 0; i < samples.Length; i++) samples[i] = 0.5f * Mathf.Sin(2f * Mathf.PI * 1000f * i / rate);

        SoundClipAnalysis.Measure(samples, rate, out float audible, out float peak, out float rms, out float brightness);

        Assert.AreEqual(-6.02f, peak, 0.1f);
        Assert.AreEqual(-9.03f, rms, 0.1f);
        Assert.AreEqual(1000f, brightness, 80f);
        Assert.AreEqual(0.5f, audible, 0.01f);
    }

    [Test]
    public void AudibleLengthIgnoresASilentTail()
    {
        const int rate = 44100;
        var samples = new float[rate * 2]; // 0.25 s of tone, then silence up to 2 s
        for(int i = 0; i < rate / 4; i++) samples[i] = 0.8f * Mathf.Sin(2f * Mathf.PI * 440f * i / rate);

        SoundClipAnalysis.Measure(samples, rate, out float audible, out float _, out float rms, out float _);

        Assert.AreEqual(0.25f, audible, 0.01f);
        Assert.AreEqual(SoundClipAnalysis.ToDb(0.8f / Mathf.Sqrt(2f)), rms, 0.2f, "RMS covers only the audible part");
    }

    [TestCase("click_003", "click")]
    [TestCase("Glass-12", "glass")]
    [TestCase("bong", "bong")]
    [TestCase("error_008", "error")]
    [TestCase("JDSherbert - Ultimate UI SFX Pack - Popup Open - 1", "popup open")]
    [TestCase("JDSherbert - Ultimate UI SFX Pack - Cursor - 5", "cursor")]
    public void ClipFamilyDropsTheNumber(string clipName, string family)
    {
        Assert.AreEqual(family, SoundClipAnalysis.FamilyOf(clipName));
    }

    [Test]
    public void SeedingRulesCoverEveryCue()
    {
        var covered = new HashSet<SoundCue>();
        foreach(SoundAuditionSeeding.Rule rule in SoundAuditionSeeding.Rules) covered.Add(rule.Cue);
        Assert.AreEqual(Enum.GetValues(typeof(SoundCue)).Length, covered.Count);
    }

    [Test]
    public void MoveToFrontInsertsOrMovesWithoutRemoving()
    {
        var asset = ScriptableObject.CreateInstance<SoundAuditionCandidates>();
        created.Add(asset);
        AudioClip a = Clip("a"), b = Clip("b"), c = Clip("c");
        asset.Add(SoundCue.Pause, a);
        asset.Add(SoundCue.Pause, b);

        Assert.IsFalse(asset.MoveToFront(SoundCue.Pause, b), "already listed: moved, not added");
        CollectionAssert.AreEqual(new[] { b, a }, asset.For(SoundCue.Pause));

        Assert.IsTrue(asset.MoveToFront(SoundCue.Pause, c));
        CollectionAssert.AreEqual(new[] { c, b, a }, asset.For(SoundCue.Pause));

        Assert.IsTrue(asset.MoveToFront(SoundCue.Resume, a), "a cue with no entry yet gets one");
        CollectionAssert.AreEqual(new[] { a }, asset.For(SoundCue.Resume));
    }

    [Test]
    public void EveryProposedClipExists()
    {
        Dictionary<string, List<AudioClip>> clips = SoundAuditionEditor.ClipsByName();
        foreach(SoundAuditionSeeding.Rule rule in SoundAuditionSeeding.Rules)
        {
            Assert.IsTrue(clips.TryGetValue(rule.Proposed, out List<AudioClip> matches) && matches.Count == 1,
                $"{rule.Cue}: proposed clip '{rule.Proposed}' is missing or ambiguous");
        }
    }

    [Test]
    public void PreferredVariantIsMonoOggThenWav()
    {
        string pack = "Assets/Sounds/Pack/";
        Assert.Less(SoundClipAnalysis.VariantRank(pack + "Mono/ogg/a.ogg"), SoundClipAnalysis.VariantRank(pack + "Mono/wav (SD)/a.wav"));
        Assert.Less(SoundClipAnalysis.VariantRank(pack + "Mono/wav (SD)/a.wav"), SoundClipAnalysis.VariantRank(pack + "Mono/wav (HD)/a.wav"));
        Assert.Less(SoundClipAnalysis.VariantRank(pack + "Mono/wav (HD)/a.wav"), SoundClipAnalysis.VariantRank(pack + "Mono/mp3/a.mp3"));
        Assert.Less(SoundClipAnalysis.VariantRank(pack + "Mono/mp3/a.mp3"), SoundClipAnalysis.VariantRank(pack + "Stereo/ogg/a.ogg"));
    }

    [Test]
    public void SameNamedClipsKeepOnlyThePreferredVariant()
    {
        AudioClip monoOgg = Clip("Cursor - 1"), stereoOgg = Clip("Cursor - 1"), monoWav = Clip("Cursor - 1");
        AudioClip kenneyA = Clip("tick_001"), kenneyB = Clip("tick_001");

        List<(AudioClip clip, string path)> kept = SoundClipAnalysis.PreferredVariants(new[]
        {
            (stereoOgg, "Assets/Sounds/P/Stereo/ogg/Cursor - 1.ogg"),
            (monoWav, "Assets/Sounds/P/Mono/wav (SD)/Cursor - 1.wav"),
            (monoOgg, "Assets/Sounds/P/Mono/ogg/Cursor - 1.ogg"),
            (kenneyA, "Assets/Sounds/a/tick_001.ogg"),
            (kenneyB, "Assets/Sounds/b/tick_001.ogg")
        });

        CollectionAssert.AreEquivalent(new[] { monoOgg, kenneyA, kenneyB }, kept.ConvertAll(k => k.clip),
            "a tie on rank keeps both, so the name stays ambiguous");
    }
}
