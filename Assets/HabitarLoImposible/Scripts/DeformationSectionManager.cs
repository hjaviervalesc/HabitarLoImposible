using UnityEngine;
using System.Collections.Generic;

public class DeformationSectionManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private GridGenerator gridGenerator;

    [Header("Centro del hoyo")]
    [SerializeField] private Vector2Int holeCenter = new Vector2Int();

    [Header("Tamaño de las secciones")]
    [SerializeField] private float sectionWidth = 5f;

    [Header("Profundidad por sección")]
    [SerializeField] private float depthPerSection = 0.1f;

    [Header("Debug")]
    [SerializeField] private Material[] debugMaterials;

    [SerializeField] private Transform xrOrigin;

    [Header("Velocidad de deformación")]
    [SerializeField] private float deformationSpeed = 2f;

    private int previousPlayerSection = -1;

    private List<DeformationSection> sections = new List<DeformationSection>();


    public List<DeformationSection> Sections => sections;


    private void Start()
    {
        holeCenter = GetHoleCenter();
        CreateSections();
    }

    private void Update()
    {
        UpdateDeformation();
    }


    private Vector2Int GetHoleCenter()
    {
        int centerX = gridGenerator.Width / 2;
        int centerZ = gridGenerator.Height / 2;

        return new Vector2Int(centerX, centerZ);
    }

    /// <summary>
    /// Crea dinámicamente las secciones y asigna cada cubo
    /// a la sección correspondiente según su distancia al centro.
    /// </summary>
    private void CreateSections()
    {

        for (int x = 0; x < gridGenerator.Width; x++)
        {
            for (int z = 0; z < gridGenerator.Height; z++)
            {
                //distancia del centro
                float distance = Vector2.Distance(
                    new Vector2(x, z),
                    holeCenter
                );


                int sectionIndex = Mathf.FloorToInt(
                    distance / sectionWidth
                );

                // Si todavía no existe esta sección, la creamos.
                while (sections.Count <= sectionIndex)
                {
                    CreateSection(sections.Count);
                }

                // Añadimos el cubo a su sección correspondiente.
                GameObject cube = gridGenerator.Grid[x, z];

                sections[sectionIndex].AddCube(cube);
            }
        }

        //Visualización temporal de las secciones
        for (int i = 0; i < sections.Count; i++)
        {
            if (i < debugMaterials.Length)
            {
                sections[i].SetDebugMaterial(debugMaterials[i]);
            }
        }
    }


    /// <summary>
    /// Crea una nueva sección.
    /// </summary>
    private void CreateSection(int index)
    {
        GameObject sectionObject = new GameObject(
            "DeformationSection_" + index
        );

        sectionObject.transform.SetParent(transform);

        DeformationSection section =
            sectionObject.AddComponent<DeformationSection>();

        float depth = index * depthPerSection;

        section.Initialize(index, depth);

        sections.Add(section);
    }


    private void UpdateDeformation()
    {
        Vector2Int playerCell =
            gridGenerator.WorldToGrid(xrOrigin.position);

        float distance = Vector2.Distance(
            new Vector2(playerCell.x, playerCell.y),
            holeCenter
        );

        int currentPlayerSection =
            Mathf.FloorToInt(distance / sectionWidth);


        // Primera detección
        if (previousPlayerSection == -1)
        {
            previousPlayerSection = currentPlayerSection;
            return;
        }


        // El jugador se ha movido de sección
        if (currentPlayerSection != previousPlayerSection)
        {
            // Avanza hacia el centro
            if (currentPlayerSection < previousPlayerSection)
            {
                for (int i = 0; i <= currentPlayerSection; i++)
                {
                    sections[i].AddDeformation(-depthPerSection);
                }
            }

            // Se aleja del centro
            else
            {
                for (int i = 0; i <= currentPlayerSection; i++)
                {
                    sections[i].AddDeformation(depthPerSection);
                }
            }

            previousPlayerSection = currentPlayerSection;
        }
    }











}



































