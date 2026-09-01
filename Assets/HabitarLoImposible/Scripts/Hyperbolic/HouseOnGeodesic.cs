using UnityEngine;

public class HouseOnGeodesic : MonoBehaviour
{
    [Header("Carretera")]
    public HyperbolicGeodesic geodesic;

    [Header("Posición sobre la carretera")]
    [Range(0f, 1f)]
    public float positionOnRoad = 0.5f;

    [Header("Lado de la carretera")]
    public bool leftSide = true;

    [Header("Separación respecto a la carretera")]
    public float offset = 0.3f;

    [Header("Representación del disco")]
    public float diskScale = 5f;

    [Header("Altura")]
    public float height = 0.5f;

    [Header("Escala hiperbólica")]
    public bool useHyperbolicScale = true;

    [Range(0.1f, 2f)]
    public float hyperbolicScaleIntensity = 1f;


    private void Start()
    {
        PlaceHouse();
    }


    public void PlaceHouse()
    {
        if (geodesic == null)
        {
            Debug.LogError(
                "HouseOnGeodesic: No se ha asignado una geodésica."
            );

            return;
        }


        // ==================================================
        // 1. Punto de la carretera
        // ==================================================

        Vector2 roadPoint =
            geodesic.GetPointAt(positionOnRoad);


        // ==================================================
        // 2. Tangente de la carretera
        // ==================================================

        Vector2 tangent =
            geodesic.GetTangentAt(positionOnRoad);

        tangent.Normalize();


        // ==================================================
        // 3. Perpendicular a la carretera
        // ==================================================

        Vector2 normal =
            new Vector2(
                -tangent.y,
                tangent.x
            ).normalized;


        // ==================================================
        // 4. Elegir el lado de la carretera
        // ==================================================

        if (!leftSide)
        {
            normal = -normal;
        }


        // ==================================================
        // 5. Posición de la casa respecto a la carretera
        // ==================================================

        Vector2 housePoint =
            roadPoint + normal * offset;


        // ==================================================
        // 6. Convertir a Unity
        // ==================================================

        transform.localPosition =
            new Vector3(
                housePoint.x * diskScale,
                height,
                housePoint.y * diskScale
            );


        // ==================================================
        // 7. Dirección hacia la carretera
        // ==================================================

        Vector2 directionToRoad =
            -normal;


        // ==================================================
        // 8. Convertir dirección 2D a Unity
        // ==================================================

        Vector3 worldDirection =
            new Vector3(
                directionToRoad.x,
                0f,
                directionToRoad.y
            ).normalized;


        // ==================================================
        // 9. Orientación de la casa
        //
        // El frontal de la casa es -X local.
        //
        // Queremos que -X mire hacia la carretera.
        // ==================================================

        transform.localRotation =
            Quaternion.FromToRotation(
                Vector3.left,
                worldDirection
            );


        // ==================================================
        // 10. ESCALA HIPERBÓLICA
        // ==================================================

        if (useHyperbolicScale)
        {
            // Distancia al centro del disco.
            float r = housePoint.magnitude;


            // Evitamos problemas si por algún motivo
            // el punto está ligeramente fuera del disco.

            r = Mathf.Clamp(r, 0f, 0.9999f);


            // Factor de escala del modelo de Poincaré.
            //
            // Centro:
            // r = 0  → escala = 1
            //
            // Frontera:
            // r → 1  → escala → 0

            float scaleFactor =
                1f - r * r;


            // Podemos controlar la intensidad
            // del efecto desde el Inspector.

            scaleFactor =
                Mathf.Pow(
                    scaleFactor,
                    hyperbolicScaleIntensity
                );


            transform.localScale =
                Vector3.one * scaleFactor;
        }
        else
        {
            // Escala normal del asset.

            transform.localScale =
                Vector3.one;
        }
    }
}