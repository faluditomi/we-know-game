using UnityEngine;

/// <summary>
/// The confetti of the victory moment: one pre-built particle system that fires two "cannons", one from each
/// lower side of the view, up and inward so the streams arc over the board. Nothing is created or destroyed
/// at runtime. Driven by <see cref="GameManager"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class VictoryEffects : MonoBehaviour
{
    [SerializeField] private ParticleSystem confetti;
    [Tooltip("Particles fired by each of the two cannons.")]
    [Min(1)] [SerializeField] private int particlesPerCannon = 110;
    [Tooltip("Cannon height below the screen centre, as a fraction of the visible height at the confetti depth.")]
    [Range(0f, 0.6f)] [SerializeField] private float cannonDrop = 0.45f;
    [Tooltip("Upward launch speed range, in visible heights per second.")]
    [SerializeField] private Vector2 upSpeed = new Vector2(1.2f, 2.5f);
    [Tooltip("Inward launch speed range, in visible widths per second.")]
    [SerializeField] private Vector2 inwardSpeed = new Vector2(0.12f, 0.65f);

    private void Awake()
    {
        if(confetti != null) confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    /// <summary>Fires both cannons, <paramref name="depth"/> in front of <paramref name="camera"/>.</summary>
    public void Burst(Camera camera, float depth)
    {
        if(confetti == null || camera == null) return;

        float visibleHeight = BoardFraming.VisibleHeightAtDepth(depth, camera.fieldOfView, camera.orthographic, camera.orthographicSize);
        float visibleWidth = visibleHeight * camera.aspect;
        Transform view = camera.transform;
        Vector3 centre = view.position + view.forward * depth;

        confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        confetti.Play(true);
        FireCannon(centre, view, visibleWidth, visibleHeight, -1f);
        FireCannon(centre, view, visibleWidth, visibleHeight, 1f);
    }

    /// <param name="side">-1 for the left cannon, 1 for the right one.</param>
    private void FireCannon(Vector3 centre, Transform view, float visibleWidth, float visibleHeight, float side)
    {
        Vector3 muzzle = centre + view.right * side * visibleWidth * 0.5f - view.up * visibleHeight * cannonDrop;
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();

        for(int i = 0; i < particlesPerCannon; i++)
        {
            emit.position = muzzle + Random.insideUnitSphere * visibleHeight * 0.03f;
            emit.velocity = view.up * visibleHeight * Random.Range(upSpeed.x, upSpeed.y)
                - view.right * side * visibleWidth * Random.Range(inwardSpeed.x, inwardSpeed.y)
                + view.forward * Random.Range(-0.3f, 0.3f);
            confetti.Emit(emit, 1);
        }
    }

    public void SetPaused(bool paused)
    {
        if(confetti == null || (!confetti.isPlaying && !confetti.isPaused)) return;
        if(paused) confetti.Pause(true);
        else confetti.Play(true);
    }

    public void Clear()
    {
        if(confetti != null) confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
