using UnityEngine;

/// <summary>
/// Genera una carretera por cada arista de HyperbolicTessellation utilizando sus coordenadas en el disco de Poincaré.
/// </summary>
public class HyperbolicTessellationVisualizer : MonoBehaviour
{
    [Header("Espacio hiperbólico")]
    [SerializeField]
    private HyperbolicSpace hyperbolicSpace;

    [Header("Datos matemáticos")]
    public HyperbolicTessellation tessellation;

    [Header("Prefab")]
    public HyperbolicRoad roadPrefab;

    [Header("Generación")]
    public bool buildOnStart = true;

    private Transform generatedRoot;

    /// <summary>
    /// Construye la visualización al iniciar si la opción buildOnStart está activada.
    /// </summary>
    private void Start()
    {
        if (buildOnStart)
        {
            BuildView();
        }
    }


    /// <summary>
    /// Valida las referencias, genera los datos si no hay aristas y sustituye la visualización por una carretera para cada arista.
    /// </summary>
    public void BuildView()
    {
        FindReferences();

        if (hyperbolicSpace == null)
        {
            Debug.LogError("HyperbolicTessellationVisualizer: " + "falta HyperbolicSpace.", this);
            return;
        }

        if (tessellation == null)
        {
            Debug.LogError("HyperbolicTessellationVisualizer: " + "falta HyperbolicTessellation.", this);
            return;
        }

        if (roadPrefab == null)
        {
            Debug.LogError("HyperbolicTessellationVisualizer: " + "falta Road Prefab.", this);
            return;
        }

        if (tessellation.Edges.Count == 0)
        {
            tessellation.GenerateTessellation();
        }

        ClearView();
        CreateGeneratedRoot();

        int edgeIndex = 0;

        foreach (HyperbolicEdge edge in tessellation.Edges)
        {
            edgeIndex++;

            CreateRoad(edge, edgeIndex);
        }
    }

    /// <summary>
    /// Busca las referencias no asignadas al espacio hiperbólico y a la teselación en el objeto o en su jerarquía de padres.
    /// </summary>
    private void FindReferences()
    {
        if (hyperbolicSpace == null)
        {
            hyperbolicSpace = GetComponentInParent<HyperbolicSpace>();
        }

        if (tessellation == null)
        {
            tessellation = GetComponent<HyperbolicTessellation>();

            if (tessellation == null)
            {
                tessellation = GetComponentInParent<HyperbolicTessellation>();
            }
        }
    }

    /// <summary>
    /// Instancia una carretera, asigna su nombre, restablece su transformación local y la inicializa con los extremos de la arista.
    /// </summary>
    private void CreateRoad(HyperbolicEdge edge, int edgeIndex)
    {
        HyperbolicRoad road = Instantiate(roadPrefab, generatedRoot);

        road.name = $"Road_{edgeIndex:000}";

        ResetLocalTransform(road.transform);

        road.Initialize(edge.pointA, edge.pointB, hyperbolicSpace);
    }

    /// <summary>
    /// Crea el objeto padre que agrupa las carreteras generadas y lo vincula al objeto de este componente.
    /// </summary>
    private void CreateGeneratedRoot()
    {
        GameObject rootObject = new GameObject("GeneratedTessellationRoads");

        generatedRoot = rootObject.transform;

        generatedRoot.SetParent(transform, false);
    }

    /// <summary>
    /// Restablece la posición local a cero, la rotación local a la identidad y la escala local a uno.
    /// </summary>
    private void ResetLocalTransform(Transform target)
    {
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;
    }

    /// <summary>
    /// Elimina el objeto que contiene las carreteras mediante destrucción diferida en ejecución o inmediata fuera del modo Play.
    /// </summary>
    [ContextMenu("Eliminar visualización")]
    public void ClearView()
    {
        Transform existingRoot = transform.Find("GeneratedTessellationRoads");

        if (existingRoot == null)
        {
            generatedRoot = null;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existingRoot.gameObject);
        }
        else
        {
            DestroyImmediate(existingRoot.gameObject);
        }

        generatedRoot = null;
    }
}