using NUnit.Framework;
using UnityEngine;

public class VictoryTests
{
    [Test]
    public void StatsLineInEnglish()
    {
        Assert.AreEqual("8 pairs · 1:32 · 3 misses", RoundStatsText.Format(GameLanguage.English, new RoundStats(8, 92f, 3)));
    }

    [Test]
    public void StatsLineInHungarian()
    {
        Assert.AreEqual("8 pár · 1:32 · 3 tévesztés", RoundStatsText.Format(GameLanguage.Hungarian, new RoundStats(8, 92f, 3)));
    }

    [Test]
    public void SingularWordingInEnglish()
    {
        Assert.AreEqual("1 pair · 0:09 · 1 miss", RoundStatsText.Format(GameLanguage.English, new RoundStats(1, 9.9f, 1)));
        StringAssert.EndsWith("0 misses", RoundStatsText.Format(GameLanguage.English, new RoundStats(2, 5f, 0)));
    }

    [TestCase(0f, "0:00")]
    [TestCase(59.99f, "0:59")]
    [TestCase(60f, "1:00")]
    [TestCase(605.4f, "10:05")]
    [TestCase(-3f, "0:00")]
    public void TimeIsMinutesAndWholeSeconds(float seconds, string expected)
    {
        Assert.AreEqual(expected, RoundStatsText.FormatTime(seconds));
    }

    [Test]
    public void WaveStartsAtTheOriginAndRipplesOutward()
    {
        Vector3[] positions = { new Vector3(3f, 0f, 0f), Vector3.zero, new Vector3(1f, 0f, 0f), new Vector3(0f, 2f, 0f) };
        float[] delays = VictoryWave.Delays(positions, Vector3.zero, 0.7f);

        Assert.AreEqual(0f, delays[1], 1e-5f, "the last pair starts first");
        Assert.AreEqual(0.7f, delays[0], 1e-5f, "the furthest tile starts last, at the full spread");
        Assert.Less(delays[2], delays[3]);
        Assert.Less(delays[3], delays[0]);
    }

    [Test]
    public void WaveHandlesDegenerateBoards()
    {
        Assert.AreEqual(new[] { 0f }, VictoryWave.Delays(new[] { Vector3.one }, Vector3.one, 0.7f));
        Assert.IsEmpty(VictoryWave.Delays(new Vector3[0], Vector3.zero, 0.7f));
        Assert.IsEmpty(VictoryWave.Delays(null, Vector3.zero, 0.7f));
    }
}
