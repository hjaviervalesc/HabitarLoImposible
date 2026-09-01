using UnityEngine;

public class HyperbolicVisualizer : MonoBehaviour
{
    [Header("Coordenadas hiperbólicas")]
    public float x = 0f;
    public float y = 0f;

    [Header("Escala visual del disco")]
    public float scale = 5f;

    private void Start()
    {
        // Creamos un punto del hiperboloide
        HyperbolicPoint point = new HyperbolicPoint(x, y);

        // Lo proyectamos al disco de Poincaré
        Vector2 poincarePoint = point.ToPoincareDisk();

        // Posición visual en Unity
        Vector3 unityPosition = new Vector3(
            poincarePoint.x * scale,
            0.5f,
            poincarePoint.y * scale
        );

        Debug.Log(
            "HIPERBOLOIDE\n" +
            "X: " + point.x + "\n" +
            "Y: " + point.y + "\n" +
            "Z: " + point.z + "\n\n" +

            "DISCO DE POINCARÉ\n" +
            "U: " + poincarePoint.x + "\n" +
            "V: " + poincarePoint.y + "\n\n" +

            "UNITY\n" +
            unityPosition
        );
    }
}