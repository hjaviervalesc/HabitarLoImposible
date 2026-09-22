using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridSurfaceMesh))]

/// <summary>
/// Genera barreras invisibles alrededor de los bordes del tablero,
/// dejando aberturas en las posiciones de las escaleras.
/// </summary>
public class GridInvisibleBarrier : MonoBehaviour
{
    private enum GridEdge
    {
        North,
        South,
        East,
        West
    }

    [System.Serializable]
    private class StairOpening
    {
        [Tooltip("Collider que delimita la abertura de las escaleras.")]
        public BoxCollider openingCollider;

        [Tooltip("Borde del tablero en el que están las escaleras.")]
        public GridEdge edge = GridEdge.North;

        [Tooltip("Espacio adicional dejado a ambos lados.")]
        [Min(0f)]
        public float padding = 0.5f;
    }

    private struct Interval
    {
        public float Start;
        public float End;

        public Interval(float start, float end)
        {
            Start = start;
            End = end;
        }
    }

    [Header("Referencias")]
    [SerializeField] private GridSurfaceMesh gridSurface;

    [Header("Dimensiones de la barrera")]
    [Min(0.01f)]
    [SerializeField] private float barrierThickness = 0.2f;

    [Min(0.1f)]
    [SerializeField] private float barrierHeight = 2f;

    [Tooltip("Altura de la base de la barrera respecto al grid.")]
    [SerializeField] private float baseHeight = 0f;

    [Header("Aberturas para escaleras")]
    [SerializeField] private List<StairOpening> stairOpenings = new List<StairOpening>();

    [Header("Generación")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private string containerName = "InvisibleBarriers";

    private Transform barriersContainer;

    private void Awake()
    {
        if (gridSurface == null)
        {
            gridSurface = GetComponent<GridSurfaceMesh>();
        }
    }

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateBarriers();
        }
    }

    /// <summary>
    /// Genera las barreras de los cuatro bordes del tablero.
    /// </summary>
    [ContextMenu("Generate Barriers")]
    public void GenerateBarriers()
    {
        if (gridSurface == null)
        {
            Debug.LogWarning("No se ha encontrado GridSurfaceMesh.", this);
            return;
        }

        ClearBarriers();

        GameObject container = new GameObject(containerName);
        container.transform.SetParent(transform, false);
        barriersContainer = container.transform;

        float gridWidth = gridSurface.Width * gridSurface.CellSize;
        float gridDepth = gridSurface.Height * gridSurface.CellSize;

        GenerateEdge(GridEdge.North, gridWidth, gridDepth);
        GenerateEdge(GridEdge.South, gridWidth, gridDepth);
        GenerateEdge(GridEdge.East, gridWidth, gridDepth);
        GenerateEdge(GridEdge.West, gridWidth, gridDepth);
    }

    /// <summary>
    /// Genera los tramos de barrera disponibles en uno de los bordes.
    /// </summary>
    private void GenerateEdge(GridEdge edge, float gridWidth, float gridDepth)
    {
        bool horizontal = edge == GridEdge.North || edge == GridEdge.South;
        float edgeLength = horizontal ? gridWidth : gridDepth;
        float edgeStart = -edgeLength * 0.5f;
        float edgeEnd = edgeLength * 0.5f;

        List<Interval> blockedIntervals = GetBlockedIntervals(edge, horizontal, edgeStart, edgeEnd);
        float availableStart = edgeStart;

        for (int i = 0; i < blockedIntervals.Count; i++)
        {
            Interval blocked = blockedIntervals[i];

            if (blocked.Start > availableStart)
            {
                CreateBarrierSection(edge, availableStart, blocked.Start, gridWidth, gridDepth);
            }

            availableStart = Mathf.Max(availableStart, blocked.End);
        }

        if (availableStart < edgeEnd)
        {
            CreateBarrierSection(edge, availableStart, edgeEnd, gridWidth, gridDepth);
        }
    }

    /// <summary>
    /// Obtiene los intervalos que deben permanecer abiertos para las escaleras.
    /// </summary>
    private List<Interval> GetBlockedIntervals(GridEdge edge, bool horizontal, float edgeStart, float edgeEnd)
    {
        List<Interval> intervals = new List<Interval>();

        for (int i = 0; i < stairOpenings.Count; i++)
        {
            StairOpening opening = stairOpenings[i];

            if (opening == null || opening.openingCollider == null || opening.edge != edge)
            {
                continue;
            }

            Bounds worldBounds = opening.openingCollider.bounds;
            Vector3 localMinimum = transform.InverseTransformPoint(worldBounds.min);
            Vector3 localMaximum = transform.InverseTransformPoint(worldBounds.max);

            float openingStart;
            float openingEnd;

            if (horizontal)
            {
                openingStart = Mathf.Min(localMinimum.x, localMaximum.x);
                openingEnd = Mathf.Max(localMinimum.x, localMaximum.x);
            }
            else
            {
                openingStart = Mathf.Min(localMinimum.z, localMaximum.z);
                openingEnd = Mathf.Max(localMinimum.z, localMaximum.z);
            }

            openingStart -= opening.padding;
            openingEnd += opening.padding;

            openingStart = Mathf.Clamp(openingStart, edgeStart, edgeEnd);
            openingEnd = Mathf.Clamp(openingEnd, edgeStart, edgeEnd);

            if (openingEnd > openingStart)
            {
                intervals.Add(new Interval(openingStart, openingEnd));
            }
        }

        intervals.Sort((a, b) => a.Start.CompareTo(b.Start));

        return MergeIntervals(intervals);
    }

    /// <summary>
    /// Combina los intervalos de abertura que se solapan.
    /// </summary>
    private List<Interval> MergeIntervals(List<Interval> intervals)
    {
        List<Interval> merged = new List<Interval>();

        for (int i = 0; i < intervals.Count; i++)
        {
            Interval current = intervals[i];

            if (merged.Count == 0)
            {
                merged.Add(current);
                continue;
            }

            int lastIndex = merged.Count - 1;
            Interval previous = merged[lastIndex];

            if (current.Start <= previous.End)
            {
                previous.End = Mathf.Max(previous.End, current.End);
                merged[lastIndex] = previous;
            }
            else
            {
                merged.Add(current);
            }
        }

        return merged;
    }

    /// <summary>
    /// Crea un tramo de barrera mediante un BoxCollider invisible.
    /// </summary>
    private void CreateBarrierSection(GridEdge edge, float sectionStart, float sectionEnd, float gridWidth, float gridDepth)
    {
        float sectionLength = sectionEnd - sectionStart;

        if (sectionLength <= 0.001f)
        {
            return;
        }

        float sectionCenter = (sectionStart + sectionEnd) * 0.5f;
        float halfWidth = gridWidth * 0.5f;
        float halfDepth = gridDepth * 0.5f;

        Vector3 localPosition;
        Vector3 colliderSize;

        switch (edge)
        {
            case GridEdge.North:
                localPosition = new Vector3(sectionCenter, baseHeight + barrierHeight * 0.5f, halfDepth);
                colliderSize = new Vector3(sectionLength, barrierHeight, barrierThickness);
                break;

            case GridEdge.South:
                localPosition = new Vector3(sectionCenter, baseHeight + barrierHeight * 0.5f, -halfDepth);
                colliderSize = new Vector3(sectionLength, barrierHeight, barrierThickness);
                break;

            case GridEdge.East:
                localPosition = new Vector3(halfWidth, baseHeight + barrierHeight * 0.5f, sectionCenter);
                colliderSize = new Vector3(barrierThickness, barrierHeight, sectionLength);
                break;

            default:
                localPosition = new Vector3(-halfWidth, baseHeight + barrierHeight * 0.5f, sectionCenter);
                colliderSize = new Vector3(barrierThickness, barrierHeight, sectionLength);
                break;
        }

        GameObject barrier = new GameObject(edge + "_Barrier");
        barrier.transform.SetParent(barriersContainer, false);
        barrier.transform.localPosition = localPosition;

        BoxCollider boxCollider = barrier.AddComponent<BoxCollider>();
        boxCollider.size = colliderSize;
        boxCollider.isTrigger = false;
    }

    /// <summary>
    /// Elimina todas las barreras generadas anteriormente.
    /// </summary>
    [ContextMenu("Clear Barriers")]
    public void ClearBarriers()
    {
        Transform existingContainer = transform.Find(containerName);

        if (existingContainer == null)
        {
            barriersContainer = null;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existingContainer.gameObject);
        }
        else
        {
            DestroyImmediate(existingContainer.gameObject);
        }

        barriersContainer = null;
    }
}