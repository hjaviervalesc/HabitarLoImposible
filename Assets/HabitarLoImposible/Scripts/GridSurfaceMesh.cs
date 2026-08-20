using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
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


    private void Awake()
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


        mesh.indexFormat = IndexFormat.UInt32;

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

        MeshCollider meshCollider =
    GetComponent<MeshCollider>();

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = mesh;
        }
    }

    public void ApplySectionDeformations(
    List<DeformationSection> sections,
    int firstAffectedSection,
    int lastAffectedSection)
    {
        if (sections == null || sections.Count == 0)
            return;

        firstAffectedSection =
            Mathf.Max(0, firstAffectedSection);

        lastAffectedSection =
            Mathf.Min(
                sections.Count - 1,
                lastAffectedSection
            );


        // -----------------------------------------
        // Actualizamos únicamente las secciones
        // que han cambiado.
        // -----------------------------------------

        for (int s = firstAffectedSection;
             s <= lastAffectedSection;
             s++)
        {
            DeformationSection currentSection =
                sections[s];

            int nextIndex =
                Mathf.Min(
                    s + 1,
                    sections.Count - 1
                );

            DeformationSection nextSection =
                sections[nextIndex];


            foreach (int vertexIndex
                in currentSection.VertexIndices)
            {
                Vector3 originalVertex =
                    originalVertices[vertexIndex];


                float distance =
                    new Vector2(
                        originalVertex.x,
                        originalVertex.z
                    ).magnitude;


                float t = 0f;


                if (currentSection.OuterRadius >
                    currentSection.InnerRadius)
                {
                    t = Mathf.InverseLerp(
                        currentSection.InnerRadius,
                        currentSection.OuterRadius,
                        distance
                    );
                }


                float deformation =
                    Mathf.Lerp(
                        currentSection.CurrentOffset,
                        nextSection.CurrentOffset,
                        t
                    );


                Vector3 vertex =
                    originalVertex;

                vertex.y += deformation;

                vertices[vertexIndex] =
                    vertex;
            }
        }


        // -----------------------------------------
        // Actualizamos la malla UNA SOLA VEZ
        // -----------------------------------------

        mesh.vertices = vertices;

        RecalculateAffectedNormals(
        sections,
        firstAffectedSection,
        lastAffectedSection
    );

    }

    public void ApplyRadialDeformation(
    float deformationRadius)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex =
                originalVertices[i];

            float distance =
                new Vector2(
                    vertex.x,
                    vertex.z
                ).magnitude;

            if (distance <= deformationRadius)
            {
                float t =
                    distance / deformationRadius;

                float deformation =
                    Mathf.Lerp(
                        -0.1f,
                        0f,
                        t
                    );

                vertex.y += deformation;

                vertices[i] =
                    vertex;
            }
            else
            {
                vertex.y =
                    originalVertices[i].y;
            }
        }

        mesh.vertices =
            vertices;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void RecalculateAffectedNormals(
    List<DeformationSection> sections,
    int firstAffectedSection,
    int lastAffectedSection)
    {
        Vector3[] normals = mesh.normals;

        int vertexWidth = width + 1;

        HashSet<int> affectedVertices =
            new HashSet<int>();

        // Recogemos los vértices de las secciones afectadas
        // y de las secciones vecinas.
        for (int s = firstAffectedSection;
             s <= lastAffectedSection;
             s++)
        {
            foreach (int vertexIndex in sections[s].VertexIndices)
            {
                affectedVertices.Add(vertexIndex);

                int x = vertexIndex % vertexWidth;
                int z = vertexIndex / vertexWidth;

                // Añadimos vecinos porque sus normales
                // también pueden haber cambiado.
                if (x > 0)
                    affectedVertices.Add(vertexIndex - 1);

                if (x < width)
                    affectedVertices.Add(vertexIndex + 1);

                if (z > 0)
                    affectedVertices.Add(vertexIndex - vertexWidth);

                if (z < height)
                    affectedVertices.Add(vertexIndex + vertexWidth);
            }
        }

        foreach (int vertexIndex in affectedVertices)
        {
            normals[vertexIndex] =
                CalculateVertexNormal(vertexIndex);
        }

        mesh.normals = normals;
    }

    private Vector3 CalculateVertexNormal(int vertexIndex)
    {
        Vector3 normal = Vector3.zero;

        int vertexWidth = width + 1;

        int x = vertexIndex % vertexWidth;
        int z = vertexIndex / vertexWidth;


        // Triángulos que rodean al vértice.
        for (int dz = -1; dz <= 0; dz++)
        {
            for (int dx = -1; dx <= 0; dx++)
            {
                int cellX = x + dx;
                int cellZ = z + dz;

                if (cellX < 0 || cellX >= width ||
                    cellZ < 0 || cellZ >= height)
                    continue;


                int bottomLeft =
                    cellZ * vertexWidth + cellX;

                int bottomRight =
                    bottomLeft + 1;

                int topLeft =
                    bottomLeft + vertexWidth;

                int topRight =
                    topLeft + 1;


                // Triángulo 1
                if (vertexIndex == bottomLeft ||
                    vertexIndex == topLeft ||
                    vertexIndex == topRight)
                {
                    normal += GetTriangleNormal(
                        bottomLeft,
                        topLeft,
                        topRight
                    );
                }


                // Triángulo 2
                if (vertexIndex == bottomLeft ||
                    vertexIndex == topRight ||
                    vertexIndex == bottomRight)
                {
                    normal += GetTriangleNormal(
                        bottomLeft,
                        topRight,
                        bottomRight
                    );
                }
            }
        }

        return normal.normalized;
    }

    private Vector3 GetTriangleNormal(
    int a,
    int b,
    int c)
    {
        Vector3 edge1 =
            vertices[b] - vertices[a];

        Vector3 edge2 =
            vertices[c] - vertices[a];

        return Vector3.Cross(
            edge1,
            edge2
        ).normalized;
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