using System.Collections.Generic;
using UnityEngine;

public class HyperbolicTessellation : MonoBehaviour
{
    [Header("Teselación regular {p,q}")]
    [Min(3)]
    public int p = 7;

    [Min(3)]
    public int q = 3;

    [Header("Representación")]
    public float diskScale = 50f;

    [Header("Plantilla de geodésica")]
    public HyperbolicGeodesic geodesicPrefab;

    [Header("Orientación")]
    public float rotationDegrees = 0f;

    [Header("Generación")]
    public bool generateOnStart = true;

    [Tooltip("Genera los 7 polígonos vecinos del central")]
    public bool generateFirstCrown = true;

    // Evita dibujar dos veces un mismo lado.
    private readonly HashSet<string> generatedEdges =
        new HashSet<string>();


    private void Start()
    {
        if (generateOnStart)
        {
            GenerateTessellation();
        }
    }


    // ======================================================
    // GENERAR TESELACIÓN
    // ======================================================

    [ContextMenu("Generar teselación")]
    public void GenerateTessellation()
    {
        if (geodesicPrefab == null)
        {
            Debug.LogError(
                "Falta asignar Geodesic Prefab."
            );

            return;
        }

        if (!IsHyperbolicTessellation())
        {
            Debug.LogError(
                $"La teselación {{{p},{q}}} no es hiperbólica. " +
                "Debe cumplirse (p - 2)(q - 2) > 4."
            );

            return;
        }

        ClearGeneratedGeodesics();
        generatedEdges.Clear();

        // --------------------------------------------------
        // 1. Construir el polígono central
        // --------------------------------------------------

        Vector2[] centralPolygon =
            CalculateCentralPolygon();

        DrawPolygon(
            centralPolygon,
            "Central"
        );

        int polygonCount = 1;

        // --------------------------------------------------
        // 2. Generar la primera corona
        // --------------------------------------------------

        if (generateFirstCrown)
        {
            for (int side = 0; side < p; side++)
            {
                Vector2 pointA =
                    centralPolygon[side];

                Vector2 pointB =
                    centralPolygon[(side + 1) % p];

                Vector2[] reflectedPolygon =
                    ReflectPolygonAcrossGeodesic(
                        centralPolygon,
                        pointA,
                        pointB
                    );

                DrawPolygon(
                    reflectedPolygon,
                    $"Crown01_Polygon_{side + 1:00}"
                );

                polygonCount++;
            }
        }

        Debug.Log(
            $"Teselación {{{p},{q}}} generada.\n" +
            $"Polígonos: {polygonCount}\n" +
            $"Geodésicas diferentes: {generatedEdges.Count}"
        );
    }


    // ======================================================
    // CALCULAR POLÍGONO CENTRAL
    // ======================================================

    private Vector2[] CalculateCentralPolygon()
    {
        // Carrión, págs. 35-36.
        float s =
            Mathf.Sin(Mathf.PI / p);

        float c =
            Mathf.Cos(Mathf.PI / q);

        float ratioSC =
            s / c;

        float d =
            1f /
            Mathf.Sqrt(
                1f - ratioSC * ratioSC
            );

        float cosHalfSector =
            Mathf.Cos(Mathf.PI / p);

        // Radio euclídeo de los vértices
        // dentro del disco de Poincaré.
        float vertexRadius =
            d * cosHalfSector -
            Mathf.Sqrt(
                d * d *
                cosHalfSector * cosHalfSector -
                1f
            );

        Vector2[] vertices =
            new Vector2[p];

        float rotationRadians =
            rotationDegrees *
            Mathf.Deg2Rad;

        for (int i = 0; i < p; i++)
        {
            float angle =
                rotationRadians +
                2f * Mathf.PI * i / p;

            vertices[i] =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) *
                vertexRadius;
        }

        Debug.Log(
            $"Radio normalizado de los vértices: " +
            $"{vertexRadius:F6}"
        );

        return vertices;
    }


    // ======================================================
    // REFLEJAR POLÍGONO RESPECTO DE UNA GEODÉSICA
    // ======================================================

    private Vector2[] ReflectPolygonAcrossGeodesic(
        Vector2[] polygon,
        Vector2 pointA,
        Vector2 pointB
    )
    {
        Vector2[] reflectedPolygon =
            new Vector2[polygon.Length];

        float determinant =
            pointA.x * pointB.y -
            pointA.y * pointB.x;

        // --------------------------------------------------
        // CASO 1: la geodésica es un diámetro
        // --------------------------------------------------

        if (Mathf.Abs(determinant) < 0.0001f)
        {
            Vector2 lineDirection =
                (pointB - pointA).normalized;

            for (int i = 0;
                 i < polygon.Length;
                 i++)
            {
                reflectedPolygon[i] =
                    ReflectPointAcrossDiameter(
                        polygon[i],
                        lineDirection
                    );
            }

            return reflectedPolygon;
        }

        // --------------------------------------------------
        // CASO 2: la geodésica es una circunferencia
        // --------------------------------------------------

        CalculateGeodesicCircle(
            pointA,
            pointB,
            out Vector2 center,
            out float radius
        );

        for (int i = 0;
             i < polygon.Length;
             i++)
        {
            reflectedPolygon[i] =
                ReflectPointAcrossCircle(
                    polygon[i],
                    center,
                    radius
                );
        }

        return reflectedPolygon;
    }


    // ======================================================
    // REFLEXIÓN RESPECTO DE UNA CIRCUNFERENCIA
    // ======================================================

    private Vector2 ReflectPointAcrossCircle(
        Vector2 point,
        Vector2 center,
        float radius
    )
    {
        Vector2 direction =
            point - center;

        float squaredDistance =
            direction.sqrMagnitude;

        if (squaredDistance < 0.000001f)
        {
            Debug.LogError(
                "No se puede invertir el centro " +
                "de una circunferencia."
            );

            return point;
        }

        // P' = C + R²(P-C) / ||P-C||²

        return center +
               direction *
               (
                   radius * radius /
                   squaredDistance
               );
    }


    // ======================================================
    // REFLEXIÓN RESPECTO DE UN DIÁMETRO
    // ======================================================

    private Vector2 ReflectPointAcrossDiameter(
        Vector2 point,
        Vector2 lineDirection
    )
    {
        Vector2 projection =
            Vector2.Dot(
                point,
                lineDirection
            ) *
            lineDirection;

        return 2f * projection - point;
    }


    // ======================================================
    // CIRCUNFERENCIA DE LA GEODÉSICA
    // ======================================================

    private void CalculateGeodesicCircle(
        Vector2 pointA,
        Vector2 pointB,
        out Vector2 center,
        out float radius
    )
    {
        float denominator =
            2f *
            (
                pointA.x * pointB.y -
                pointA.y * pointB.x
            );

        float a =
            (pointA.sqrMagnitude + 1f) *
            pointB.y -
            (pointB.sqrMagnitude + 1f) *
            pointA.y;

        float b =
            (pointB.sqrMagnitude + 1f) *
            pointA.x -
            (pointA.sqrMagnitude + 1f) *
            pointB.x;

        center =
            new Vector2(
                a / denominator,
                b / denominator
            );

        radius =
            Vector2.Distance(
                center,
                pointA
            );
    }


    // ======================================================
    // DIBUJAR POLÍGONO
    // ======================================================

    private void DrawPolygon(
        Vector2[] vertices,
        string polygonName
    )
    {
        for (int i = 0;
             i < vertices.Length;
             i++)
        {
            Vector2 pointA =
                vertices[i];

            Vector2 pointB =
                vertices[(i + 1) % vertices.Length];

            string edgeKey =
                CreateEdgeKey(
                    pointA,
                    pointB
                );

            // Si el lado ya pertenece a otro polígono,
            // no lo dibujamos otra vez.
            if (!generatedEdges.Add(edgeKey))
            {
                continue;
            }

            CreateGeodesic(
                pointA,
                pointB,
                $"{polygonName}_Side_{i + 1:00}"
            );
        }
    }


    // ======================================================
    // CREAR UNA GEODÉSICA
    // ======================================================

    private void CreateGeodesic(
        Vector2 poincareA,
        Vector2 poincareB,
        string geodesicName
    )
    {
        Vector2 hyperboloidA =
            PoincareToHyperboloid(
                poincareA
            );

        Vector2 hyperboloidB =
            PoincareToHyperboloid(
                poincareB
            );

        HyperbolicGeodesic geodesic =
            Instantiate(
                geodesicPrefab,
                transform
            );

        geodesic.name =
            geodesicName;

        geodesic.pointA =
            hyperboloidA;

        geodesic.pointB =
            hyperboloidB;

        geodesic.diskScale =
            diskScale;

        geodesic.DrawGeodesic();
    }


    // ======================================================
    // ELIMINAR LADOS DUPLICADOS
    // ======================================================

    private string CreateEdgeKey(
        Vector2 pointA,
        Vector2 pointB
    )
    {
        const float precision =
            100000f;

        int ax =
            Mathf.RoundToInt(
                pointA.x * precision
            );

        int ay =
            Mathf.RoundToInt(
                pointA.y * precision
            );

        int bx =
            Mathf.RoundToInt(
                pointB.x * precision
            );

        int by =
            Mathf.RoundToInt(
                pointB.y * precision
            );

        string first =
            $"{ax},{ay}";

        string second =
            $"{bx},{by}";

        // La clave debe ser la misma tanto para A-B
        // como para B-A.

        if (string.CompareOrdinal(
                first,
                second
            ) < 0)
        {
            return first + "|" + second;
        }

        return second + "|" + first;
    }


    // ======================================================
    // DISCO DE POINCARÉ → HIPERBOLOIDE
    // ======================================================

    private Vector2 PoincareToHyperboloid(
        Vector2 poincarePoint
    )
    {
        float squaredRadius =
            poincarePoint.sqrMagnitude;

        float denominator =
            1f - squaredRadius;

        if (denominator <= 0.000001f)
        {
            Debug.LogError(
                "El punto está fuera del disco."
            );

            denominator = 0.000001f;
        }

        float x =
            2f * poincarePoint.x /
            denominator;

        float y =
            2f * poincarePoint.y /
            denominator;

        return new Vector2(x, y);
    }


    // ======================================================
    // COMPROBAR CONDICIÓN HIPERBÓLICA
    // ======================================================

    private bool IsHyperbolicTessellation()
    {
        return
            (p - 2) *
            (q - 2) >
            4;
    }


    // ======================================================
    // LIMPIAR GENERACIÓN ANTERIOR
    // ======================================================

    private void ClearGeneratedGeodesics()
    {
        for (int i = transform.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                transform.GetChild(i);

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(
                    child.gameObject
                );
            }
        }
    }
}