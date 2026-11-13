using NUnit.Framework;

public class InactivityTimerTests
{
    [Test]
    public void ExpiresAfterTheTimeoutWhileRunning()
    {
        InactivityTimer timer = new InactivityTimer(60f);
        Assert.IsFalse(timer.Tick(59f, true));
        Assert.IsTrue(timer.Tick(1f, true));
    }

    [Test]
    public void DoesNotCountWhileStopped()
    {
        InactivityTimer timer = new InactivityTimer(10f);
        Assert.IsFalse(timer.Tick(100f, false));
        Assert.AreEqual(0f, timer.IdleSeconds);
    }

    [Test]
    public void ResetStartsCountingAgain()
    {
        InactivityTimer timer = new InactivityTimer(10f);
        timer.Tick(9f, true);
        timer.Reset();
        Assert.IsFalse(timer.Tick(9f, true));
        Assert.IsTrue(timer.Tick(1f, true));
    }

    [Test]
    public void ExpiryFiresOnceThenStartsOver()
    {
        InactivityTimer timer = new InactivityTimer(5f);
        Assert.IsTrue(timer.Tick(5f, true));
        Assert.AreEqual(0f, timer.IdleSeconds);
        Assert.IsFalse(timer.Tick(1f, true));
    }

    [Test]
    public void ChangingTheTimeoutAppliesToTheCurrentCount()
    {
        InactivityTimer timer = new InactivityTimer(60f);
        timer.Tick(20f, true);
        timer.TimeoutSeconds = 15f;
        Assert.IsTrue(timer.Expired);
    }
}
