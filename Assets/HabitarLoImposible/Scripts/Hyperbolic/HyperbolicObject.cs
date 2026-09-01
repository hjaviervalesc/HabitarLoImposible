using UnityEngine;

public class HyperbolicObject : MonoBehaviour
{
    [Header("Coordenadas hiperbólicas")]
    public float x = 0f;
    public float y = 0f;

    [Header("Dirección hiperbólica")]
    public float direction = 0f;

    [Header("Representación del disco")]
    public float diskScale = 5f;

    [Header("Altura")]
    public float height = 0.5f;

    [Header("Escala del objeto")]
    public float baseScale = 1f;

    private void Start()
    {
        UpdateTransform();
    }

    public void UpdateTransform()
    {
        // Crear punto hiperbólico
        HyperbolicPoint point =
            new HyperbolicPoint(x, y);

        // Proyección al disco
        Vector2 poincare =
            point.ToPoincareDisk();

        // Posición
        transform.localPosition = new Vector3(
            poincare.x * diskScale,
            height,
            poincare.y * diskScale
        );

        // Escala visual
        float distanceFromCenter =
            poincare.magnitude;

        float visualScale =
            Mathf.Max(
                1f - distanceFromCenter,
                0.05f
            );

        transform.localScale =
            Vector3.one *
            baseScale *
            visualScale;

        // Orientación
        transform.localRotation =
            Quaternion.Euler(
                0f,
                direction,
                0f
            );
    }
}