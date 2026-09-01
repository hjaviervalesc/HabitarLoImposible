using UnityEngine;

public class HyperbolicPointVisualizer : MonoBehaviour
{
    [Header("Coordenadas hiperbólicas")]
    public float x = 0f;
    public float y = 0f;

    [Header("Configuración visual")]
    public float diskScale = 5f;
    public float height = 0.5f;

    private void Start()
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        // Crear punto en el hiperboloide
        HyperbolicPoint point = new HyperbolicPoint(x, y);

        // Proyectar al disco de Poincaré
        Vector2 poincare = point.ToPoincareDisk();

        // Posición relativa al origen del sistema hiperbólico
        Vector3 localPosition = new Vector3(
            poincare.x * diskScale,
            height,
            poincare.y * diskScale
        );

        // Aplicar posición local
        transform.localPosition = localPosition;

        Debug.Log(
            "Hiperboloide: (" +
            point.x + ", " +
            point.y + ", " +
            point.z + ")" +
            " → Poincaré: (" +
            poincare.x + ", " +
            poincare.y + ")" +
            " → Unity local: " +
            localPosition
        );
    }
}