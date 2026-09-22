using UnityEngine;

/// <summary>
/// Genera y actualiza un collider local alrededor del jugador que reproduce la altura de la malla deformada del tablero.
/// </summary>
[RequireComponent(typeof(MeshCollider))]
public class LocalCollisionMesh : MonoBehaviour
{
    [Header("Grid visual")]
    [SerializeField] private GridSurfaceMesh gridSurfaceMesh;

    [Header("Jugador")]
    [SerializeField] private Transform xrOrigin;

    [Header("Tamaño del collider")]
    [Min(1)]
    [SerializeField] private int radius = 10;

    [Header("Resolución")]
    [Min(0.1f)]
    [SerializeField] private float cellSize = 1f;

    [Header("Detección de superficie")]
    [Tooltip("Altura desde la que se comprueba qué superficie está sosteniendo al jugador.")]
    [Min(0f)]
    [SerializeField] private float raycastStartOffset = 0.3f;

    [Tooltip("Distancia utilizada para buscar una superficie debajo del jugador.")]
    [Min(0.1f)]
    [SerializeField] private float surfaceCheckDistance = 2f;

    [Tooltip("Distancia máxima respecto a la superficie del grid para volver a activar su collider local.")]
    [Min(0.1f)]
    [SerializeField] private float maximumGridDistance = 1f;

    private Mesh collisionMesh;
    private MeshCollider meshCollider;
    private Vector3[] vertices;
    private int[] triangles;
    private bool gridCollisionActive = true;

    private Vector2Int previousPlayerCell = new Vector2Int(int.MinValue, int.MinValue);

    /// <summary>
    /// Obtiene el MeshCollider utilizado para representar la superficie de colisión local.
    /// </summary>
    private void Awake()
    {
        meshCollider = GetComponent<MeshCollider>();
    }

    /// <summary>
    /// Valida las referencias, genera la malla de colisión y actualiza su geometría y estado inicial.
    /// </summary>
    private void Start()
    {
        if (gridSurfaceMesh == null || xrOrigin == null)
        {
            Debug.LogWarning("Faltan referencias en LocalCollisionMesh.", this);
            enabled = false;
            return;
        }

        GenerateCollisionMesh();
        UpdateCollision();
        UpdateColliderState();
    }

    /// <summary>
    /// Comprueba el estado del collider y actualiza su geometría cuando el jugador cambia de casilla mientras la colisión está activa.
    /// </summary>
    private void Update()
    {
        if (xrOrigin == null || gridSurfaceMesh == null)
        {
            return;
        }

        UpdateColliderState();

        if (!gridCollisionActive)
        {
            return;
        }

        Vector2Int currentCell = GetPlayerCell();

        if (currentCell == previousPlayerCell)
        {
            return;
        }

        previousPlayerCell = currentCell;
        UpdateCollision();
    }

    /// <summary>
    /// Genera la malla utilizada por el collider local.
    /// </summary>
    private void GenerateCollisionMesh()
    {
        int size = radius * 2;
        int vertexWidth = size + 1;
        int vertexHeight = size + 1;

        vertices = new Vector3[vertexWidth * vertexHeight];

        for (int z = 0; z < vertexHeight; z++)
        {
            for (int x = 0; x < vertexWidth; x++)
            {
                int index = z * vertexWidth + x;

                vertices[index] = new Vector3((x - radius) * cellSize, 0f, (z - radius) * cellSize);
            }
        }

        triangles = new int[size * size * 6];

        int triangleIndex = 0;

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                int bottomLeft = z * vertexWidth + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * vertexWidth + x;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomRight;
            }
        }

        collisionMesh = new Mesh { name = "LocalCollisionMesh" };

        collisionMesh.MarkDynamic();
        collisionMesh.vertices = vertices;
        collisionMesh.triangles = triangles;
        collisionMesh.RecalculateNormals();
        collisionMesh.RecalculateBounds();

        meshCollider.sharedMesh = collisionMesh;
    }

    /// <summary>
    /// Activa o desactiva el collider local según la superficie que se encuentre debajo del jugador.
    /// </summary>
    private void UpdateColliderState()
    {
        if (!IsPlayerInsideGrid())
        {
            SetGridCollisionActive(false);
            return;
        }

        if (!gridCollisionActive)
        {
            if (!IsPlayerNearGridSurface())
            {
                return;
            }

            meshCollider.enabled = true;
            UpdateCollision();
            Physics.SyncTransforms();
        }

        Collider supportingCollider = GetSupportingCollider();
        bool supportedByGrid = supportingCollider == meshCollider;

        SetGridCollisionActive(supportedByGrid);
    }

    /// <summary>
    /// Obtiene el collider de la superficie más cercana situada bajo el jugador.
    /// </summary>
    private Collider GetSupportingCollider()
    {
        Vector3 upDirection = xrOrigin.up.normalized;
        Vector3 rayOrigin = xrOrigin.position + upDirection * raycastStartOffset;

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, -upDirection, surfaceCheckDistance + raycastStartOffset, ~0, QueryTriggerInteraction.Ignore);

        Collider closestCollider = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform == xrOrigin || hitCollider.transform.IsChildOf(xrOrigin))
            {
                continue;
            }

            if (hits[i].distance < closestDistance)
            {
                closestDistance = hits[i].distance;
                closestCollider = hitCollider;
            }
        }

        return closestCollider;
    }

    /// <summary>
    /// Cambia el estado del collider local del tablero.
    /// </summary>
    private void SetGridCollisionActive(bool shouldBeActive)
    {
        if (gridCollisionActive == shouldBeActive && meshCollider.enabled == shouldBeActive)
        {
            return;
        }

        gridCollisionActive = shouldBeActive;
        meshCollider.enabled = shouldBeActive;

        if (shouldBeActive)
        {
            previousPlayerCell = new Vector2Int(int.MinValue, int.MinValue);
        }
    }

    /// <summary>
    /// Comprueba si el jugador se encuentra dentro de los límites del tablero.
    /// </summary>
    private bool IsPlayerInsideGrid()
    {
        Vector3 localPosition = gridSurfaceMesh.transform.InverseTransformPoint(xrOrigin.position);

        float halfWidth = gridSurfaceMesh.Width * gridSurfaceMesh.CellSize * 0.5f;

        float halfDepth = gridSurfaceMesh.Height * gridSurfaceMesh.CellSize * 0.5f;

        return localPosition.x >= -halfWidth && localPosition.x <= halfWidth && localPosition.z >= -halfDepth && localPosition.z <= halfDepth;
    }

    /// <summary>
    /// Comprueba si el jugador está suficientemente cerca de la superficie del tablero como para reactivar su collider.
    /// </summary>
    private bool IsPlayerNearGridSurface()
    {
        Vector3 playerPosition = xrOrigin.position;
        float gridWorldHeight = GetGridHeight(playerPosition);
        float distance = Mathf.Abs(playerPosition.y - gridWorldHeight);

        return distance <= maximumGridDistance;
    }

    /// <summary>
    /// Centra el collider alrededor del jugador y adapta sus vértices a la altura actual del tablero.
    /// </summary>
    private void UpdateCollision()
    {
        if (gridSurfaceMesh == null || xrOrigin == null || collisionMesh == null)
        {
            return;
        }

        Vector3 playerPosition = xrOrigin.position;

        transform.position = new Vector3(playerPosition.x, gridSurfaceMesh.transform.position.y, playerPosition.z);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 localVertex = vertices[i];

            Vector3 worldPosition = transform.TransformPoint(new Vector3(localVertex.x, 0f, localVertex.z));

            float worldHeight = GetGridHeight(worldPosition);

            Vector3 collisionPosition = transform.InverseTransformPoint(new Vector3(worldPosition.x, worldHeight, worldPosition.z));

            vertices[i].y = collisionPosition.y;
        }

        collisionMesh.vertices = vertices;
        collisionMesh.RecalculateNormals();
        collisionMesh.RecalculateBounds();

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = collisionMesh;
    }

    /// <summary>
    /// Calcula la altura de la malla visual en una posición del mundo mediante interpolación entre los vértices de la casilla correspondiente.
    /// </summary>
    private float GetGridHeight(Vector3 worldPosition)
    {
        Vector3 localPosition = gridSurfaceMesh.transform.InverseTransformPoint(worldPosition);

        float gridWidth = gridSurfaceMesh.Width * gridSurfaceMesh.CellSize;

        float gridDepth = gridSurfaceMesh.Height * gridSurfaceMesh.CellSize;

        float offsetX = gridWidth * 0.5f;
        float offsetZ = gridDepth * 0.5f;

        float gridX = (localPosition.x + offsetX) / gridSurfaceMesh.CellSize;

        float gridZ = (localPosition.z + offsetZ) / gridSurfaceMesh.CellSize;

        if (gridX < 0f || gridX > gridSurfaceMesh.Width || gridZ < 0f || gridZ > gridSurfaceMesh.Height)
        {
            return gridSurfaceMesh.transform.TransformPoint(new Vector3(localPosition.x, 0f, localPosition.z)).y;
        }

        int x0 = Mathf.Clamp(Mathf.FloorToInt(gridX), 0, gridSurfaceMesh.Width);

        int z0 = Mathf.Clamp(Mathf.FloorToInt(gridZ), 0, gridSurfaceMesh.Height);

        int x1 = Mathf.Min(x0 + 1, gridSurfaceMesh.Width);
        int z1 = Mathf.Min(z0 + 1, gridSurfaceMesh.Height);

        float tx = gridX - x0;
        float tz = gridZ - z0;

        int vertexWidth = gridSurfaceMesh.Width + 1;

        int index00 = z0 * vertexWidth + x0;
        int index10 = z0 * vertexWidth + x1;
        int index01 = z1 * vertexWidth + x0;
        int index11 = z1 * vertexWidth + x1;

        Vector3[] gridVertices = gridSurfaceMesh.Vertices;

        float height00 = gridVertices[index00].y;
        float height10 = gridVertices[index10].y;
        float height01 = gridVertices[index01].y;
        float height11 = gridVertices[index11].y;

        float heightBottom = Mathf.Lerp(height00, height10, tx);
        float heightTop = Mathf.Lerp(height01, height11, tx);
        float localHeight = Mathf.Lerp(heightBottom, heightTop, tz);

        Vector3 localPoint = new Vector3(localPosition.x, localHeight, localPosition.z);

        return gridSurfaceMesh.transform.TransformPoint(localPoint).y;
    }

    /// <summary>
    /// Obtiene la casilla del tablero en la que se encuentra el jugador.
    /// </summary>
    private Vector2Int GetPlayerCell()
    {
        Vector3 localPosition = gridSurfaceMesh.transform.InverseTransformPoint(xrOrigin.position);

        float offsetX = gridSurfaceMesh.Width * gridSurfaceMesh.CellSize * 0.5f;

        float offsetZ = gridSurfaceMesh.Height * gridSurfaceMesh.CellSize * 0.5f;

        int x = Mathf.FloorToInt((localPosition.x + offsetX) / gridSurfaceMesh.CellSize);

        int z = Mathf.FloorToInt((localPosition.z + offsetZ) / gridSurfaceMesh.CellSize);

        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Elimina la malla de colisión generada al destruir el componente.
    /// </summary>
    private void OnDestroy()
    {
        if (collisionMesh != null)
        {
            Destroy(collisionMesh);
        }
    }
}
