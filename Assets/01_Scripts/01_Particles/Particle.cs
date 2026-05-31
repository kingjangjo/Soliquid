using UnityEngine;

public class Particle
{
    public Vector3 position;
    public Vector3 prevPosition;
    public Vector3 velocity;
    public float density;
    public float nearDensity;

    public Particle(Vector3 pos)
    {
        position = pos;
        prevPosition = pos;
        velocity = Vector3.zero;
    }
}