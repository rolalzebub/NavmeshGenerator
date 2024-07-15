using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Plane
{

    public Vector3 Normal { get { return normal; } }
    public float Distance { get { return distance; } }

    private Vector3 normal;
    private float distance;

    public Plane(Vector3 normal, float distance)
    {
        this.normal = normal;
        this.distance = distance;
    }

}

