using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AABB
{
    public Vector3 Min { get { return min; } }
    public Vector3 Max { get { return max; } }
    public Vector3 Center { get { return (min + max) * 0.5f; } }

    private Vector3 min, max;

    public AABB(Vector3 min, Vector3 max)
    {
        this.min = Vector3.Min(min, max);
        this.max = Vector3.Max(min, max);
    }

    public AABB(Bounds bounds)
    {
        this.min = bounds.min;
        this.max = bounds.max;
    }

    public Bounds ToBounds()
    {
        return new Bounds(Center, Max - Min);
    }
}
