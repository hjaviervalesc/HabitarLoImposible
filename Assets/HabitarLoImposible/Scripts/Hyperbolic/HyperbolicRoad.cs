using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HyperbolicRoad : MonoBehaviour
{
    [Header("Geodésica")]
    public HyperbolicGeodesic geodesic;

    [Header("Carretera")]
    [Min(0.1f)]
    public float roadWidth = 1f;

    [Min(0.01f)]
    public float roadHeight = 0.05f;

    [Header("Resolución")]
    [Range(2, 200)]
    public int segments = 80;

    private MeshFilter meshFilter;


    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }


    private void Start()
    {
        GenerateRoad();
    }


    public void GenerateRoad()
    {
        if (geodesic == null)
        {
            Debug.LogError(
                "HyperbolicRoad: No se ha asignado una HyperbolicGeodesic."
            );

            return;
        }

        Mesh mesh = new Mesh();

        mesh.name = "HyperbolicRoadMesh";

        Vector3[] vertices =
            new Vector3[(segments + 1) * 2];

        Vector2[] uv =
            new Vector2[vertices.Length];

        int[] triangles =
            new int[segments * 6];


        // ==================================================
        // CREAR LOS VÉRTICES
        // ==================================================

        for (int i = 0; i <= segments; i++)
        {
            float t =
                i / (float)segments;


            // ----------------------------------------------
            // Posición sobre la geodésica
            // ----------------------------------------------

            Vector2 point =
                geodesic.GetPointAt(t);


            // ----------------------------------------------
            // Tangente de la geodésica
            // ----------------------------------------------

            Vector2 tangent =
                geodesic.GetTangentAt(t);


            // ----------------------------------------------
            // Vector perpendicular
            // ----------------------------------------------

            Vector2 normal =
                new Vector2(
                    -tangent.y,
                    tangent.x
                ).normalized;


            // ----------------------------------------------
            // Mitad del ancho
            // ----------------------------------------------

            float halfWidth =
                roadWidth * 0.5f;


            // ----------------------------------------------
            // Lado izquierdo y derecho
            // ----------------------------------------------

            Vector2 left =
                point +
                normal * halfWidth;


            Vector2 right =
                point -
                normal * halfWidth;


            // ----------------------------------------------
            // Convertir a Unity
            // ----------------------------------------------

            vertices[i * 2] =
                ToUnityPosition(left);


            vertices[i * 2 + 1] =
                ToUnityPosition(right);


            // ----------------------------------------------
            // UV
            // ----------------------------------------------

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


            // Primer triángulo

            triangles[triangleIndex++] =
                current;

            triangles[triangleIndex++] =
                next;

            triangles[triangleIndex++] =
                current + 1;


            // Segundo triángulo

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

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;


        // Normales

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();


        meshFilter.mesh = mesh;
    }


    private Vector3 ToUnityPosition(Vector2 point)
    {
        return new Vector3(
            point.x * geodesic.diskScale,
            roadHeight,
            point.y * geodesic.diskScale
        );
    }
}