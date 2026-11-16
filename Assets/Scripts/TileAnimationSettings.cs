using System;
using UnityEngine;

/// <summary>Tunable timing and intensity for tile selection, mismatch and match presentation.</summary>
[Serializable]
public sealed class TileAnimationSettings
{
    [Header("Reveal / conceal")]
    [Min(0.01f)] public float flipDuration = 0.3f;
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Incorrect pair")]
    [Tooltip("Time both logos stay readable before the incorrect feedback starts.")]
    [Min(0f)] public float mismatchRevealHold = 0.5f;
    [Min(0.01f)] public float shakeDuration = 0.45f;
    [Tooltip("Shake displacement as a fraction of the tile width.")]
    [Min(0f)] public float shakeAmplitude = 0.08f;
    [Min(0f)] public float shakeFrequency = 22f;
    public Color incorrectTint = new Color(1f, 0.25f, 0.25f, 1f);
    [Range(0f, 1f)] public float incorrectTintStrength = 0.75f;

    [Header("Correct pair presentation")]
    [Min(0.01f)] public float presentDuration = 0.9f;
    public AnimationCurve presentCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Full turns performed while the tiles fly towards the camera (at least half a turn is always used).")]
    [Min(0)] public int presentSpins = 2;
    [Min(0.01f)] public float joinDuration = 0.25f;
    public AnimationCurve joinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Distance in front of the camera at which presented tiles rest. Raised automatically if it would clip the near plane.")]
    [Min(0.01f)] public float presentationDepth = 4f;
    [Tooltip("Presented tile height as a fraction of the visible screen height. Applied as explicit scale, independent of projection.")]
    [Range(0.05f, 0.9f)] public float presentationTileScreenHeight = 0.3f;

    [Header("Return to board")]
    [Min(0.01f)] public float returnDuration = 0.6f;
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Victory moment")]
    [Tooltip("Seconds each tile takes to hop, spin once and land during the board wave.")]
    [Min(0.05f)] public float victoryHopDuration = 0.55f;
    [Tooltip("How far tiles hop towards the camera, in tile heights.")]
    [Min(0f)] public float victoryHopHeight = 0.35f;
    [Tooltip("Extra scale at the top of the hop.")]
    [Range(0f, 0.3f)] public float victoryHopScale = 0.12f;
    [Tooltip("Delay between the last matched pair and the furthest tile starting to hop.")]
    [Min(0f)] public float victoryWaveSpread = 0.7f;
    [Tooltip("Shortest total length of the victory moment, in seconds.")]
    [Min(0f)] public float victoryMinDuration = 1.6f;
    [Tooltip("Strength of the tile screen surge (brightness and glitch).")]
    [Range(0f, 1f)] public float victorySurgeStrength = 1f;
    [Tooltip("Camera zoom-in at the peak of the punch, as a fraction of the field of view.")]
    [Range(0f, 0.2f)] public float victoryPunch = 0.05f;
    [Min(0.05f)] public float victoryPunchDuration = 0.45f;
    [Tooltip("Confetti depth as a fraction of the camera's distance to the board.")]
    [Range(0.2f, 0.95f)] public float victoryConfettiDepth = 0.7f;

    public void Validate()
    {
        flipCurve = EnsureCurve(flipCurve);
        presentCurve = EnsureCurve(presentCurve);
        joinCurve = EnsureCurve(joinCurve);
        returnCurve = EnsureCurve(returnCurve);
    }

    public static float Evaluate(AnimationCurve curve, float t)
    {
        t = Mathf.Clamp01(t);
        return curve == null || curve.length == 0 ? t : curve.Evaluate(t);
    }

    private static AnimationCurve EnsureCurve(AnimationCurve curve)
    {
        return curve == null || curve.length == 0 ? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f) : curve;
    }
}
