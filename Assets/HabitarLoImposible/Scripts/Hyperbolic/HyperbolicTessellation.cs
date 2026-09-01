using System.Collections.Generic;
using UnityEngine;

public class HyperbolicTessellation : MonoBehaviour
{
    [Header("Teselación regular {p,q}")]
    [Min(3)] public int p = 7;
    [Min(3)] public int q = 3;

    [Header("Representación")]
    public float diskScale = 50f;
    public HyperbolicGeodesic geodesicPrefab;
    public float rotationDegrees = 0f;

    [Header("Generación")]
    public bool generateOnStart = true;

    [Range(0, 4)]
    public int numberOfCrowns = 2;

    [Header("Protección de la frontera")]
    [Range(0.0001f, 0.01f)]
    public float boundaryMargin = 0.001f;


    // Evita repetir lados.
    private readonly HashSet<string> generatedEdges =
        new HashSet<string>();

    // Guarda los polígonos generados.
    private readonly List<Vector2[]> generatedPolygons =
        new List<Vector2[]>();

    // Evita repetir polígonos.
    private readonly HashSet<string> generatedPolygonKeys =
        new HashSet<string>();


    private void Start()
    {
        if (generateOnStart)
        {
            GenerateTessellation();
        }
    }


    // ======================================================
    // GENERACIÓN PRINCIPAL
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

        ClearGeneration();

        Vector2[] centralPolygon =
            CalculateCentralPolygon();

        AddPolygon(
            centralPolygon,
            "Central"
        );

        GenerateCrowns(
            centralPolygon
        );

        Debug.Log(
            $"Teselación {{{p},{q}}} generada.\n" +
            $"Coronas: {numberOfCrowns}\n" +
            $"Polígonos almacenados: {generatedPolygons.Count}\n" +
            $"Geodésicas diferentes: {generatedEdges.Count}"
        );
    }


    // ======================================================
    // GENERAR CORONAS
    // ======================================================

    private void GenerateCrowns(
        Vector2[] centralPolygon
    )
    {
        List<Vector2[]> currentCrown =
            new List<Vector2[]>
            {
                centralPolygon
            };

        for (int crown = 1;
             crown <= numberOfCrowns;
             crown++)
        {
            List<Vector2[]> nextCrown =
                new List<Vector2[]>();

            foreach (Vector2[] polygon in currentCrown)
            {
                for (int side = 0;
                     side < polygon.Length;
                     side++)
                {
                    Vector2 pointA =
                        polygon[side];

                    Vector2 pointB =
                        polygon[
                            (side + 1) %
                            polygon.Length
                        ];

                    Vector2[] reflectedPolygon =
                        ReflectPolygon(
                            polygon,
                            pointA,
                            pointB
                        );

                    bool wasAdded =
                        AddPolygon(
                            reflectedPolygon,
                            $"Crown{crown:00}_" +
                            $"Polygon_{nextCrown.Count + 1:00}"
                        );

                    if (wasAdded)
                    {
                        nextCrown.Add(
                            reflectedPolygon
                        );
                    }
                }
            }

            Debug.Log(
                $"Corona {crown}: " +
                $"{nextCrown.Count} polígonos nuevos."
            );

            currentCrown = nextCrown;

            if (currentCrown.Count == 0)
            {
                break;
            }
        }
    }


    // ======================================================
    // AÑADIR POLÍGONO
    // ======================================================

    private bool AddPolygon(
        Vector2[] polygon,
        string polygonName
    )
    {
        // Rechazar polígonos demasiado próximos
        // o exteriores a la frontera.

        if (!IsPolygonInsideDisk(polygon))
        {
            return false;
        }

        string polygonKey =
            CreatePolygonKey(polygon);

        // HashSet.Add devuelve false
        // si el polígono ya existía.

        if (!generatedPolygonKeys.Add(polygonKey))
        {
            return false;
        }

        generatedPolygons.Add(polygon);
        DrawPolygon(polygon, polygonName);

        return true;
    }


    // ======================================================
    // POLÍGONO CENTRAL
    // ======================================================

    private Vector2[] CalculateCentralPolygon()
    {
        // Carrión Rebellato, págs. 35-36.

        float s =
            Mathf.Sin(Mathf.PI / p);

        float c =
            Mathf.Cos(Mathf.PI / q);

        float d =
            1f /
            Mathf.Sqrt(
                1f -
                Mathf.Pow(
                    s / c,
                    2f
                )
            );

        float cosAngle =
            Mathf.Cos(Mathf.PI / p);

        float vertexRadius =
            d * cosAngle -
            Mathf.Sqrt(
                d * d *
                cosAngle *
                cosAngle -
                1f
            );

        Vector2[] vertices =
            new Vector2[p];

        float rotation =
            rotationDegrees *
            Mathf.Deg2Rad;

        for (int i = 0; i < p; i++)
        {
            float angle =
                rotation +
                2f *
                Mathf.PI *
                i / p;

            vertices[i] =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) *
                vertexRadius;
        }

        return vertices;
    }


    // ======================================================
    // REFLEXIÓN HIPERBÓLICA
    // ======================================================

    private Vector2[] ReflectPolygon(
        Vector2[] polygon,
        Vector2 pointA,
        Vector2 pointB
    )
    {
        Vector2[] reflected =
            new Vector2[polygon.Length];

        // Geodésica con forma de diámetro.

        if (HyperbolicGeodesic.IsDiameter(
                pointA,
                pointB
            ))
        {
            Vector2 direction =
                (pointB - pointA).normalized;

            for (int i = 0;
                 i < polygon.Length;
                 i++)
            {
                reflected[i] =
                    ReflectAcrossDiameter(
                        polygon[i],
                        direction
                    );
            }

            return reflected;
        }

        // Geodésica con forma de arco.

        bool circleFound =
            HyperbolicGeodesic.TryGetSupportingCircle(
                pointA,
                pointB,
                out Vector2 center,
                out float radius
            );

        if (!circleFound)
        {
            Debug.LogError(
                "No se ha podido calcular " +
                "la circunferencia de la geodésica."
            );

            return polygon;
        }

        for (int i = 0;
             i < polygon.Length;
             i++)
        {
            reflected[i] =
                ReflectAcrossCircle(
                    polygon[i],
                    center,
                    radius
                );
        }

        return reflected;
    }


    // ======================================================
    // INVERSIÓN CIRCULAR
    // ======================================================

    private Vector2 ReflectAcrossCircle(
        Vector2 point,
        Vector2 center,
        float radius
    )
    {
        // Hitchman, secciones 3.2 y 5.1:
        //
        // P' = C + R²(P-C) / ||P-C||²

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

    private Vector2 ReflectAcrossDiameter(
        Vector2 point,
        Vector2 direction
    )
    {
        Vector2 projection =
            Vector2.Dot(
                point,
                direction
            ) *
            direction;

        return 2f * projection - point;
    }


    // ======================================================
    // COMPROBAR FRONTERA DEL DISCO
    // ======================================================

    private bool IsPolygonInsideDisk(
        Vector2[] polygon
    )
    {
        float maximumRadius =
            1f - boundaryMargin;

        float maximumSquaredRadius =
            maximumRadius *
            maximumRadius;

        foreach (Vector2 vertex in polygon)
        {
            if (vertex.sqrMagnitude >=
                maximumSquaredRadius)
            {
                return false;
            }
        }

        return true;
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
                vertices[
                    (i + 1) %
                    vertices.Length
                ];

            string edgeKey =
                CreateEdgeKey(
                    pointA,
                    pointB
                );

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
    // CREAR GEODÉSICA
    // ======================================================

    private void CreateGeodesic(
        Vector2 poincareA,
        Vector2 poincareB,
        string geodesicName
    )
    {
        HyperbolicGeodesic geodesic =
            Instantiate(
                geodesicPrefab,
                transform
            );

        geodesic.name =
            geodesicName;

        geodesic.pointA =
            HyperbolicPoint.PoincareToHyperboloid(
                poincareA
            );

        geodesic.pointB =
            HyperbolicPoint.PoincareToHyperboloid(
                poincareB
            );

        geodesic.diskScale =
            diskScale;

        geodesic.DrawGeodesic();
    }


    // ======================================================
    // CLAVE ÚNICA DE UN POLÍGONO
    // ======================================================

    private string CreatePolygonKey(
        Vector2[] polygon
    )
    {
        const float precision =
            100000f;

        List<string> vertexKeys =
            new List<string>();

        foreach (Vector2 vertex in polygon)
        {
            int x =
                Mathf.RoundToInt(
                    vertex.x *
                    precision
                );

            int y =
                Mathf.RoundToInt(
                    vertex.y *
                    precision
                );

            vertexKeys.Add(
                $"{x},{y}"
            );
        }

        vertexKeys.Sort();

        return string.Join(
            "|",
            vertexKeys
        );
    }


    // ======================================================
    // CLAVE ÚNICA DE UN LADO
    // ======================================================

    private string CreateEdgeKey(
        Vector2 pointA,
        Vector2 pointB
    )
    {
        const float precision =
            100000f;

        string first =
            $"{Mathf.RoundToInt(pointA.x * precision)}," +
            $"{Mathf.RoundToInt(pointA.y * precision)}";

        string second =
            $"{Mathf.RoundToInt(pointB.x * precision)}," +
            $"{Mathf.RoundToInt(pointB.y * precision)}";

        return string.CompareOrdinal(
            first,
            second
        ) < 0
            ? first + "|" + second
            : second + "|" + first;
    }


    // ======================================================
    // CONDICIÓN HIPERBÓLICA
    // ======================================================

    private bool IsHyperbolicTessellation()
    {
        return
            (p - 2) *
            (q - 2) >
            4;
    }


    // ======================================================
    // LIMPIAR GENERACIÓN
    // ======================================================

    private void ClearGeneration()
    {
        generatedEdges.Clear();
        generatedPolygons.Clear();
        generatedPolygonKeys.Clear();

        for (int i = transform.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject child =
                transform
                    .GetChild(i)
                    .gameObject;

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }
}