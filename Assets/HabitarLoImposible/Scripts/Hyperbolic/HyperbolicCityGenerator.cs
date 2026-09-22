using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera los elementos de la ciudad hiperbólica. Los edificios se sitúan junto a las carreteras. Los objetos libres se distribuyen por el disco. Se evitan solapamientos entre objetos y carreteras.
/// </summary>
public class HyperbolicCityGenerator : MonoBehaviour
{
    /// <summary>
    /// Define los prefabs, la cantidad y el modo de colocación de un tipo de objeto.
    /// </summary>
    [Serializable]
    public class SpawnRule
    {
        [Header("Tipo")]
        public string objectTypeName = "Casas";

        [Header("Prefabs")]
        public GameObject[] prefabs;

        [Header("Cantidad")]
        [Min(0)]
        public int amount = 30;

        [Header("Colocación")]
        [Tooltip("Si está activado, el objeto se coloca " + "junto a una carretera y mira hacia ella.")]
        public bool faceNearestRoad = true;
    }

    /// <summary>
    /// Huella ocupada por un objeto sobre el plano X-Z.
    /// </summary>
    private struct OccupiedFootprint
    {
        public Rect rectangle;

        /// <summary>
        /// Inicializa la huella ocupada con el rectángulo indicado.
        /// </summary>
        public OccupiedFootprint(Rect rectangle)
        {
            this.rectangle = rectangle;
        }
    }

    [Header("Espacio hiperbólico")]
    [SerializeField]
    private HyperbolicSpace hyperbolicSpace;

    [Header("Zona de generación")]
    [Range(0.1f, 0.99f)]
    public float generationRadius = 0.92f;

    [Header("Distribución libre")]
    [Tooltip("Se aplica a los objetos que no se colocan " + "directamente junto a carreteras.")]
    public bool useHyperbolicDistribution = true;

    [Header("Colocación junto a carreteras")]
    [Tooltip("Espacio entre el borde del edificio " + "y el borde de la carretera.")]
    [Min(0f)]
    public float buildingRoadGap = 0.10f;

    [Header("Tipos de objetos")]
    public SpawnRule[] spawnRules;

    [Header("Rendimiento")]
    [Min(1)]
    public int maximumAttemptsPerObject = 30;

    [Min(1)]
    public int maximumTotalObjects = 250;

    [Header("Generación")]
    public bool generateOnStart = true;

    public bool useFixedSeed = true;

    public int seed = 12345;

    private readonly List<HyperbolicRoad> roads = new List<HyperbolicRoad>();

    private readonly List<OccupiedFootprint> occupiedFootprints = new List<OccupiedFootprint>();

    private readonly Collider[] roadOverlapBuffer = new Collider[64];

    private Transform generatedRoot;

    private int generatedObjectCount;

    /// <summary>
    /// Espera un fotograma antes de generar la ciudad si la generación automática está activada.
    /// </summary>
    private IEnumerator Start()
    {
        if (!generateOnStart)
        {
            yield break;
        }

        yield return null;

        GenerateCity();
    }

    /// <summary>
    /// Genera toda la ciudad.
    /// </summary>
    [ContextMenu("Generar ciudad")]
    public void GenerateCity()
    {
        FindReferences();

        if (hyperbolicSpace == null)
        {
            Debug.LogError("HyperbolicCityGenerator: " + "falta HyperbolicSpace.");

            return;
        }

        if (spawnRules == null || spawnRules.Length == 0)
        {
            Debug.LogWarning("HyperbolicCityGenerator: " + "no hay reglas configuradas.");

            return;
        }

        FindGeneratedRoads();

        if (roads.Count == 0)
        {
            Debug.LogError("HyperbolicCityGenerator: " + "no se encontraron carreteras.");

            return;
        }

        EnableRoadColliders();

        ClearCity();

        if (useFixedSeed)
        {
            UnityEngine.Random.InitState(seed);
        }

        CreateGeneratedRoot();

        occupiedFootprints.Clear();

        generatedObjectCount = 0;

        foreach (SpawnRule rule in spawnRules)
        {
            if (generatedObjectCount >= maximumTotalObjects)
            {
                break;
            }

            GenerateObjectsForRule(rule);
        }

        DisableRoadColliders();

    }

    /// <summary>
    /// Busca HyperbolicSpace en el objeto o en sus padres si la referencia no está asignada.
    /// </summary>
    private void FindReferences()
    {
        if (hyperbolicSpace == null)
        {
            hyperbolicSpace = GetComponentInParent<HyperbolicSpace>();
        }
    }

    /// <summary>
    /// Busca todas las carreteras activas.
    /// </summary>
    private void FindGeneratedRoads()
    {
        roads.Clear();

        HyperbolicRoad[] foundRoads = FindObjectsByType<HyperbolicRoad>(FindObjectsSortMode.None);

        foreach (HyperbolicRoad road in foundRoads)
        {
            if (road != null && road.gameObject.activeInHierarchy && road.Geodesic != null)
            {
                roads.Add(road);
            }
        }
    }

    /// <summary>
    /// Activa los MeshCollider de las carreteras para comprobar solapamientos durante la generación.
    /// </summary>
    private void EnableRoadColliders()
    {
        foreach (HyperbolicRoad road in roads)
        {
            if (road == null)
            {
                continue;
            }

            MeshCollider roadCollider = road.GetComponent<MeshCollider>();

            if (roadCollider != null)
            {
                roadCollider.enabled = true;
            }
        }
    }

    /// <summary>
    /// Desactiva los MeshCollider de las carreteras al finalizar la generación.
    /// </summary>
    private void DisableRoadColliders()
    {
        foreach (HyperbolicRoad road in roads)
        {
            if (road == null)
            {
                continue;
            }

            MeshCollider roadCollider = road.GetComponent<MeshCollider>();

            if (roadCollider != null)
            {
                roadCollider.enabled = false;
            }
        }
    }

    /// <summary>
    /// Intenta generar la cantidad de objetos indicada por la regla respetando el límite global y el modo de colocación.
    /// </summary>
    private void GenerateObjectsForRule(SpawnRule rule)
    {
        if (rule == null || rule.prefabs == null || rule.prefabs.Length == 0 || rule.amount <= 0)
        {
            return;
        }

        for (int objectIndex = 0; objectIndex < rule.amount; objectIndex++)
        {
            if (generatedObjectCount >= maximumTotalObjects)
            {
                break;
            }

            if (rule.faceNearestRoad)
            {
                TryCreateObjectNextToRoad(rule);
            }
            else
            {
                TryCreateFreeObject(rule);
            }
        }
    }

    /// <summary>
    /// Intenta crear un objeto junto a una carretera.
    /// </summary>
    private bool TryCreateObjectNextToRoad(SpawnRule rule)
    {
        for (int attempt = 0; attempt < maximumAttemptsPerObject; attempt++)
        {
            GameObject prefab = GetRandomValidPrefab(rule.prefabs);

            if (prefab == null)
            {
                return false;
            }

            HyperbolicRoad road = roads[UnityEngine.Random.Range(0, roads.Count)];

            float t = UnityEngine.Random.value;

            Vector2 roadPointPoincare = road.Geodesic.GetPointAt(t);

            Vector2 tangent = road.Geodesic.GetTangentAt(t);

            Vector2 normal = new Vector2(-tangent.y, tangent.x).normalized;

            if (UnityEngine.Random.value < 0.5f)
            {
                normal = -normal;
            }

            Vector2 roadPointUnity = hyperbolicSpace.PoincareToUnity2D(roadPointPoincare);

            float scaleFactor = hyperbolicSpace.GetScaleFactor(roadPointPoincare);

            GameObject candidate = Instantiate(prefab, generatedRoot);

            candidate.name = $"{rule.objectTypeName}_Candidate";

            float prefabHeight = candidate.transform.localPosition.y;

            candidate.transform.localScale = prefab.transform.localScale * scaleFactor;

            candidate.transform.localPosition = hyperbolicSpace.PoincareToUnity(roadPointPoincare, prefabHeight);

            OrientNegativeXAxisTowardsDirection(candidate.transform, -normal);

            if (!TryGetObjectBounds(candidate, out Bounds candidateBounds))
            {
                DestroyCandidate(candidate);

                Debug.LogWarning($"El prefab {prefab.name} " + "no tiene ningún Renderer.");

                return false;
            }

            float objectExtentTowardsRoad = GetExtentTowardsDirection(candidateBounds, candidate.transform.position, -normal);

            float scaledRoadGap = buildingRoadGap * scaleFactor;

            float roadHalfWidth = road.GetHalfWidthAt(roadPointPoincare);

            float distanceFromRoadCenter = roadHalfWidth + objectExtentTowardsRoad + scaledRoadGap;

            Vector2 finalUnityPosition = roadPointUnity + normal * distanceFromRoadCenter;

            Vector2 finalPoincarePosition = hyperbolicSpace.UnityToPoincare(finalUnityPosition);

            if (!hyperbolicSpace.IsInsideDisk(finalPoincarePosition, generationRadius))
            {
                DestroyCandidate(candidate);
                continue;
            }

            candidate.transform.localPosition = new Vector3(finalUnityPosition.x, prefabHeight, finalUnityPosition.y);

            if (!TryGetObjectBounds(candidate, out candidateBounds))
            {
                DestroyCandidate(candidate);
                continue;
            }

            if (TouchesAnyRoadCollider(candidateBounds))
            {
                DestroyCandidate(candidate);
                continue;
            }

            Rect candidateFootprint = CreateFootprintRectangle(candidateBounds);

            if (OverlapsAnotherObject(candidateFootprint))
            {
                DestroyCandidate(candidate);
                continue;
            }

            AcceptCandidate(candidate, rule, candidateFootprint);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Intenta colocar libremente un objeto.
    /// </summary>
    private bool TryCreateFreeObject(SpawnRule rule)
    {
        for (int attempt = 0; attempt < maximumAttemptsPerObject; attempt++)
        {
            GameObject prefab = GetRandomValidPrefab(rule.prefabs);

            if (prefab == null)
            {
                return false;
            }

            Vector2 poincarePosition = GetRandomPointInsideDisk();

            float scaleFactor = hyperbolicSpace.GetScaleFactor(poincarePosition);

            GameObject candidate = Instantiate(prefab, generatedRoot);

            candidate.name = $"{rule.objectTypeName}_Candidate";

            float prefabHeight = candidate.transform.localPosition.y;

            candidate.transform.localPosition = hyperbolicSpace.PoincareToUnity(poincarePosition, prefabHeight);

            candidate.transform.localScale = prefab.transform.localScale * scaleFactor;

            candidate.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            if (!TryGetObjectBounds(candidate, out Bounds candidateBounds))
            {
                DestroyCandidate(candidate);

                Debug.LogWarning($"El prefab {prefab.name} " + "no tiene ningún Renderer.");

                return false;
            }

            if (TouchesAnyRoadCollider(candidateBounds))
            {
                DestroyCandidate(candidate);
                continue;
            }

            Rect candidateFootprint = CreateFootprintRectangle(candidateBounds);

            if (OverlapsAnotherObject(candidateFootprint))
            {
                DestroyCandidate(candidate);
                continue;
            }

            AcceptCandidate(candidate, rule, candidateFootprint);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Comprueba mediante físicas si el volumen del objeto toca una carretera.
    /// </summary>
    private bool TouchesAnyRoadCollider(Bounds objectBounds)
    {
        Vector3 halfExtents = objectBounds.extents * 0.99f;

        halfExtents.y = Mathf.Max(halfExtents.y, 2f);

        int colliderCount = Physics.OverlapBoxNonAlloc(objectBounds.center, halfExtents, roadOverlapBuffer, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < colliderCount; i++)
        {
            Collider detectedCollider = roadOverlapBuffer[i];

            if (detectedCollider == null)
            {
                continue;
            }

            HyperbolicRoad detectedRoad = detectedCollider.GetComponentInParent<HyperbolicRoad>();

            if (detectedRoad != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Asigna el nombre definitivo al objeto, registra su huella e incrementa el contador de objetos generados.
    /// </summary>
    private void AcceptCandidate(GameObject candidate, SpawnRule rule, Rect footprint)
    {
        candidate.name = $"{rule.objectTypeName}_" + $"{generatedObjectCount:000}";

        occupiedFootprints.Add(new OccupiedFootprint(footprint));

        generatedObjectCount++;
    }

    /// <summary>
    /// Obtiene una posición aleatoria para los objetos de distribución libre.
    /// </summary>
    private Vector2 GetRandomPointInsideDisk()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        float radius;

        if (useHyperbolicDistribution)
        {
            float maximumRadiusSquared = generationRadius * generationRadius;

            float maximumHyperbolicArea = maximumRadiusSquared / (1f - maximumRadiusSquared);

            float randomHyperbolicArea = UnityEngine.Random.value * maximumHyperbolicArea;

            radius = Mathf.Sqrt(randomHyperbolicArea / (1f + randomHyperbolicArea));
        }
        else
        {
            radius = Mathf.Sqrt(UnityEngine.Random.value) * generationRadius;
        }

        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    /// <summary>
    /// Calcula cuánto sobresale el objeto desde su pivote en una dirección determinada.
    /// </summary>
    private float GetExtentTowardsDirection(Bounds bounds, Vector3 pivotPosition, Vector2 direction)
    {
        Vector2 pivot = new Vector2(pivotPosition.x, pivotPosition.z);

        Vector2 cornerA = new Vector2(bounds.min.x, bounds.min.z);

        Vector2 cornerB = new Vector2(bounds.min.x, bounds.max.z);

        Vector2 cornerC = new Vector2(bounds.max.x, bounds.min.z);

        Vector2 cornerD = new Vector2(bounds.max.x, bounds.max.z);

        return Mathf.Max(0f, Vector2.Dot(cornerA - pivot, direction), Vector2.Dot(cornerB - pivot, direction), Vector2.Dot(cornerC - pivot, direction), Vector2.Dot(cornerD - pivot, direction));
    }

    /// <summary>
    /// Obtiene los Bounds conjuntos de todos los Renderer de un objeto.
    /// </summary>
    private bool TryGetObjectBounds(GameObject target, out Bounds combinedBounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        combinedBounds = new Bounds();

        bool initialized = false;

        foreach (Renderer objectRenderer in renderers)
        {
            if (objectRenderer == null || !objectRenderer.enabled)
            {
                continue;
            }

            if (!initialized)
            {
                combinedBounds = objectRenderer.bounds;

                initialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(objectRenderer.bounds);
            }
        }

        return initialized;
    }

    /// <summary>
    /// Obtiene el rectángulo ocupado en el plano X-Z a partir de los límites del objeto.
    /// </summary>
    private Rect CreateFootprintRectangle(Bounds bounds)
    {
        return Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
    }

    /// <summary>
    /// Comprueba si la huella del candidato se solapa con alguna de las huellas ya registradas.
    /// </summary>
    private bool OverlapsAnotherObject(Rect candidateFootprint)
    {
        foreach (OccupiedFootprint occupied in occupiedFootprints)
        {
            if (candidateFootprint.Overlaps(occupied.rectangle, true))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Busca un prefab no nulo recorriendo el array desde un índice aleatorio y devuelve null si no encuentra ninguno.
    /// </summary>
    private GameObject GetRandomValidPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return null;
        }

        int initialIndex = UnityEngine.Random.Range(0, prefabs.Length);

        for (int offset = 0; offset < prefabs.Length; offset++)
        {
            int index = (initialIndex + offset) % prefabs.Length;

            if (prefabs[index] != null)
            {
                return prefabs[index];
            }
        }

        return null;
    }

    /// <summary>
    /// Orienta el eje local -X en una dirección determinada.
    /// </summary>
    private void OrientNegativeXAxisTowardsDirection(Transform target, Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.000001f)
        {
            return;
        }

        direction.Normalize();

        float rotationY = Mathf.Atan2(direction.y, -direction.x) * Mathf.Rad2Deg;

        target.localRotation = Quaternion.Euler(0f, rotationY, 0f);
    }

    /// <summary>
    /// Elimina el candidato usando destrucción diferida en ejecución e inmediata fuera del modo Play.
    /// </summary>
    private void DestroyCandidate(GameObject candidate)
    {
        if (candidate == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(candidate);
        }
        else
        {
            DestroyImmediate(candidate);
        }
    }

    /// <summary>
    /// Crea el objeto padre que agrupa los elementos generados de la ciudad.
    /// </summary>
    private void CreateGeneratedRoot()
    {
        GameObject rootObject = new GameObject("GeneratedHyperbolicCity");

        generatedRoot = rootObject.transform;

        generatedRoot.SetParent(transform, false);
    }

    /// <summary>
    /// Elimina la ciudad generada y reinicia las referencias, las huellas ocupadas y el contador de objetos.
    /// </summary>
    [ContextMenu("Eliminar ciudad")]
    public void ClearCity()
    {
        Transform existingRoot = transform.Find("GeneratedHyperbolicCity");

        if (existingRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existingRoot.gameObject);
            }
            else
            {
                DestroyImmediate(existingRoot.gameObject);
            }
        }

        generatedRoot = null;

        occupiedFootprints.Clear();

        generatedObjectCount = 0;
    }
}
