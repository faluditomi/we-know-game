/// <summary>
/// Counts idle time while running and reports when the configured timeout is reached.
/// Plain model, free of Unity types, so it can be unit-tested; <see cref="MenuController"/> ticks it.
/// </summary>
public sealed class InactivityTimer
{
    private float timeoutSeconds;

    public InactivityTimer(float timeoutSeconds)
    {
        TimeoutSeconds = timeoutSeconds;
    }

    /// <summary>Seconds since the last reset, counted only while running.</summary>
    public float IdleSeconds { get; private set; }

    public float TimeoutSeconds
    {
        get => timeoutSeconds;
        set => timeoutSeconds = value > 0f ? value : 0.01f;
    }

    public bool Expired => IdleSeconds >= timeoutSeconds;

    /// <summary>Input happened: start counting again from zero.</summary>
    public void Reset() => IdleSeconds = 0f;

    /// <summary>Advances the idle time by <paramref name="deltaSeconds"/> when <paramref name="running"/>.</summary>
    /// <returns>True on the tick the timeout is reached; the timer then resets.</returns>
    public bool Tick(float deltaSeconds, bool running)
    {
        if(!running || deltaSeconds <= 0f) return false;
        IdleSeconds += deltaSeconds;
        if(!Expired) return false;
        Reset();
        return true;
    }
}
