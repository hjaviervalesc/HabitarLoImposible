using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Cube")]
    [SerializeField] private GameObject cubePrefab;

    [Header("Grid Size")]
    [SerializeField] private int width = 100;
    [SerializeField] private int height = 100;
    [SerializeField] private float cubeSize = 1.0f;

    [Header("Materials")]
    [SerializeField] private Material blackMaterial;
    [SerializeField] private Material whiteMaterial;


    //Declaramos una matriz bidimensional sin establecer tamaños todavía.
    private GameObject[,] grid;

    //Getters
    public GameObject[,] Grid => grid;
    public int Width => width;
    public int Height => height;
    public float CubeSize => cubeSize;

    private void Awake()
    {
        GenerateGrid();
    }

    /// <summary>
    /// Genera una cuadrícula de cubos con patrón de ajedrez y almacena
    /// una referencia a cada cubo en una matriz bidimensional.
    /// </summary>
    private void GenerateGrid()
    {
        //Establecemos dimensiones del grid 
        grid = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                //Establecemos posicíon del próximo cubo al final del cubo anterior en el eje x, z.
                Vector3 position = new Vector3(x * cubeSize,0,z * cubeSize);

                //Instanciamos un objeto cubePrefab idéntico en la nueva posición.
                GameObject cube = Instantiate(cubePrefab,position,Quaternion.identity,transform);

                //Lo escalamos 
                cube.transform.localScale = Vector3.one * cubeSize;

                //Obtenemos el Renderer para asignarle el material correspondiente.
                Renderer renderer = cube.GetComponent<Renderer>();

                //Si es par color blanco si es impar color negro, alternando 
                renderer.material = ((x + z) % 2 == 0) ? whiteMaterial : blackMaterial;

                //Almacenamos esa referencia a esa posición de la matriz
                grid[x, z] = cube;
            }
        }
    }

    /// <summary>
    /// Comprueba si las coordenadas indicadas están dentro de la cuadrícula.
    /// </summary>
    public bool IsInsideGrid(int x, int z)
    {
        return x >= 0 && x < width && z >= 0 && z < height;
    }


    /// <summary>
    ///  Convierte una posición del mundo en la casilla correspondiente de la cuadrícula.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / cubeSize);
        int z = Mathf.RoundToInt(worldPosition.z / cubeSize);

        return new Vector2Int(x, z);
    }
}