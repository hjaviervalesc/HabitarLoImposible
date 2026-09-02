using UnityEngine;

public class HyperbolicTessellationVisualizer: MonoBehaviour
{
    [Header("Datos matemáticos")]
    public HyperbolicTessellation tessellation;

    [Header("Escala del disco")]
    public float diskScale = 50f;

    [Header("Prefabs")]
    public HyperbolicGeodesic geodesicPrefab;
    public HyperbolicRoad roadPrefab;

    [Header("Visualización")]
    public bool buildOnStart = true;
    public bool generateRoads = true;
    public bool showGeodesicLines = false;

    private Transform generatedRoot;


    private void Start()
    {
        if (buildOnStart)
        {
            BuildView();
        }
    }


    // ======================================================
    // CONSTRUIR VISUALIZACIÓN
    // ======================================================

    [ContextMenu("Reconstruir visualización")]
    public void RebuildView()
    {
        if (tessellation != null)
        {
            tessellation.GenerateTessellation();
        }

        BuildView();
    }


    public void BuildView()
    {
        if (tessellation == null)
        {
            tessellation =
                GetComponent<HyperbolicTessellation>();
        }


        if (tessellation == null)
        {
            Debug.LogError(
                "HyperbolicTessellationView: " +
                "falta asignar la teselación."
            );

            return;
        }


        if (geodesicPrefab == null)
        {
            Debug.LogError(
                "HyperbolicTessellationView: " +
                "falta asignar Geodesic Prefab."
            );

            return;
        }


        if (generateRoads &&
            roadPrefab == null)
        {
            Debug.LogError(
                "HyperbolicTessellationView: " +
                "falta asignar Road Prefab."
            );

            return;
        }


        // Si todavía no hay datos,
        // los generamos antes de construir.

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

            CreateEdgeView(
                edge,
                edgeIndex
            );
        }


        Debug.Log(
            $"Visualización construida.\n" +
            $"Geodésicas: {edgeIndex}\n" +
            $"Carreteras: " +
            $"{(generateRoads ? edgeIndex : 0)}"
        );
    }


    // ======================================================
    // CREAR REPRESENTACIÓN DE UN LADO
    // ======================================================

    private void CreateEdgeView(
        HyperbolicEdge edge,
        int edgeIndex
    )
    {
        HyperbolicGeodesic geodesic =
            Instantiate(
                geodesicPrefab,
                generatedRoot
            );


        geodesic.name =
            $"Geodesic_{edgeIndex:000}";


        ResetLocalTransform(
            geodesic.transform
        );


        geodesic.pointA =
            HyperbolicPoint.PoincareToHyperboloid(
                edge.pointA
            );

        geodesic.pointB =
            HyperbolicPoint.PoincareToHyperboloid(
                edge.pointB
            );

        geodesic.diskScale =
            diskScale;


        geodesic.DrawGeodesic();


        LineRenderer line =
            geodesic.GetComponent<LineRenderer>();


        if (line != null)
        {
            line.enabled =
                showGeodesicLines;
        }


        if (!generateRoads)
        {
            return;
        }


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


        road.Initialize(
            geodesic
        );
    }


    // ======================================================
    // JERARQUÍA
    // ======================================================

    private void CreateGeneratedRoot()
    {
        GameObject rootObject =
            new GameObject(
                "GeneratedTessellationView"
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


    // ======================================================
    // LIMPIAR VISUALIZACIÓN
    // ======================================================

    [ContextMenu("Eliminar visualización")]
    public void ClearView()
    {
        Transform existingRoot =
            transform.Find(
                "GeneratedTessellationView"
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