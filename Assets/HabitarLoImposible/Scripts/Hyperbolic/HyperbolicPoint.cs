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

        // Hiperboloide: x² + y² - z² = -1
        z = Mathf.Sqrt(1f + x * x + y * y);
    }

    // Hiperboloide → disco de Poincaré
    public Vector2 ToPoincareDisk()
    {
        return new Vector2(
            x / (z + 1f),
            y / (z + 1f)
        );
    }

    // Disco de Poincaré → hiperboloide
    public static Vector2 PoincareToHyperboloid(Vector2 point)
    {
        float denominator = 1f - point.sqrMagnitude;

        if (denominator <= 0.000001f)
        {
            Debug.LogError(
                "El punto debe estar dentro del disco de Poincaré."
            );

            denominator = 0.000001f;
        }

        return new Vector2(
            2f * point.x / denominator,
            2f * point.y / denominator
        );
    }
}