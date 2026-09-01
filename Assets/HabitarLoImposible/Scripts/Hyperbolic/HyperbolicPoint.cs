using UnityEngine;

public class HyperbolicPoint
{
    public float x;
    public float y;
    public float z;

    public HyperbolicPoint(float x, float y)
    {
        this.x = x;
        this.y = y;

        // El punto pertenece al hiperboloide:
        // x² + y² - z² = -1
        this.z = Mathf.Sqrt(
            1f + x * x + y * y
        );
    }

    public Vector2 ToPoincareDisk()
    {
        float u = x / (z + 1f);
        float v = y / (z + 1f);

        return new Vector2(u, v);
    }
}