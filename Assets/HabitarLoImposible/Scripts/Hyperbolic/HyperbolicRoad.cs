using UnityEngine;

[RequireComponent(typeof(HyperbolicGeodesic))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class HyperbolicRoad : MonoBehaviour
{
    [Header("Carretera")]
    [Min(0.01f)]
    public float roadWidth = 1.25f;

    [Min(0.01f)]
    public float roadHeight = 0.05f;

    [Header("Escalado")]
    public bool useHyperbolicScale = true;

    [Header("Resolución")]
    [Range(2, 100)]
    public int segments = 20;

    private HyperbolicSpace hyperbolicSpace;
    private HyperbolicGeodesic geodesic;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private Mesh generatedMesh;
    private bool hasGenerated;


    public HyperbolicGeodesic Geodesic
    {
        get
        {
            if (geodesic == null)
            {
                geodesic =
                    GetComponent<HyperbolicGeodesic>();
            }

            return geodesic;
        }
    }


    private void Awake()
    {
        geodesic =
            GetComponent<HyperbolicGeodesic>();

        meshFilter =
            GetComponent<MeshFilter>();

        meshCollider =
            GetComponent<MeshCollider>();
    }


    private void Start()
    {
        if (!hasGenerated &&
            hyperbolicSpace != null)
        {
            GenerateRoad();
        }
    }


    /// <summary>
    /// Configura la geodésica y genera
    /// la malla de la carretera.
    /// </summary>
    public void Initialize(
        Vector2 hyperbolicPointA,
        Vector2 hyperbolicPointB,
        HyperbolicSpace targetSpace
    )
    {
        hyperbolicSpace =
            targetSpace;

        Geodesic.PointA =
            hyperbolicPointA;

        Geodesic.PointB =
            hyperbolicPointB;

        Geodesic.CalculateGeodesic();

        GenerateRoad();
    }


    /// <summary>
    /// Devuelve la mitad de la anchura
    /// de la carretera en un punto del disco.
    /// </summary>
    public float GetHalfWidthAt(
        Vector2 poincarePosition
    )
    {
        float scaleFactor = 1f;

        if (useHyperbolicScale &&
            hyperbolicSpace != null)
        {
            scaleFactor =
                hyperbolicSpace.GetScaleFactor(
                    poincarePosition
                );
        }

        return roadWidth *
               scaleFactor *
               0.5f;
    }


    /// <summary>
    /// Genera la malla visible y actualiza
    /// el MeshCollider de la carretera.
    /// </summary>
    public void GenerateRoad()
    {
        if (hyperbolicSpace == null)
        {
            hyperbolicSpace =
                GetComponentInParent<
                    HyperbolicSpace
                >();
        }

        if (hyperbolicSpace == null)
        {
            Debug.LogError(
                "HyperbolicRoad: falta HyperbolicSpace."
            );

            return;
        }

        if (meshFilter == null)
        {
            meshFilter =
                GetComponent<MeshFilter>();
        }

        if (meshCollider == null)
        {
            meshCollider =
                GetComponent<MeshCollider>();
        }

        Geodesic.CalculateGeodesic();

        DestroyGeneratedMesh();

        generatedMesh = new Mesh
        {
            name = "HyperbolicRoadMesh"
        };

        Vector3[] vertices =
            new Vector3[(segments + 1) * 2];

        Vector2[] uv =
            new Vector2[vertices.Length];

        int[] triangles =
            new int[segments * 6];

        for (int i = 0;
             i <= segments;
             i++)
        {
            float t =
                i / (float)segments;

            Vector2 point =
                Geodesic.GetPointAt(t);

            Vector2 tangent =
                Geodesic.GetTangentAt(t);

            Vector2 normal =
                new Vector2(
                    -tangent.y,
                    tangent.x
                ).normalized;

            float halfWidth =
                GetHalfWidthAt(point);

            Vector3 center =
                hyperbolicSpace.PoincareToUnity(
                    point,
                    roadHeight
                );

            Vector3 widthDirection =
                new Vector3(
                    normal.x,
                    0f,
                    normal.y
                ) *
                halfWidth;

            vertices[i * 2] =
                center + widthDirection;

            vertices[i * 2 + 1] =
                center - widthDirection;

            uv[i * 2] =
                new Vector2(0f, t);

            uv[i * 2 + 1] =
                new Vector2(1f, t);
        }

        int triangleIndex = 0;

        for (int i = 0;
             i < segments;
             i++)
        {
            int current = i * 2;
            int next = (i + 1) * 2;

            triangles[triangleIndex++] =
                current;

            triangles[triangleIndex++] =
                next;

            triangles[triangleIndex++] =
                current + 1;

            triangles[triangleIndex++] =
                current + 1;

            triangles[triangleIndex++] =
                next;

            triangles[triangleIndex++] =
                next + 1;
        }

        generatedMesh.vertices =
            vertices;

        generatedMesh.triangles =
            triangles;

        generatedMesh.uv =
            uv;

        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh =
            generatedMesh;

        /*
         * El collider utiliza exactamente
         * la misma malla que la carretera.
         */
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;

            meshCollider.sharedMesh =
                generatedMesh;
        }

        hasGenerated = true;
    }


    private void DestroyGeneratedMesh()
    {
        if (generatedMesh == null)
        {
            return;
        }

        if (meshCollider != null &&
            meshCollider.sharedMesh ==
            generatedMesh)
        {
            meshCollider.sharedMesh = null;
        }

        if (meshFilter != null &&
            meshFilter.sharedMesh ==
            generatedMesh)
        {
            meshFilter.sharedMesh = null;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedMesh);
        }
        else
        {
            DestroyImmediate(generatedMesh);
        }

        generatedMesh = null;
    }


    private void OnDestroy()
    {
        DestroyGeneratedMesh();
    }
}