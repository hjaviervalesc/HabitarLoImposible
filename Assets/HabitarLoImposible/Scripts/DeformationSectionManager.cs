using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DeformationSectionManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private GridSurfaceMesh gridSurfaceMesh;

    [Header("Tamaño de las secciones")]
    [SerializeField] private float sectionWidth = 1f;


    private List<DeformationSection> sections =
        new List<DeformationSection>();


    public List<DeformationSection> Sections => sections;

    [Header("Debug Deformation")]
    [SerializeField] private int testSection = 5;
    [SerializeField] private float testDeformation = -0.01f;

    [Header("Player")]
    [SerializeField] private Transform xrOrigin;

    [Header("Deformación")]
    [SerializeField] private float deformationPerSection = 0.01f;
    private int previousPlayerSection = -1;

    private float currentDeformationRadius = 0f;

    private void Start()
    {
        CreateSections();

        previousPlayerSection = GetPlayerSection();
    }

    private void Update()
    {
        int currentPlayerSection = GetPlayerSection();

        if (currentPlayerSection == previousPlayerSection)
            return;

        UpdateDeformation(
            previousPlayerSection,
            currentPlayerSection
        );

        previousPlayerSection = currentPlayerSection;
    }


    private void UpdateDeformation(
        int previousSection,
        int currentSection)
    {
        int firstAffectedSection;
        int lastAffectedSection;


        // -----------------------------------------
        // El jugador se acerca al centro
        // -----------------------------------------

        if (currentSection < previousSection)
        {
            firstAffectedSection = 0;
            lastAffectedSection = currentSection;

            for (int i = 0; i <= currentSection; i++)
            {
                sections[i].AddDeformation(
                    -deformationPerSection
                );
            }
        }


        // -----------------------------------------
        // El jugador se aleja del centro
        // -----------------------------------------

        else
        {
            firstAffectedSection = 0;
            lastAffectedSection = previousSection;

            for (int i = 0; i <= previousSection; i++)
            {
                sections[i].AddDeformation(
                    deformationPerSection
                );
            }
        }


        // -----------------------------------------
        // Actualizamos la malla UNA SOLA VEZ
        // -----------------------------------------

        gridSurfaceMesh.ApplySectionDeformations(
            sections,
            firstAffectedSection,
            lastAffectedSection
        );
    }
    private int GetPlayerSection()
    {
        Vector2 playerPosition = new Vector2(
            xrOrigin.position.x,
            xrOrigin.position.z
        );

        float distance = playerPosition.magnitude;

        int sectionIndex =
            Mathf.FloorToInt(distance / sectionWidth);

        sectionIndex = Mathf.Clamp(
            sectionIndex,
            0,
            sections.Count - 1
        );

        return sectionIndex;
    }

    /// <summary>
    /// Crea las secciones concéntricas alrededor
    /// del centro de la malla.
    /// </summary>
    private void CreateSections()
    {
        sections.Clear();

        Vector3[] vertices =
            gridSurfaceMesh.Vertices;


        // ------------------------------------------------
        // 1. Averiguamos cuál es el radio máximo de la malla
        // ------------------------------------------------

        float maxRadius = 0f;

        foreach (Vector3 vertex in vertices)
        {
            float distance = Vector2.Distance(
                Vector2.zero,
                new Vector2(vertex.x, vertex.z)
            );

            if (distance > maxRadius)
            {
                maxRadius = distance;
            }
        }


        // ------------------------------------------------
        // 2. Calculamos cuántas secciones necesitamos
        // ------------------------------------------------

        int sectionCount =
            Mathf.CeilToInt(maxRadius / sectionWidth);


        // ------------------------------------------------
        // 3. Creamos las secciones
        // ------------------------------------------------

        for (int i = 0; i < sectionCount; i++)
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


        // ------------------------------------------------
        // 4. Asignamos cada vértice a una sección
        // ------------------------------------------------

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];


            float distance = Vector2.Distance(
                Vector2.zero,
                new Vector2(vertex.x, vertex.z)
            );


            int sectionIndex =
                Mathf.FloorToInt(
                    distance / sectionWidth
                );


            if (sectionIndex >= 0 &&
                sectionIndex < sections.Count)
            {
                sections[sectionIndex]
                    .AddVertex(i);
            }
        }
    }

    //private void OnDrawGizmos()
    //{
    //    if (gridSurfaceMesh == null)
    //        return;

    //    if (gridSurfaceMesh.Vertices == null)
    //        return;

    //    if (sections == null || sections.Count == 0)
    //        return;

    //    Vector3[] vertices = gridSurfaceMesh.Vertices;

    //    for (int i = 0; i < sections.Count; i++)
    //    {
    //        DeformationSection section = sections[i];

    //        float hue = (float)i / sections.Count;

    //        Gizmos.color = Color.HSVToRGB(hue, 1f, 1f);

    //        foreach (int vertexIndex in section.VertexIndices)
    //        {
    //            if (vertexIndex < 0 ||
    //                vertexIndex >= vertices.Length)
    //                continue;

    //            Vector3 vertexPosition =
    //                gridSurfaceMesh.transform.TransformPoint(
    //                    vertices[vertexIndex]
    //                );

    //            Gizmos.DrawSphere(
    //                vertexPosition,
    //                0.05f
    //            );
    //        }
    //    }
    //}
}


































