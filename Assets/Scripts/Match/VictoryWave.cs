using System.Collections.Generic;
using UnityEngine;

/// <summary>Start delays for the victory board wave: tiles further from the last matched pair start later.</summary>
public static class VictoryWave
{
    /// <summary>
    /// Delay per position, from 0 for the position nearest <paramref name="origin"/> (scaled by distance)
    /// up to <paramref name="spread"/> seconds for the furthest one.
    /// </summary>
    public static float[] Delays(IReadOnlyList<Vector3> positions, Vector3 origin, float spread)
    {
        if(positions == null) return new float[0];
        float[] delays = new float[positions.Count];
        float maxDistance = 0f;
        for(int i = 0; i < positions.Count; i++)
        {
            delays[i] = Vector3.Distance(positions[i], origin);
            maxDistance = Mathf.Max(maxDistance, delays[i]);
        }

        spread = Mathf.Max(0f, spread);
        for(int i = 0; i < delays.Length; i++)
        {
            delays[i] = maxDistance > 0f ? spread * delays[i] / maxDistance : 0f;
        }
        return delays;
    }
}
