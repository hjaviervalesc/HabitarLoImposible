using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera una teselación hiperbólica regular {p,q} mediante reflexiones del polígono central y registra sus polígonos y aristas.
/// </summary>
public class HyperbolicTessellation : MonoBehaviour
{
    [Header("Teselación regular {p,q}")]
    [Min(3)] public int p = 7;
    [Min(3)] public int q = 3;

    [Header("Orientación")]
    public float rotationDegrees = 0f;

    [Header("Generación")]
    public bool generateOnAwake = true;

    [Range(0, 5)]
    public int numberOfCrowns = 2;

    [Header("Protección de la frontera")]
    [Range(0.0001f, 0.01f)]
    public float boundaryMargin = 0.001f;

    private readonly List<Vector2[]> polygons = new List<Vector2[]>();
    private readonly List<HyperbolicEdge> edges = new List<HyperbolicEdge>();
    private readonly HashSet<string> polygonKeys = new HashSet<string>();
    private readonly HashSet<string> edgeKeys = new HashSet<string>();

    public IReadOnlyList<HyperbolicEdge> Edges => edges;

    /// <summary>
    /// Genera la teselación al iniciar si la opción generateOnAwake está activada.
    /// </summary>
    private void Awake()
    {
        if (generateOnAwake)
        {
            GenerateTessellation();
        }
    }

    /// <summary>
    /// Limpia los datos anteriores, comprueba la condición hiperbólica y genera el polígono central y sus coronas.
    /// </summary>
    [ContextMenu("Generar datos de teselación")]
    public void GenerateTessellation()
    {
        ClearData();

        if (!IsHyperbolicTessellation())
        {
            Debug.LogError($"La teselación {{{p},{q}}} no es hiperbólica. " + "Debe cumplirse (p - 2)(q - 2) > 4.");
            return;
        }

        Vector2[] centralPolygon = CalculateCentralPolygon();

        AddPolygon(centralPolygon);

        GenerateCrowns(centralPolygon);
    }

    /// <summary>
    /// Genera las coronas mediante reflexiones de los polígonos de la corona anterior respecto a cada uno de sus lados.
    /// </summary>
    private void GenerateCrowns(Vector2[] centralPolygon)
    {
        List<Vector2[]> currentCrown = new List<Vector2[]> { centralPolygon };

        for (int crown = 1; crown <= numberOfCrowns; crown++)
        {
            List<Vector2[]> nextCrown = new List<Vector2[]>();

            foreach (Vector2[] polygon in currentCrown)
            {
                for (int side = 0; side < polygon.Length; side++)
                {
                    Vector2 pointA = polygon[side];
                    Vector2 pointB = polygon[(side + 1) % polygon.Length];

                    Vector2[] reflectedPolygon = ReflectPolygon(polygon, pointA, pointB);

                    if (AddPolygon(reflectedPolygon))
                    {
                        nextCrown.Add(reflectedPolygon);
                    }
                }
            }

            currentCrown = nextCrown;

            if (currentCrown.Count == 0)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Añade un polígono y registra sus lados si sus vértices están dentro del límite permitido y su clave no está registrada.
    /// </summary>
    private bool AddPolygon(Vector2[] polygon)
    {
        if (!IsPolygonInsideDisk(polygon))
        {
            return false;
        }

        string polygonKey = CreatePolygonKey(polygon);

        if (!polygonKeys.Add(polygonKey))
        {
            return false;
        }

        polygons.Add(polygon);
        AddPolygonEdges(polygon);

        return true;
    }

    /// <summary>
    /// Añade los lados de un polígono a la colección de aristas evitando duplicados mediante sus claves.
    /// </summary>
    private void AddPolygonEdges(Vector2[] polygon)
    {
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 pointA = polygon[i];
            Vector2 pointB = polygon[(i + 1) % polygon.Length];

            string edgeKey = CreateEdgeKey(pointA, pointB);

            if (!edgeKeys.Add(edgeKey))
            {
                continue;
            }

            edges.Add(new HyperbolicEdge(pointA, pointB));
        }
    }

    /// <summary>
    /// Calcula los vértices del polígono regular central a partir de p y q y aplica la rotación configurada.
    /// </summary>
    private Vector2[] CalculateCentralPolygon()
    {
        float s = Mathf.Sin(Mathf.PI / p);
        float c = Mathf.Cos(Mathf.PI / q);

        float d = 1f / Mathf.Sqrt(1f - Mathf.Pow(s / c, 2f));

        float cosAngle = Mathf.Cos(Mathf.PI / p);

        float vertexRadius = d * cosAngle - Mathf.Sqrt(d * d * cosAngle * cosAngle - 1f);

        Vector2[] vertices = new Vector2[p];

        float rotation = rotationDegrees * Mathf.Deg2Rad;

        for (int i = 0; i < p; i++)
        {
            float angle = rotation + 2f * Mathf.PI * i / p;

            vertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * vertexRadius;
        }

        return vertices;
    }

    /// <summary>
    /// Refleja un polígono respecto a un lado hiperbólico mediante reflexión sobre un diámetro o inversión en su circunferencia de soporte.
    /// </summary>
    private Vector2[] ReflectPolygon(Vector2[] polygon, Vector2 pointA, Vector2 pointB)
    {
        Vector2[] reflected = new Vector2[polygon.Length];

        if (HyperbolicGeodesic.IsDiameter(pointA, pointB))
        {
            Vector2 direction = (pointB - pointA).normalized;

            for (int i = 0; i < polygon.Length; i++)
            {
                reflected[i] = ReflectAcrossDiameter(polygon[i], direction);
            }

            return reflected;
        }

        bool circleFound = HyperbolicGeodesic.TryGetSupportingCircle(pointA, pointB, out Vector2 center, out float radius);

        if (!circleFound)
        {
            return polygon;
        }

        for (int i = 0; i < polygon.Length; i++)
        {
            reflected[i] = ReflectAcrossCircle(polygon[i], center, radius);
        }

        return reflected;
    }

    /// <summary>
    /// Invierte un punto respecto a una circunferencia y devuelve el punto original si está demasiado cerca de su centro.
    /// </summary>
    private Vector2 ReflectAcrossCircle(Vector2 point, Vector2 center, float radius)
    {
        Vector2 direction = point - center;
        float squaredDistance = direction.sqrMagnitude;

        if (squaredDistance < 0.000001f)
        {
            return point;
        }

        return center + direction * (radius * radius / squaredDistance);
    }

    /// <summary>
    /// Refleja un punto respecto a la recta que pasa por el origen con la dirección unitaria indicada.
    /// </summary>
    private Vector2 ReflectAcrossDiameter(Vector2 point, Vector2 direction)
    {
        Vector2 projection = Vector2.Dot(point, direction) * direction;

        return 2f * projection - point;
    }

    /// <summary>
    /// Comprueba que todos los vértices del polígono están dentro del disco respetando el margen de protección de la frontera.
    /// </summary>
    private bool IsPolygonInsideDisk(Vector2[] polygon)
    {
        float maximumRadius = 1f - boundaryMargin;
        float maximumSquaredRadius = maximumRadius * maximumRadius;

        foreach (Vector2 vertex in polygon)
        {
            if (vertex.sqrMagnitude >= maximumSquaredRadius)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Genera una clave del polígono ordenando las claves de sus vértices para detectar duplicados con la precisión establecida.
    /// </summary>
    private string CreatePolygonKey(Vector2[] polygon)
    {
        const float precision = 100000f;

        List<string> vertexKeys = new List<string>();

        foreach (Vector2 vertex in polygon)
        {
            vertexKeys.Add(CreatePointKey(vertex, precision));
        }

        vertexKeys.Sort();

        return string.Join("|", vertexKeys);
    }

    /// <summary>
    /// Genera una clave del lado independiente del orden de sus extremos para detectar duplicados con la precisión establecida.
    /// </summary>
    private string CreateEdgeKey(Vector2 pointA, Vector2 pointB)
    {
        const float precision = 100000f;

        string first = CreatePointKey(pointA, precision);
        string second = CreatePointKey(pointB, precision);

        return string.CompareOrdinal(first, second) < 0 ? first + "|" + second : second + "|" + first;
    }

    /// <summary>
    /// Genera una clave redondeando las coordenadas del punto multiplicadas por el factor de precisión indicado.
    /// </summary>
    private string CreatePointKey(Vector2 point, float precision)
    {
        int x = Mathf.RoundToInt(point.x * precision);
        int y = Mathf.RoundToInt(point.y * precision);

        return $"{x},{y}";
    }

    /// <summary>
    /// Comprueba si p y q satisfacen la condición (p - 2)(q - 2) mayor que cuatro.
    /// </summary>
    private bool IsHyperbolicTessellation()
    {
        return (p - 2) * (q - 2) > 4;
    }

    /// <summary>
    /// Elimina los polígonos, las aristas y las claves almacenadas de la teselación.
    /// </summary>
    private void ClearData()
    {
        polygons.Clear();
        edges.Clear();
        polygonKeys.Clear();
        edgeKeys.Clear();
    }
}