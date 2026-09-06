using UnityEngine;

/// <summary>
/// Genera una carretera por cada arista
/// calculada por HyperbolicTessellation.
///
/// Las aristas ya están expresadas directamente
/// en coordenadas del disco de Poincaré.
/// </summary>
public class HyperbolicTessellationVisualizer :
    MonoBehaviour
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


    private void Start()
    {
        if (buildOnStart)
        {
            BuildView();
        }
    }


    [ContextMenu("Reconstruir visualización")]
    public void RebuildView()
    {
        FindReferences();

        if (tessellation != null)
        {
            tessellation.GenerateTessellation();
        }

        BuildView();
    }


    public void BuildView()
    {
        FindReferences();

        if (hyperbolicSpace == null)
        {
            Debug.LogError(
                "HyperbolicTessellationVisualizer: " +
                "falta HyperbolicSpace.",
                this
            );

            return;
        }

        if (tessellation == null)
        {
            Debug.LogError(
                "HyperbolicTessellationVisualizer: " +
                "falta HyperbolicTessellation.",
                this
            );

            return;
        }

        if (roadPrefab == null)
        {
            Debug.LogError(
                "HyperbolicTessellationVisualizer: " +
                "falta Road Prefab.",
                this
            );

            return;
        }

        if (tessellation.Edges.Count == 0)
        {
            tessellation.GenerateTessellation();
        }

        ClearView();
        CreateGeneratedRoot();

        int edgeIndex = 0;

        foreach (HyperbolicEdge edge
                 in tessellation.Edges)
        {
            edgeIndex++;

            CreateRoad(
                edge,
                edgeIndex
            );
        }
    }


    private void FindReferences()
    {
        if (hyperbolicSpace == null)
        {
            hyperbolicSpace =
                GetComponentInParent<HyperbolicSpace>();
        }

        if (tessellation == null)
        {
            tessellation =
                GetComponent<HyperbolicTessellation>();

            if (tessellation == null)
            {
                tessellation =
                    GetComponentInParent<
                        HyperbolicTessellation
                    >();
            }
        }
    }


    private void CreateRoad(
        HyperbolicEdge edge,
        int edgeIndex
    )
    {
        HyperbolicRoad road =
            Instantiate(
                roadPrefab,
                generatedRoot
            );

        road.name =
            $"Road_{edgeIndex:000}";

        ResetLocalTransform(
            road.transform
        );

        // Los puntos de la arista ya pertenecen
        // al disco de Poincaré.
        road.Initialize(
            edge.pointA,
            edge.pointB,
            hyperbolicSpace
        );
    }


    private void CreateGeneratedRoot()
    {
        GameObject rootObject =
            new GameObject(
                "GeneratedTessellationRoads"
            );

        generatedRoot =
            rootObject.transform;

        generatedRoot.SetParent(
            transform,
            false
        );
    }


    private void ResetLocalTransform(
        Transform target
    )
    {
        target.localPosition =
            Vector3.zero;

        target.localRotation =
            Quaternion.identity;

        target.localScale =
            Vector3.one;
    }


    [ContextMenu("Eliminar visualización")]
    public void ClearView()
    {
        Transform existingRoot =
            transform.Find(
                "GeneratedTessellationRoads"
            );

        if (existingRoot == null)
        {
            generatedRoot = null;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(
                existingRoot.gameObject
            );
        }
        else
        {
            DestroyImmediate(
                existingRoot.gameObject
            );
        }

        generatedRoot = null;
    }
}