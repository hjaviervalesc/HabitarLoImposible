using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HyperbolicRoad : MonoBehaviour
{
    [Header("Geodésica")]
    public HyperbolicGeodesic geodesic;

    [Header("Carretera")]
    [Min(0.01f)]
    public float roadWidth = 1.25f;

    [Min(0.01f)]
    public float roadHeight = 0.05f;

    [Header("Resolución")]
    [Range(2, 100)]
    public int segments = 20;

    private MeshFilter meshFilter;
    private bool hasGenerated;


    private void Awake()
    {
        meshFilter =
            GetComponent<MeshFilter>();
    }


    private void Start()
    {
        // Permite que las carreteras antiguas
        // sigan funcionando.

        if (!hasGenerated && geodesic != null)
        {
            GenerateRoad();
        }
    }


    public void Initialize(
        HyperbolicGeodesic targetGeodesic
    )
    {
        geodesic =
            targetGeodesic;

        GenerateRoad();
    }


    public void GenerateRoad()
    {
        if (geodesic == null)
        {
            Debug.LogError(
                "HyperbolicRoad: falta asignar " +
                "una HyperbolicGeodesic."
            );

            return;
        }


        if (meshFilter == null)
        {
            meshFilter =
                GetComponent<MeshFilter>();
        }


        Mesh mesh =
            new Mesh();

        mesh.name =
            "HyperbolicRoadMesh";


        Vector3[] vertices =
            new Vector3[(segments + 1) * 2];

        Vector2[] uv =
            new Vector2[vertices.Length];

        int[] triangles =
            new int[segments * 6];


        float halfWidth =
            roadWidth * 0.5f;


        // ==================================================
        // CREAR VÉRTICES
        // ==================================================

        for (int i = 0; i <= segments; i++)
        {
            float t =
                i / (float)segments;


            Vector2 point =
                geodesic.GetPointAt(t);

            Vector2 tangent =
                geodesic.GetTangentAt(t);


            Vector2 normal =
                new Vector2(
                    -tangent.y,
                    tangent.x
                ).normalized;


            Vector3 center =
                ToUnityPosition(point);


            Vector3 widthDirection =
                new Vector3(
                    normal.x,
                    0f,
                    normal.y
                ) *
                halfWidth;


            vertices[i * 2] =
                center +
                widthDirection;


            vertices[i * 2 + 1] =
                center -
                widthDirection;


            uv[i * 2] =
                new Vector2(
                    0f,
                    t
                );


            uv[i * 2 + 1] =
                new Vector2(
                    1f,
                    t
                );
        }


        // ==================================================
        // CREAR TRIÁNGULOS
        // ==================================================

        int triangleIndex = 0;


        for (int i = 0; i < segments; i++)
        {
            int current =
                i * 2;

            int next =
                (i + 1) * 2;


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


        // ==================================================
        // ASIGNAR MALLA
        // ==================================================

        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;

        mesh.uv =
            uv;


        mesh.RecalculateNormals();
        mesh.RecalculateBounds();


        meshFilter.mesh =
            mesh;

        hasGenerated =
            true;
    }


    private Vector3 ToUnityPosition(
        Vector2 point
    )
    {
        return new Vector3(
            point.x * geodesic.diskScale,
            roadHeight,
            point.y * geodesic.diskScale
        );
    }
}