using System;
using UnityEngine;

[Serializable]
public class HyperbolicEdge
{
    public Vector2 pointA;
    public Vector2 pointB;

    public HyperbolicEdge(
        Vector2 pointA,
        Vector2 pointB
    )
    {
        this.pointA = pointA;
        this.pointB = pointB;
    }
}