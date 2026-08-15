using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GridSurfaceMesh : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int width = 100;
    [SerializeField] private int height = 100;

    [Header("Tamaño de cada casilla")]
    [SerializeField] private float cellSize = 1f;

    private Mesh mesh;

    private Vector3[] vertices;

    // Posición original de cada vértice
    private Vector3[] originalVertices;

    private int[] triangles;


    [Header("Debug Deformation")]
    [SerializeField] private float slopeDepth = 1f;
    [SerializeField] private float slopeStart = -5f;
    [SerializeField] private float slopeEnd = 5f;


    [Header("Test Embudo")]
    [SerializeField] private float holeRadius = 10f;
    [SerializeField] private float holeDepth = 3f;


    public Mesh Mesh => mesh;

    public Vector3[] Vertices => vertices;

    public int Width => width;

    public int Height => height;

    public float CellSize => cellSize;


    private void Start()
    {
        GenerateMesh();

        //CreateTestSlope();
        //CreateTestFunnel();
    }


    private void GenerateMesh()
    {
        // Número de vértices.
        // Necesitamos uno más que el número de casillas.
        int vertexWidth = width + 1;
        int vertexHeight = height + 1;


        vertices = new Vector3[
            vertexWidth * vertexHeight
        ];


        // Creamos los vértices.
        for (int z = 0; z < vertexHeight; z++)
        {
            for (int x = 0; x < vertexWidth; x++)
            {
                int index =
                    z * vertexWidth + x;


                float offsetX =
                    (width * cellSize) / 2f;

                float offsetZ =
                    (height * cellSize) / 2f;


                vertices[index] = new Vector3(
                    x * cellSize - offsetX,
                    0f,
                    z * cellSize - offsetZ
                );
            }
        }


        // Guardamos una copia de las posiciones originales.
        originalVertices =
            (Vector3[])vertices.Clone();


        // Cada casilla necesita 2 triángulos.
        triangles =
            new int[width * height * 6];


        int triangleIndex = 0;


        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int bottomLeft =
                    z * vertexWidth + x;

                int bottomRight =
                    bottomLeft + 1;

                int topLeft =
                    (z + 1) * vertexWidth + x;

                int topRight =
                    topLeft + 1;


                // Primer triángulo
                triangles[triangleIndex++] =
                    bottomLeft;

                triangles[triangleIndex++] =
                    topLeft;

                triangles[triangleIndex++] =
                    topRight;


                // Segundo triángulo
                triangles[triangleIndex++] =
                    bottomLeft;

                triangles[triangleIndex++] =
                    topRight;

                triangles[triangleIndex++] =
                    bottomRight;
            }
        }


        // Creamos la Mesh.
        mesh = new Mesh();

        mesh.name =
            "GridSurfaceMesh";


        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;


        mesh.RecalculateNormals();
        mesh.RecalculateBounds();


        // La asignamos al MeshFilter.
        MeshFilter meshFilter =
            GetComponent<MeshFilter>();

        meshFilter.mesh =
            mesh;
    }


    /// <summary>
    /// Aplica la deformación correspondiente
    /// a una sección.
    /// </summary>
    public void ApplySectionDeformation(
        DeformationSection section)
    {
        foreach (int vertexIndex in section.VertexIndices)
        {
            Vector3 vertex =
                originalVertices[vertexIndex];


            vertex.y +=
                section.CurrentOffset;


            vertices[vertexIndex] =
                vertex;
        }


        // Actualizamos la malla una sola vez.
        mesh.vertices =
            vertices;


        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }


    private void CreateTestSlope()
    {
        for (int z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                int index =
                    z * (width + 1) + x;


                Vector3 vertex =
                    vertices[index];


                if (vertex.x >= slopeStart &&
                    vertex.x <= slopeEnd)
                {
                    float t =
                        Mathf.InverseLerp(
                            slopeStart,
                            slopeEnd,
                            vertex.x
                        );


                    vertex.y =
                        -t * slopeDepth;


                    vertices[index] =
                        vertex;
                }
            }
        }


        mesh.vertices =
            vertices;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }


    private void CreateTestFunnel()
    {
        for (int z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                int index =
                    z * (width + 1) + x;


                Vector3 vertex =
                    vertices[index];


                float distance =
                    new Vector2(
                        vertex.x,
                        vertex.z
                    ).magnitude;


                if (distance <= holeRadius)
                {
                    float t =
                        distance / holeRadius;


                    float depth =
                        (1f - t) * holeDepth;


                    vertex.y =
                        -depth;


                    vertices[index] =
                        vertex;
                }
            }
        }


        mesh.vertices =
            vertices;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}