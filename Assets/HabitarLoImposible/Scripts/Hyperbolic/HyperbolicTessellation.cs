using System.Collections.Generic;
using UnityEngine;

public class HyperbolicTessellation : MonoBehaviour
{
    [Header("Teselación regular {p,q}")]
    [Min(3)] public int p = 7;
    [Min(3)] public int q = 3;

    [Header("Orientación")]
    public float rotationDegrees = 0f;

    [Header("Generación")]
    public bool generateOnAwake = true;

    [Range(0, 4)]
    public int numberOfCrowns = 2;

    [Header("Protección de la frontera")]
    [Range(0.0001f, 0.01f)]
    public float boundaryMargin = 0.001f;


    private readonly List<Vector2[]> polygons =
        new List<Vector2[]>();

    private readonly List<HyperbolicEdge> edges =
        new List<HyperbolicEdge>();

    private readonly HashSet<string> polygonKeys =
        new HashSet<string>();

    private readonly HashSet<string> edgeKeys =
        new HashSet<string>();


    // Otros scripts podrán consultar estos datos,
    // pero no modificar las listas directamente.

    public IReadOnlyList<Vector2[]> Polygons =>
        polygons;

    public IReadOnlyList<HyperbolicEdge> Edges =>
        edges;


    private void Awake()
    {
        if (generateOnAwake)
        {
            GenerateTessellation();
        }
    }


    // ======================================================
    // GENERACIÓN PRINCIPAL
    // ======================================================

    [ContextMenu("Generar datos de teselación")]
    public void GenerateTessellation()
    {
        ClearData();


        if (!IsHyperbolicTessellation())
        {
            Debug.LogError(
                $"La teselación {{{p},{q}}} no es hiperbólica. " +
                "Debe cumplirse (p - 2)(q - 2) > 4."
            );

            return;
        }


        Vector2[] centralPolygon =
            CalculateCentralPolygon();


        AddPolygon(
            centralPolygon
        );


        GenerateCrowns(
            centralPolygon
        );


        Debug.Log(
            $"Datos de teselación {{{p},{q}}} generados.\n" +
            $"Coronas: {numberOfCrowns}\n" +
            $"Polígonos: {polygons.Count}\n" +
            $"Lados únicos: {edges.Count}"
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


                    if (AddPolygon(reflectedPolygon))
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


            currentCrown =
                nextCrown;


            if (currentCrown.Count == 0)
            {
                break;
            }
        }
    }


    // ======================================================
    // AÑADIR POLÍGONO Y SUS LADOS
    // ======================================================

    private bool AddPolygon(
        Vector2[] polygon
    )
    {
        if (!IsPolygonInsideDisk(polygon))
        {
            return false;
        }


        string polygonKey =
            CreatePolygonKey(polygon);


        if (!polygonKeys.Add(polygonKey))
        {
            return false;
        }


        polygons.Add(polygon);
        AddPolygonEdges(polygon);

        return true;
    }


    private void AddPolygonEdges(
        Vector2[] polygon
    )
    {
        for (int i = 0;
             i < polygon.Length;
             i++)
        {
            Vector2 pointA =
                polygon[i];

            Vector2 pointB =
                polygon[
                    (i + 1) %
                    polygon.Length
                ];


            string edgeKey =
                CreateEdgeKey(
                    pointA,
                    pointB
                );


            if (!edgeKeys.Add(edgeKey))
            {
                continue;
            }


            edges.Add(
                new HyperbolicEdge(
                    pointA,
                    pointB
                )
            );
        }
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


        for (int i = 0;
             i < p;
             i++)
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


        bool circleFound =
            HyperbolicGeodesic.TryGetSupportingCircle(
                pointA,
                pointB,
                out Vector2 center,
                out float radius
            );


        if (!circleFound)
        {
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
            return point;
        }


        return center +
               direction *
               (
                   radius * radius /
                   squaredDistance
               );
    }


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
    // FRONTERA
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
    // CLAVES ÚNICAS
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
            vertexKeys.Add(
                CreatePointKey(
                    vertex,
                    precision
                )
            );
        }


        vertexKeys.Sort();


        return string.Join(
            "|",
            vertexKeys
        );
    }


    private string CreateEdgeKey(
        Vector2 pointA,
        Vector2 pointB
    )
    {
        const float precision =
            100000f;


        string first =
            CreatePointKey(
                pointA,
                precision
            );

        string second =
            CreatePointKey(
                pointB,
                precision
            );


        return string.CompareOrdinal(
            first,
            second
        ) < 0
            ? first + "|" + second
            : second + "|" + first;
    }


    private string CreatePointKey(
        Vector2 point,
        float precision
    )
    {
        int x =
            Mathf.RoundToInt(
                point.x *
                precision
            );

        int y =
            Mathf.RoundToInt(
                point.y *
                precision
            );


        return $"{x},{y}";
    }


    // ======================================================
    // UTILIDADES
    // ======================================================

    private bool IsHyperbolicTessellation()
    {
        return
            (p - 2) *
            (q - 2) >
            4;
    }


    private void ClearData()
    {
        polygons.Clear();
        edges.Clear();
        polygonKeys.Clear();
        edgeKeys.Clear();
    }
}