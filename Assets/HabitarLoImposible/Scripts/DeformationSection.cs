using UnityEngine;
using System.Collections.Generic;

public class DeformationSection : MonoBehaviour
{
    [Header("Id")]
    [SerializeField] private int sectionIndex;

   private float depth;

    // Cubos que pertenecen a esta sección
    private List<GameObject> cubes = new List<GameObject>();

    private Dictionary<GameObject, float> originalHeights =
    new Dictionary<GameObject, float>();
    private float currentOffset = 0f;


    public int SectionIndex => sectionIndex;
    public float Depth => depth;
    public List<GameObject> Cubes => cubes;

    /// <summary>
    /// Inicializa la sección.
    /// </summary>
    public void Initialize(int index, float sectionDepth)
    {
        sectionIndex = index;
        depth = sectionDepth;
    }


    /// <summary>
    /// Añade un cubo a esta sección.
    /// </summary>
    public void AddCube(GameObject cube)
    {
        cubes.Add(cube);
        originalHeights[cube] = cube.transform.position.y;
    }

    /// <summary>
    /// Cambia temporalmente el material de todos los cubos
    /// pertenecientes a esta sección.
    /// </summary>
    public void SetDebugMaterial(Material material)
    {
        foreach (GameObject cube in cubes)
        {
            Renderer renderer = cube.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material = material;
            }
        }
    }

    public void SetHeight(float offset)
    {
        foreach (GameObject cube in cubes)
        {
            Vector3 position = cube.transform.position;

            position.y = originalHeights[cube] + offset;

            cube.transform.position = position;
        }
    }

    public void UpdateHeight(float targetOffset, float speed)
    {
        currentOffset = Mathf.Lerp(
            currentOffset,
            targetOffset,
            speed * Time.deltaTime
        );

        foreach (GameObject cube in cubes)
        {
            Vector3 position = cube.transform.position;

            position.y =
                originalHeights[cube] + currentOffset;

            cube.transform.position = position;
        }
    }
    public void AddDeformation(float amount)
    {
        currentOffset += amount;

        foreach (GameObject cube in cubes)
        {
            Vector3 position = cube.transform.position;

            position.y =
                originalHeights[cube] + currentOffset;

            cube.transform.position = position;
        }
    }






}