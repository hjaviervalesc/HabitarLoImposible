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
    private Vector3[] originalVertices;
    private int[] triangles;

    private MeshCollider meshCollider;

    public Mesh Mesh => mesh;
    public Vector3[] Vertices => vertices;
    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    private void Awake()
    {
        GenerateMesh();
    }

    /// <summary>
    /// Genera una malla centrada en el origen del objeto.
    /// </summary>
    private void GenerateMesh()
    {
        int vertexWidth = width + 1;
        int vertexHeight = height + 1;

        vertices = new Vector3[
            vertexWidth * vertexHeight
        ];

        float offsetX = (width * cellSize) / 2f;
        float offsetZ = (height * cellSize) / 2f;

        // Generamos los vértices.
        for (int z = 0; z < vertexHeight; z++)
        {
            for (int x = 0; x < vertexWidth; x++)
            {
                int index =
                    z * vertexWidth + x;

                vertices[index] = new Vector3(
                    x * cellSize - offsetX,
                    0f,
                    z * cellSize - offsetZ
                );
            }
        }

        // Conservamos una copia sin deformar.
        originalVertices =
            (Vector3[])vertices.Clone();

        // Cada casilla está formada por dos triángulos.
        triangles = new int[
            width * height * 6
        ];

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

                // Primer triángulo.
                triangles[triangleIndex++] =
                    bottomLeft;

                triangles[triangleIndex++] =
                    topLeft;

                triangles[triangleIndex++] =
                    topRight;

                // Segundo triángulo.
                triangles[triangleIndex++] =
                    bottomLeft;

                triangles[triangleIndex++] =
                    topRight;

                triangles[triangleIndex++] =
                    bottomRight;
            }
        }

        mesh = new Mesh
        {
            name = "GridSurfaceMesh",
            indexFormat = IndexFormat.UInt32
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter meshFilter =
            GetComponent<MeshFilter>();

        meshFilter.mesh = mesh;

        meshCollider =
            GetComponent<MeshCollider>();

        meshCollider.sharedMesh = mesh;
    }

    /// <summary>
    /// Aplica las deformaciones guardadas en las secciones.
    /// La distancia se calcula respecto al centro de la casa.
    /// </summary>
    public void ApplySectionDeformations(
        List<DeformationSection> sections,
        int firstAffectedSection,
        int lastAffectedSection,
        Vector2 deformationCenter)
    {
        if (sections == null ||
            sections.Count == 0)
        {
            return;
        }

        firstAffectedSection = Mathf.Clamp(
            firstAffectedSection,
            0,
            sections.Count - 1
        );

        lastAffectedSection = Mathf.Clamp(
            lastAffectedSection,
            0,
            sections.Count - 1
        );

        if (firstAffectedSection >
            lastAffectedSection)
        {
            return;
        }

        // Actualizamos solamente los vértices
        // pertenecientes a las secciones modificadas.
        for (int s = firstAffectedSection;
             s <= lastAffectedSection;
             s++)
        {
            DeformationSection currentSection =
                sections[s];

            int nextIndex = Mathf.Min(
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

                Vector2 vertexPosition =
                    new Vector2(
                        originalVertex.x,
                        originalVertex.z
                    );

                // Distancia respecto a la casa,
                // no respecto al origen del grid.
                float distance =
                    Vector2.Distance(
                        vertexPosition,
                        deformationCenter
                    );

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

                // Interpolamos entre esta sección
                // y la sección siguiente.
                float deformation =
                    Mathf.Lerp(
                        currentSection.CurrentOffset,
                        nextSection.CurrentOffset,
                        t
                    );

                Vector3 deformedVertex =
                    originalVertex;

                deformedVertex.y += deformation;

                vertices[vertexIndex] =
                    deformedVertex;
            }
        }

        mesh.vertices = vertices;

        RecalculateAffectedNormals(
            sections,
            firstAffectedSection,
            lastAffectedSection
        );

        mesh.RecalculateBounds();

        UpdateMeshCollider();
    }

    /// <summary>
    /// Actualiza el collider para que el jugador pueda caminar
    /// sobre la superficie deformada.
    /// </summary>
    private void UpdateMeshCollider()
    {
        if (meshCollider == null)
        {
            meshCollider =
                GetComponent<MeshCollider>();
        }

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    /// <summary>
    /// Recalcula las normales de las secciones modificadas
    /// y de sus vértices vecinos.
    /// </summary>
    private void RecalculateAffectedNormals(
        List<DeformationSection> sections,
        int firstAffectedSection,
        int lastAffectedSection)
    {
        Vector3[] normals = mesh.normals;

        int vertexWidth = width + 1;

        HashSet<int> affectedVertices =
            new HashSet<int>();

        for (int s = firstAffectedSection;
             s <= lastAffectedSection;
             s++)
        {
            foreach (int vertexIndex
                     in sections[s].VertexIndices)
            {
                affectedVertices.Add(vertexIndex);

                int x =
                    vertexIndex % vertexWidth;

                int z =
                    vertexIndex / vertexWidth;

                if (x > 0)
                {
                    affectedVertices.Add(
                        vertexIndex - 1
                    );
                }

                if (x < width)
                {
                    affectedVertices.Add(
                        vertexIndex + 1
                    );
                }

                if (z > 0)
                {
                    affectedVertices.Add(
                        vertexIndex - vertexWidth
                    );
                }

                if (z < height)
                {
                    affectedVertices.Add(
                        vertexIndex + vertexWidth
                    );
                }
            }
        }

        foreach (int vertexIndex
                 in affectedVertices)
        {
            normals[vertexIndex] =
                CalculateVertexNormal(vertexIndex);
        }

        mesh.normals = normals;
    }

    /// <summary>
    /// Calcula la normal de un vértice a partir
    /// de los triángulos que lo rodean.
    /// </summary>
    private Vector3 CalculateVertexNormal(
        int vertexIndex)
    {
        Vector3 normal = Vector3.zero;

        int vertexWidth = width + 1;

        int x =
            vertexIndex % vertexWidth;

        int z =
            vertexIndex / vertexWidth;

        for (int dz = -1; dz <= 0; dz++)
        {
            for (int dx = -1; dx <= 0; dx++)
            {
                int cellX = x + dx;
                int cellZ = z + dz;

                if (cellX < 0 ||
                    cellX >= width ||
                    cellZ < 0 ||
                    cellZ >= height)
                {
                    continue;
                }

                int bottomLeft =
                    cellZ * vertexWidth + cellX;

                int bottomRight =
                    bottomLeft + 1;

                int topLeft =
                    bottomLeft + vertexWidth;

                int topRight =
                    topLeft + 1;

                // Primer triángulo.
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

                // Segundo triángulo.
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

        if (normal.sqrMagnitude < 0.000001f)
        {
            return Vector3.up;
        }

        return normal.normalized;
    }

    /// <summary>
    /// Calcula la normal de un triángulo.
    /// </summary>
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
}