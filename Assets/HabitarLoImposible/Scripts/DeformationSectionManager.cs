using System.Collections.Generic;
using UnityEngine;

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
    [Tooltip(
        "Altura añadida al atravesar cada sección hacia la casa."
    )]
    [Min(0f)]
    [SerializeField]
    private float deformationPerSection = 0.01f;

    private readonly List<DeformationSection> sections =
        new List<DeformationSection>();

    private Vector2 localDeformationCenter;
    private int previousPlayerSection = -1;

    public List<DeformationSection> Sections =>
        sections;

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
            Debug.LogError(
                "No se ha podido crear ninguna sección.",
                this
            );

            enabled = false;
            return;
        }

        previousPlayerSection =
            GetPlayerSection();
    }

    private void Update()
    {
        int currentPlayerSection =
            GetPlayerSection();

        if (currentPlayerSection ==
            previousPlayerSection)
        {
            return;
        }

        UpdateDeformation(
            previousPlayerSection,
            currentPlayerSection
        );

        previousPlayerSection =
            currentPlayerSection;
    }

    private bool ValidateReferences()
    {
        if (gridSurfaceMesh == null)
        {
            Debug.LogError(
                "No se ha asignado GridSurfaceMesh.",
                this
            );

            return false;
        }

        if (houseCenter == null)
        {
            Debug.LogError(
                "No se ha asignado HouseCenter.",
                this
            );

            return false;
        }

        if (xrOrigin == null)
        {
            Debug.LogError(
                "No se ha asignado XR Origin.",
                this
            );

            return false;
        }

        return true;
    }

    /// <summary>
    /// Convierte la posición de la casa al espacio
    /// local de la malla.
    /// </summary>
    private void CalculateLocalDeformationCenter()
    {
        Vector3 localHousePosition =
            gridSurfaceMesh.transform
                .InverseTransformPoint(
                    houseCenter.position
                );

        localDeformationCenter =
            new Vector2(
                localHousePosition.x,
                localHousePosition.z
            );
    }

    /// <summary>
    /// Eleva las secciones cuando el jugador se acerca
    /// y las baja cuando se aleja.
    /// </summary>
    private void UpdateDeformation(
        int previousSection,
        int currentSection)
    {
        int firstAffectedSection = 0;
        int lastAffectedSection = 0;

        // El jugador se acerca a la casa.
        if (currentSection < previousSection)
        {
            /*
             * Reproducimos todos los cruces de sección.
             * Esto también funciona si el jugador avanza
             * varias secciones en un único frame.
             */
            for (int crossedSection =
                     previousSection - 1;
                 crossedSection >= currentSection;
                 crossedSection--)
            {
                AddDeformationUntilSection(
                    crossedSection,
                    deformationPerSection
                );
            }

            lastAffectedSection =
                previousSection - 1;
        }

        // El jugador se aleja de la casa.
        else
        {
            for (int crossedSection =
                     previousSection;
                 crossedSection < currentSection;
                 crossedSection++)
            {
                AddDeformationUntilSection(
                    crossedSection,
                    -deformationPerSection
                );
            }

            lastAffectedSection =
                currentSection - 1;
        }

        lastAffectedSection = Mathf.Clamp(
            lastAffectedSection,
            0,
            sections.Count - 1
        );

        gridSurfaceMesh.ApplySectionDeformations(
            sections,
            firstAffectedSection,
            lastAffectedSection,
            localDeformationCenter
        );
    }

    /// <summary>
    /// Añade una deformación a todas las secciones
    /// comprendidas entre el centro y el índice indicado.
    /// </summary>
    private void AddDeformationUntilSection(
        int lastSection,
        float amount)
    {
        lastSection = Mathf.Clamp(
            lastSection,
            0,
            sections.Count - 1
        );

        for (int i = 0;
             i <= lastSection;
             i++)
        {
            sections[i].AddDeformation(
                amount
            );
        }
    }

    /// <summary>
    /// Obtiene la sección en la que se encuentra
    /// actualmente el jugador.
    /// </summary>
    private int GetPlayerSection()
    {
        Vector3 localPlayerPosition =
            gridSurfaceMesh.transform
                .InverseTransformPoint(
                    xrOrigin.position
                );

        Vector2 playerXZ =
            new Vector2(
                localPlayerPosition.x,
                localPlayerPosition.z
            );

        float distance =
            Vector2.Distance(
                playerXZ,
                localDeformationCenter
            );

        int sectionIndex =
            Mathf.FloorToInt(
                distance / sectionWidth
            );

        return Mathf.Clamp(
            sectionIndex,
            0,
            sections.Count - 1
        );
    }

    /// <summary>
    /// Crea las secciones concéntricas alrededor
    /// de la posición de la casa.
    /// </summary>
    private void CreateSections()
    {
        sections.Clear();

        Vector3[] vertices =
            gridSurfaceMesh.Vertices;

        if (vertices == null ||
            vertices.Length == 0)
        {
            return;
        }

        float maxRadius = 0f;

        // Buscamos el vértice más alejado
        // respecto al centro de la casa.
        foreach (Vector3 vertex in vertices)
        {
            Vector2 vertexXZ =
                new Vector2(
                    vertex.x,
                    vertex.z
                );

            float distance =
                Vector2.Distance(
                    vertexXZ,
                    localDeformationCenter
                );

            if (distance > maxRadius)
            {
                maxRadius = distance;
            }
        }

        int sectionCount =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    maxRadius / sectionWidth
                )
            );

        // Creamos las secciones.
        for (int i = 0;
             i < sectionCount;
             i++)
        {
            float innerRadius =
                i * sectionWidth;

            float outerRadius =
                (i + 1) * sectionWidth;

            DeformationSection section =
                new DeformationSection(
                    i,
                    innerRadius,
                    outerRadius
                );

            sections.Add(section);
        }

        // Asignamos cada vértice a su sección.
        for (int vertexIndex = 0;
             vertexIndex < vertices.Length;
             vertexIndex++)
        {
            Vector3 vertex =
                vertices[vertexIndex];

            Vector2 vertexXZ =
                new Vector2(
                    vertex.x,
                    vertex.z
                );

            float distance =
                Vector2.Distance(
                    vertexXZ,
                    localDeformationCenter
                );

            int sectionIndex =
                Mathf.FloorToInt(
                    distance / sectionWidth
                );

            sectionIndex = Mathf.Clamp(
                sectionIndex,
                0,
                sections.Count - 1
            );

            sections[sectionIndex]
                .AddVertex(vertexIndex);
        }
    }

    /// <summary>
    /// Calcula el desplazamiento producido por la deformación
    /// en una posición determinada del mundo.
    /// </summary>
    public Vector3 GetWorldDeformationAtPosition(
        Vector3 worldPosition)
    {
        if (sections == null ||
            sections.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 localPosition =
            gridSurfaceMesh.transform
                .InverseTransformPoint(
                    worldPosition
                );

        Vector2 positionXZ =
            new Vector2(
                localPosition.x,
                localPosition.z
            );

        float distance =
            Vector2.Distance(
                positionXZ,
                localDeformationCenter
            );

        int sectionIndex =
            Mathf.FloorToInt(
                distance / sectionWidth
            );

        sectionIndex = Mathf.Clamp(
            sectionIndex,
            0,
            sections.Count - 1
        );

        DeformationSection currentSection =
            sections[sectionIndex];

        int nextSectionIndex =
            Mathf.Min(
                sectionIndex + 1,
                sections.Count - 1
            );

        DeformationSection nextSection =
            sections[nextSectionIndex];

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

        float localDeformation =
            Mathf.Lerp(
                currentSection.CurrentOffset,
                nextSection.CurrentOffset,
                t
            );

        /*
         * Convertimos el desplazamiento vertical local
         * al espacio del mundo.
         */
        return gridSurfaceMesh.transform.TransformVector(
            Vector3.up * localDeformation
        );
    }
}