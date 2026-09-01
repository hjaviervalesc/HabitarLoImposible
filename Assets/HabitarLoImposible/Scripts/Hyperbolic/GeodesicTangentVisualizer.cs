using UnityEngine;

public class GeodesicTangentVisualizer : MonoBehaviour
{
    public HyperbolicGeodesic geodesic;

    [Range(0f, 1f)]
    public float positionOnGeodesic = 0.5f;

    private void Start()
    {
        ShowTangent();
    }

    private void ShowTangent()
    {
        Vector2 position =
            geodesic.GetPointAt(
                positionOnGeodesic
            );

        Vector2 tangent =
            geodesic.GetTangentAt(
                positionOnGeodesic
            );

        Debug.Log(
            "Posición Poincaré: " +
            position
        );

        Debug.Log(
            "Tangente REAL: " +
            tangent
        );

        float angle =
            Mathf.Atan2(
                tangent.y,
                tangent.x
            ) * Mathf.Rad2Deg;

        transform.localPosition =
            new Vector3(
                position.x * geodesic.diskScale,
                0.7f,
                position.y * geodesic.diskScale
            );

        transform.localRotation =
            Quaternion.Euler(
                0f,
                -angle,
                0f
            );
    }
}