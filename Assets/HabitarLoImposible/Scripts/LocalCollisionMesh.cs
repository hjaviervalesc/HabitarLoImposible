using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class LocalCollisionMesh : MonoBehaviour
{
    [Header("Grid visual")]
    [SerializeField] private GridSurfaceMesh gridSurfaceMesh;

    [Header("Jugador")]
    [SerializeField] private Transform xrOrigin;

    [Header("Tamaño del collider")]
    [SerializeField] private int radius = 10;

    [Header("Resolución")]
    [SerializeField] private float cellSize = 1f;


    private Mesh collisionMesh;
    private MeshCollider meshCollider;

    private Vector3[] vertices;
    private int[] triangles;

    private Vector2Int previousPlayerCell =
        new Vector2Int(int.MinValue, int.MinValue);


    private void Start()
    {
        meshCollider = GetComponent<MeshCollider>();

        GenerateCollisionMesh();

        UpdateCollision();
    }


    private void Update()
    {
        if (xrOrigin == null)
            return;

        Vector2Int currentCell =
            GetPlayerCell();

        if (currentCell == previousPlayerCell)
            return;

        previousPlayerCell = currentCell;

        UpdateCollision();
    }


    // =========================================================
    // GENERACIÓN DE LA MALLA
    // =========================================================

    private void GenerateCollisionMesh()
    {
        int size = radius * 2;

        int vertexWidth = size + 1;
        int vertexHeight = size + 1;


        // -----------------------------------------------------
        // VÉRTICES
        // -----------------------------------------------------

        vertices = new Vector3[
            vertexWidth * vertexHeight
        ];


        for (int z = 0; z < vertexHeight; z++)
        {
            for (int x = 0; x < vertexWidth; x++)
            {
                int index =
                    z * vertexWidth + x;

                vertices[index] = new Vector3(
                    (x - radius) * cellSize,
                    0f,
                    (z - radius) * cellSize
                );
            }
        }


        // -----------------------------------------------------
        // TRIÁNGULOS
        // -----------------------------------------------------

        triangles =
            new int[size * size * 6];

        int triangleIndex = 0;


        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
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


        // -----------------------------------------------------
        // MESH
        // -----------------------------------------------------

        collisionMesh =
            new Mesh();

        collisionMesh.name =
            "LocalCollisionMesh";

        collisionMesh.vertices =
            vertices;

        collisionMesh.triangles =
            triangles;

        collisionMesh.RecalculateNormals();
        collisionMesh.RecalculateBounds();

        meshCollider.sharedMesh =
            collisionMesh;
    }


    // =========================================================
    // ACTUALIZACIÓN
    // =========================================================

    private void UpdateCollision()
    {
        if (gridSurfaceMesh == null)
            return;

        if (xrOrigin == null)
            return;


        // -----------------------------------------------------
        // Colocamos el collider alrededor del jugador
        // -----------------------------------------------------

        Vector3 playerPosition =
            xrOrigin.position;

        transform.position =
            new Vector3(
                playerPosition.x,
                0f,
                playerPosition.z
            );


        // -----------------------------------------------------
        // Actualizamos la altura de cada vértice
        // -----------------------------------------------------

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 localVertex =
                vertices[i];


            // Posición del vértice en el mundo
            Vector3 worldPosition =
                transform.TransformPoint(
                    new Vector3(
                        localVertex.x,
                        0f,
                        localVertex.z
                    )
                );


            // Altura de la malla visual
            float worldHeight =
                GetGridHeight(worldPosition);


            // Convertimos la posición mundial
            // a coordenadas locales del collider
            Vector3 collisionPosition =
                transform.InverseTransformPoint(
                    new Vector3(
                        worldPosition.x,
                        worldHeight,
                        worldPosition.z
                    )
                );


            vertices[i].y =
                collisionPosition.y;
        }


        // -----------------------------------------------------
        // Aplicamos los vértices
        // -----------------------------------------------------

        collisionMesh.vertices =
            vertices;

        collisionMesh.RecalculateNormals();
        collisionMesh.RecalculateBounds();


        // -----------------------------------------------------
        // Actualizamos el MeshCollider
        // -----------------------------------------------------

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = collisionMesh;
    }


    // =========================================================
    // ALTURA DE LA MALLA VISUAL
    // =========================================================

    private float GetGridHeight(Vector3 worldPosition)
    {
        Vector3 localPosition =
            gridSurfaceMesh.transform.InverseTransformPoint(
                worldPosition
            );


        float gridWidth =
            gridSurfaceMesh.Width *
            gridSurfaceMesh.CellSize;

        float gridHeight =
            gridSurfaceMesh.Height *
            gridSurfaceMesh.CellSize;


        float offsetX =
            gridWidth / 2f;

        float offsetZ =
            gridHeight / 2f;


        // Coordenadas dentro del grid
        float gridX =
            (localPosition.x + offsetX) /
            gridSurfaceMesh.CellSize;

        float gridZ =
            (localPosition.z + offsetZ) /
            gridSurfaceMesh.CellSize;


        // -----------------------------------------------------
        // Comprobamos que estamos dentro del grid
        // -----------------------------------------------------

        if (gridX < 0f ||
            gridX > gridSurfaceMesh.Width ||
            gridZ < 0f ||
            gridZ > gridSurfaceMesh.Height)
        {
            return gridSurfaceMesh.transform
                .TransformPoint(
                    new Vector3(
                        localPosition.x,
                        0f,
                        localPosition.z
                    )
                ).y;
        }


        // -----------------------------------------------------
        // Vértices que rodean la posición
        // -----------------------------------------------------

        int x0 =
            Mathf.FloorToInt(gridX);

        int z0 =
            Mathf.FloorToInt(gridZ);

        int x1 =
            Mathf.Min(
                x0 + 1,
                gridSurfaceMesh.Width
            );

        int z1 =
            Mathf.Min(
                z0 + 1,
                gridSurfaceMesh.Height
            );


        float tx =
            gridX - x0;

        float tz =
            gridZ - z0;


        int vertexWidth =
            gridSurfaceMesh.Width + 1;


        int index00 =
            z0 * vertexWidth + x0;

        int index10 =
            z0 * vertexWidth + x1;

        int index01 =
            z1 * vertexWidth + x0;

        int index11 =
            z1 * vertexWidth + x1;


        Vector3[] gridVertices =
            gridSurfaceMesh.Vertices;


        // -----------------------------------------------------
        // Interpolación bilineal de la altura
        // -----------------------------------------------------

        float height00 =
            gridVertices[index00].y;

        float height10 =
            gridVertices[index10].y;

        float height01 =
            gridVertices[index01].y;

        float height11 =
            gridVertices[index11].y;


        float heightBottom =
            Mathf.Lerp(
                height00,
                height10,
                tx
            );

        float heightTop =
            Mathf.Lerp(
                height01,
                height11,
                tx
            );

        float localHeight =
            Mathf.Lerp(
                heightBottom,
                heightTop,
                tz
            );


        // Convertimos la altura local de GridSurfaceMesh
        // a altura mundial
        Vector3 localPoint =
            new Vector3(
                localPosition.x,
                localHeight,
                localPosition.z
            );

        Vector3 worldPoint =
            gridSurfaceMesh.transform.TransformPoint(
                localPoint
            );


        return worldPoint.y;
    }


    // =========================================================
    // POSICIÓN DEL JUGADOR
    // =========================================================

    private Vector2Int GetPlayerCell()
    {
        Vector3 localPosition =
            gridSurfaceMesh.transform.InverseTransformPoint(
                xrOrigin.position
            );


        float offsetX =
            (gridSurfaceMesh.Width *
            gridSurfaceMesh.CellSize) / 2f;

        float offsetZ =
            (gridSurfaceMesh.Height *
            gridSurfaceMesh.CellSize) / 2f;


        int x =
            Mathf.FloorToInt(
                (localPosition.x + offsetX) /
                gridSurfaceMesh.CellSize
            );

        int z =
            Mathf.FloorToInt(
                (localPosition.z + offsetZ) /
                gridSurfaceMesh.CellSize
            );


        return new Vector2Int(x, z);
    }


    // =========================================================
    // ACTUALIZACIÓN EXTERNA
    // =========================================================

    public void RefreshCollision()
    {
        UpdateCollision();
    }
}
