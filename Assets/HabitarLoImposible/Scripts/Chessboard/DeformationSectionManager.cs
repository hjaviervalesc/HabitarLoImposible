using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Divide la malla en secciones concéntricas y controla progresivamente su deformación según el desplazamiento del jugador respecto a la casa.
/// </summary>
public class DeformationSectionManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField]
    private GridSurfaceMesh gridSurfaceMesh;

    [Header("Centro de la deformación")]
    [Tooltip("Objeto colocado en el centro de la casa.")]
    [SerializeField]
    private Transform houseCenter;

    [Header("Jugador")]
    [SerializeField]
    private Transform xrOrigin;

    [Header("Secciones")]
    [Min(0.01f)]
    [SerializeField]
    private float sectionWidth = 1f;

    [Header("Deformación")]
    [Tooltip("Altura añadida al atravesar cada sección hacia la casa.")]
    [Min(0f)]
    [SerializeField]
    private float deformationPerSection = 0.005f;

    [Tooltip("Tiempo aproximado que tarda en aplicarse cada incremento.")]
    [Min(0.01f)]
    [SerializeField]
    private float deformationDuration = 3f;

    [Tooltip("Número de secciones próximas al jugador que no se modifican.")]
    [Min(0)]
    [SerializeField]
    private int protectedSections = 2;

    [Header("Rendimiento")]
    [Tooltip("Tiempo entre actualizaciones de la malla. " + "Un valor de 0.1 realiza unas 10 actualizaciones por segundo.")]
    [Min(0.02f)]
    [SerializeField]
    private float deformationUpdateInterval = 0.1f;

    private readonly List<DeformationSection> sections = new List<DeformationSection>();

    private readonly List<float> targetOffsets = new List<float>();

    private Vector2 localDeformationCenter;

    private int previousPlayerSection = -1;

    private float deformationUpdateTimer;

    public List<DeformationSection> Sections => sections;

    [Header("Visualización de secciones")]
    [SerializeField]
    private bool showSectionGizmos = true;

    [Range(16, 128)]
    [SerializeField]
    private int gizmoCircleSegments = 64;

    [Min(0f)]
    [SerializeField]
    private float gizmoHeightOffset = 0.05f;

    /// <summary>
    /// Comprueba las referencias, calcula el centro y crea las secciones de la malla.
    /// </summary>
    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        CalculateLocalDeformationCenter();
        CreateSections();

        if (sections.Count == 0)
        {
            Debug.LogError("No se ha podido crear ninguna sección.", this);

            enabled = false;
            return;
        }

        InitializeTargetOffsets();

        previousPlayerSection = GetPlayerSection();
    }

    /// <summary>
    /// Detecta el cambio de sección del jugador y actualiza la malla únicamente cuando transcurre el intervalo.
    /// </summary>
    private void Update()
    {
        int currentPlayerSection = GetPlayerSection();

        if (currentPlayerSection != previousPlayerSection)
        {
            UpdateTargetDeformation(previousPlayerSection, currentPlayerSection);

            previousPlayerSection = currentPlayerSection;
        }

        deformationUpdateTimer += Time.deltaTime;

        if (deformationUpdateTimer < deformationUpdateInterval)
        {
            return;
        }

        float elapsedTime = deformationUpdateTimer;

        deformationUpdateTimer = 0f;

        SmoothDeformation(elapsedTime);
    }

    /// <summary>
    /// Comprueba que todas las referencias necesarias estén asignadas.
    /// </summary>
    private bool ValidateReferences()
    {
        if (gridSurfaceMesh == null)
        {
            Debug.LogError("No se ha asignado GridSurfaceMesh.", this);

            return false;
        }

        if (houseCenter == null)
        {
            Debug.LogError("No se ha asignado HouseCenter.", this);

            return false;
        }

        if (xrOrigin == null)
        {
            Debug.LogError("No se ha asignado XR Origin.", this);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Convierte la posición de la casa al espacio local de la malla.
    /// </summary>
    private void CalculateLocalDeformationCenter()
    {
        Vector3 localHousePosition = gridSurfaceMesh.transform.InverseTransformPoint(houseCenter.position);

        localDeformationCenter = new Vector2(localHousePosition.x, localHousePosition.z);
    }

    /// <summary>
    /// Inicializa la deformación objetivo de las secciones.
    /// </summary>
    private void InitializeTargetOffsets()
    {
        targetOffsets.Clear();

        for (int i = 0; i < sections.Count; i++)
        {
            targetOffsets.Add(sections[i].CurrentOffset);
        }
    }

    /// <summary>
    /// Modifica la deformación que las secciones deberán alcanzar progresivamente.
    /// </summary>
    private void UpdateTargetDeformation(int previousSection, int currentSection)
    {
        if (currentSection < previousSection)
        {
            for (int crossedSection = previousSection - 1; crossedSection >= currentSection; crossedSection--)
            {
                AddTargetDeformationUntilSection(crossedSection - protectedSections, deformationPerSection);
            }
        }
        else
        {
            for (int crossedSection = previousSection; crossedSection < currentSection; crossedSection++)
            {
                AddTargetDeformationUntilSection(crossedSection - protectedSections, -deformationPerSection);
            }
        }
    }

    /// <summary>
    /// Añade una deformación objetivo desde la sección central hasta la sección indicada.
    /// </summary>
    private void AddTargetDeformationUntilSection(int lastSection, float amount)
    {
        if (lastSection < 0)
        {
            return;
        }

        lastSection = Mathf.Clamp(lastSection, 0, sections.Count - 1);

        for (int i = 0; i <= lastSection; i++)
        {
            targetOffsets[i] += amount;
        }
    }

    /// <summary>
    /// Acerca gradualmente la deformación actual de cada sección a su deformación objetivo. Este método no se ejecuta en todos los fotogramas, sino según el intervalo de actualización indicado.
    /// </summary>
    private void SmoothDeformation(float elapsedTime)
    {
        if (sections.Count == 0 || targetOffsets.Count != sections.Count)
        {
            return;
        }

        float deformationSpeed = deformationPerSection / Mathf.Max(deformationDuration, 0.01f);

        float maximumChange = deformationSpeed * elapsedTime;

        int firstChangedSection = sections.Count;

        int lastChangedSection = -1;

        for (int i = 0; i < sections.Count; i++)
        {
            float currentOffset = sections[i].CurrentOffset;

            float newOffset = Mathf.MoveTowards(currentOffset, targetOffsets[i], maximumChange);

            float offsetDifference = newOffset - currentOffset;

            if (Mathf.Abs(offsetDifference) < 0.00001f)
            {
                continue;
            }

            sections[i].AddDeformation(offsetDifference);

            firstChangedSection = Mathf.Min(firstChangedSection, i);

            lastChangedSection = Mathf.Max(lastChangedSection, i);
        }

        if (lastChangedSection < 0)
        {
            return;
        }

        gridSurfaceMesh.ApplySectionDeformations(sections, firstChangedSection, lastChangedSection, localDeformationCenter);
    }

    /// <summary>
    /// Activa o desactiva el sistema de deformación. Al reactivarlo, actualiza la sección del jugador para evitar procesar de golpe todas las secciones atravesadas mientras el componente estaba desactivado.
    /// </summary>
    public void SetDeformationActive(bool active)
    {
        if (active && sections.Count > 0)
        {
            previousPlayerSection = GetPlayerSection();

            deformationUpdateTimer = 0f;
        }

        enabled = active;
    }

    /// <summary>
    /// Obtiene la sección concéntrica en la que se encuentra el jugador.
    /// </summary>
    private int GetPlayerSection()
    {
        Vector3 localPlayerPosition = gridSurfaceMesh.transform.InverseTransformPoint(xrOrigin.position);

        Vector2 playerXZ = new Vector2(localPlayerPosition.x, localPlayerPosition.z);

        float distance = Vector2.Distance(playerXZ, localDeformationCenter);

        int sectionIndex = Mathf.FloorToInt(distance / sectionWidth);

        return Mathf.Clamp(sectionIndex, 0, sections.Count - 1);
    }

    /// <summary>
    /// Crea las secciones concéntricas y asigna cada vértice a una de ellas.
    /// </summary>
    private void CreateSections()
    {
        sections.Clear();

        Vector3[] vertices = gridSurfaceMesh.Vertices;

        if (vertices == null || vertices.Length == 0)
        {
            return;
        }

        float maxRadius = 0f;

        foreach (Vector3 vertex in vertices)
        {
            Vector2 vertexXZ = new Vector2(vertex.x, vertex.z);

            float distance = Vector2.Distance(vertexXZ, localDeformationCenter);

            if (distance > maxRadius)
            {
                maxRadius = distance;
            }
        }

        int sectionCount = Mathf.Max(1, Mathf.CeilToInt(maxRadius / sectionWidth));

        for (int i = 0; i < sectionCount; i++)
        {
            float innerRadius = i * sectionWidth;

            float outerRadius = (i + 1) * sectionWidth;

            DeformationSection section = new DeformationSection(i, innerRadius, outerRadius);

            sections.Add(section);
        }

        for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
        {
            Vector3 vertex = vertices[vertexIndex];

            Vector2 vertexXZ = new Vector2(vertex.x, vertex.z);

            float distance = Vector2.Distance(vertexXZ, localDeformationCenter);

            int sectionIndex = Mathf.FloorToInt(distance / sectionWidth);

            sectionIndex = Mathf.Clamp(sectionIndex, 0, sections.Count - 1);

            sections[sectionIndex].AddVertex(vertexIndex);
        }
    }

    /// <summary>
    /// Obtiene en coordenadas mundiales la deformación existente en una posición determinada.
    /// </summary>
    public Vector3 GetWorldDeformationAtPosition(Vector3 worldPosition)
    {
        if (sections.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 localPosition = gridSurfaceMesh.transform.InverseTransformPoint(worldPosition);

        Vector2 positionXZ = new Vector2(localPosition.x, localPosition.z);

        float distance = Vector2.Distance(positionXZ, localDeformationCenter);

        int sectionIndex = Mathf.FloorToInt(distance / sectionWidth);

        sectionIndex = Mathf.Clamp(sectionIndex, 0, sections.Count - 1);

        DeformationSection currentSection = sections[sectionIndex];

        int nextSectionIndex = Mathf.Min(sectionIndex + 1, sections.Count - 1);

        DeformationSection nextSection = sections[nextSectionIndex];

        float t = 0f;

        if (currentSection.OuterRadius > currentSection.InnerRadius)
        {
            t = Mathf.InverseLerp(currentSection.InnerRadius, currentSection.OuterRadius, distance);
        }

        float localDeformation = Mathf.Lerp(currentSection.CurrentOffset, nextSection.CurrentOffset, t);

        return gridSurfaceMesh.transform.TransformVector(Vector3.up * localDeformation);
    }

    /// <summary>
    /// Dibuja las secciones concéntricas con distintos colores y marca el centro de la deformación al seleccionar el objeto.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showSectionGizmos || gridSurfaceMesh == null || houseCenter == null || sectionWidth <= 0f)
        {
            return;
        }

        Vector3 localHousePosition = gridSurfaceMesh.transform.InverseTransformPoint(houseCenter.position);

        Vector2 center = new Vector2(localHousePosition.x, localHousePosition.z);

        int sectionCount = GetGizmoSectionCount(center);

        if (sectionCount <= 0)
        {
            return;
        }

        for (int i = 0; i < sectionCount; i++)
        {
            float innerRadius = i * sectionWidth;

            float outerRadius = (i + 1) * sectionWidth;

            float middleRadius = (innerRadius + outerRadius) * 0.5f;

            float hue = i / (float)sectionCount;

            Color sectionColor = Color.HSVToRGB(hue, 0.8f, 1f);

            sectionColor.a = 0.9f;

            Gizmos.color = sectionColor;

            DrawLocalCircle(center, middleRadius);

            DrawLocalCircle(center, outerRadius);
        }

        Vector3 worldCenter = gridSurfaceMesh.transform.TransformPoint(new Vector3(center.x, gizmoHeightOffset, center.y));

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(worldCenter, sectionWidth * 0.15f);
    }

    /// <summary>
    /// Obtiene el número de secciones existentes o lo calcula a partir de los vértices de la malla para su visualización.
    /// </summary>
    private int GetGizmoSectionCount(Vector2 center)
    {
        if (sections.Count > 0)
        {
            return sections.Count;
        }

        Vector3[] vertices = gridSurfaceMesh.Vertices;

        if (vertices == null || vertices.Length == 0)
        {
            return 0;
        }

        float maximumRadius = 0f;

        foreach (Vector3 vertex in vertices)
        {
            Vector2 vertexPosition = new Vector2(vertex.x, vertex.z);

            float distance = Vector2.Distance(vertexPosition, center);

            maximumRadius = Mathf.Max(maximumRadius, distance);
        }

        return Mathf.Max(1, Mathf.CeilToInt(maximumRadius / sectionWidth));
    }

    /// <summary>
    /// Dibuja un círculo mediante segmentos en coordenadas mundiales a partir de su centro y radio locales.
    /// </summary>
    private void DrawLocalCircle(Vector2 center, float radius)
    {
        if (radius <= 0f)
        {
            return;
        }

        Vector3 previousPoint = GetCircleWorldPoint(center, radius, 0f);

        for (int i = 1; i <= gizmoCircleSegments; i++)
        {
            float angle = i / (float)gizmoCircleSegments * Mathf.PI * 2f;

            Vector3 currentPoint = GetCircleWorldPoint(center, radius, angle);

            Gizmos.DrawLine(previousPoint, currentPoint);

            previousPoint = currentPoint;
        }
    }

    /// <summary>
    /// Calcula un punto del círculo en coordenadas locales y lo transforma a coordenadas mundiales.
    /// </summary>
    private Vector3 GetCircleWorldPoint(Vector2 center, float radius, float angle)
    {
        Vector3 localPoint = new Vector3(center.x + Mathf.Cos(angle) * radius, gizmoHeightOffset, center.y + Mathf.Sin(angle) * radius);

        return gridSurfaceMesh.transform.TransformPoint(localPoint);
    }
}