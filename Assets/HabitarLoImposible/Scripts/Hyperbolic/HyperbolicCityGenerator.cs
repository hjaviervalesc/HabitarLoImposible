using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HyperboliceCityGenerator : MonoBehaviour
{
    // ======================================================
    // REGLA DE GENERACIÓN
    // ======================================================

    [Serializable]
    public class SpawnRule
    {
        [Header("Tipo")]
        public string objectTypeName = "Casas";

        public GameObject[] prefabs;

        [Header("Cantidad total en el disco")]
        [Min(0)]
        public int minimumAmount = 30;

        [Min(0)]
        public int maximumAmount = 40;

        [Header("Escala")]
        [Tooltip(
            "Escala del prefab en el centro del disco."
        )]
        [Min(0.01f)]
        public float baseScale = 1f;

        [Header("Separación entre objetos")]
        [Tooltip(
            "Separación adicional respecto a otros objetos."
        )]
        [Min(0f)]
        public float extraSeparation = 0.5f;

        [Header("Distancia a la carretera")]
        [Tooltip(
            "Espacio adicional entre el límite del objeto " +
            "y el borde de la carretera."
        )]
        [Min(0f)]
        public float minimumRoadDistance = 1f;

        [Tooltip(
            "Distancia máxima desde el pivote hasta " +
            "la carretera. Pon 0 para desactivarla."
        )]
        [Min(0f)]
        public float maximumRoadDistance = 10f;

        [Header("Altura")]
        public float height = 0.5f;

        [Header("Orientación")]
        [Tooltip(
            "El eje local -X del prefab mira " +
            "hacia la carretera."
        )]
        public bool faceNearestRoad = true;
    }

    // ======================================================
    // HUELLA OCUPADA EN EL PLANO X-Z
    // ======================================================

    private struct OccupiedFootprint
    {
        public Rect rectangle;

        public OccupiedFootprint(
            Rect rectangle
        )
        {
            this.rectangle = rectangle;
        }
    }

    // ======================================================
    // CONFIGURACIÓN
    // ======================================================

    [Header("Escala del disco")]
    [Min(0.01f)]
    public float diskScale = 50f;

    [Header("Zona de generación")]
    [Range(0.1f, 0.99f)]
    public float generationRadius = 0.92f;

    [Header("Carreteras")]
    [Tooltip(
        "Mitad de la anchura visual de la carretera."
    )]
    [Min(0f)]
    public float roadHalfWidth = 1.5f;

    [Tooltip(
        "Puntos examinados en cada geodésica."
    )]
    [Range(4, 50)]
    public int samplesPerGeodesic = 20;

    [Header("Tipos de objetos")]
    [Tooltip(
        "Genera primero bancos y supermercados, " +
        "después casas y finalmente árboles."
    )]
    public SpawnRule[] spawnRules;

    [Header("Rendimiento para Quest")]
    [Min(1)]
    public int maximumAttemptsPerObject = 25;

    [Min(1)]
    public int maximumTotalObjects = 250;

    [Header("Generación")]
    public bool generateOnStart = true;
    public bool useFixedSeed = true;
    public int seed = 12345;

    // ======================================================
    // DATOS INTERNOS
    // ======================================================

    private readonly List<HyperbolicGeodesic> geodesics =
        new List<HyperbolicGeodesic>();

    private readonly List<OccupiedFootprint> occupiedFootprints =
        new List<OccupiedFootprint>();

    private Transform generatedRoot;
    private int generatedObjectCount;

    // ======================================================
    // INICIO
    // ======================================================

    private IEnumerator Start()
    {
        if (!generateOnStart)
        {
            yield break;
        }

        // Esperamos a que el visualizador
        // construya las geodésicas.

        yield return null;

        GenerateCity();
    }

    // ======================================================
    // GENERAR CIUDAD
    // ======================================================

    [ContextMenu("Generar ciudad")]
    public void GenerateCity()
    {
        if (spawnRules == null ||
            spawnRules.Length == 0)
        {
            Debug.LogWarning(
                "HyperboliceCityGenerator: " +
                "no hay reglas configuradas."
            );

            return;
        }

        FindGeneratedGeodesics();

        if (geodesics.Count == 0)
        {
            Debug.LogError(
                "HyperboliceCityGenerator: " +
                "no se han encontrado geodésicas."
            );

            return;
        }

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
            if (generatedObjectCount >=
                maximumTotalObjects)
            {
                break;
            }

            GenerateObjectsForRule(rule);
        }

        Debug.Log(
            $"Ciudad generada.\n" +
            $"Geodésicas: {geodesics.Count}\n" +
            $"Objetos: {generatedObjectCount}"
        );
    }

    // ======================================================
    // BUSCAR GEODÉSICAS
    // ======================================================

    private void FindGeneratedGeodesics()
    {
        geodesics.Clear();

        HyperbolicGeodesic[] foundGeodesics =
            FindObjectsByType<HyperbolicGeodesic>(
                FindObjectsSortMode.None
            );

        foreach (HyperbolicGeodesic geodesic
                 in foundGeodesics)
        {
            if (geodesic != null &&
                geodesic.gameObject.activeInHierarchy)
            {
                geodesics.Add(geodesic);
            }
        }
    }

    // ======================================================
    // GENERAR UNA CATEGORÍA
    // ======================================================

    private void GenerateObjectsForRule(
        SpawnRule rule
    )
    {
        if (rule == null ||
            rule.prefabs == null ||
            rule.prefabs.Length == 0)
        {
            return;
        }

        int minimum =
            Mathf.Min(
                rule.minimumAmount,
                rule.maximumAmount
            );

        int maximum =
            Mathf.Max(
                rule.minimumAmount,
                rule.maximumAmount
            );

        int requestedAmount =
            UnityEngine.Random.Range(
                minimum,
                maximum + 1
            );

        int generatedForRule = 0;

        for (int objectIndex = 0;
             objectIndex < requestedAmount;
             objectIndex++)
        {
            if (generatedObjectCount >=
                maximumTotalObjects)
            {
                break;
            }

            if (TryCreateObject(rule))
            {
                generatedForRule++;
            }
        }

        Debug.Log(
            $"{rule.objectTypeName}: " +
            $"{generatedForRule}/" +
            $"{requestedAmount} generados."
        );
    }

    // ======================================================
    // INTENTAR CREAR OBJETO
    // ======================================================

    private bool TryCreateObject(
        SpawnRule rule
    )
    {
        for (int attempt = 0;
             attempt < maximumAttemptsPerObject;
             attempt++)
        {
            GameObject prefab =
                GetRandomValidPrefab(
                    rule.prefabs
                );

            if (prefab == null)
            {
                return false;
            }

            Vector2 poincarePosition =
                GetRandomPointInsideDisk();

            Vector2 localUnityPosition =
                poincarePosition *
                diskScale;

            FindNearestRoadPoint(
                poincarePosition,
                out Vector2 nearestRoadLocalPoint,
                out float distanceToRoad
            );

            float poincareScale =
                GetPoincareScaleFactor(
                    poincarePosition
                );

            GameObject candidate =
                Instantiate(
                    prefab,
                    generatedRoot
                );

            candidate.name =
                $"{rule.objectTypeName}_Candidate";

            candidate.transform.localPosition =
                new Vector3(
                    localUnityPosition.x,
                    rule.height,
                    localUnityPosition.y
                );

            candidate.transform.localScale =
                prefab.transform.localScale *
                rule.baseScale *
                poincareScale;

            if (rule.faceNearestRoad)
            {
                OrientNegativeXAxisTowardsRoad(
                    candidate.transform,
                    localUnityPosition,
                    nearestRoadLocalPoint
                );
            }
            else
            {
                candidate.transform.localRotation =
                    Quaternion.Euler(
                        0f,
                        UnityEngine.Random.Range(
                            0f,
                            360f
                        ),
                        0f
                    );
            }

            if (!TryGetObjectBounds(
                    candidate,
                    out Bounds candidateBounds
                ))
            {
                DestroyCandidate(candidate);

                Debug.LogWarning(
                    $"El prefab {prefab.name} " +
                    "no tiene ningún Renderer."
                );

                return false;
            }

            float footprintRadius =
                GetFootprintRadius(
                    candidateBounds,
                    candidate.transform.position
                );

            float requiredRoadDistance =
                roadHalfWidth +
                footprintRadius +
                rule.minimumRoadDistance;

            if (distanceToRoad <
                requiredRoadDistance)
            {
                DestroyCandidate(candidate);
                continue;
            }

            if (rule.maximumRoadDistance > 0f &&
                distanceToRoad >
                rule.maximumRoadDistance)
            {
                DestroyCandidate(candidate);
                continue;
            }

            Rect candidateFootprint =
                CreateFootprintRectangle(
                    candidateBounds,
                    rule.extraSeparation
                );

            if (OverlapsAnotherObject(
                    candidateFootprint
                ))
            {
                DestroyCandidate(candidate);
                continue;
            }

            candidate.name =
                $"{rule.objectTypeName}_" +
                $"{generatedObjectCount:000}";

            occupiedFootprints.Add(
                new OccupiedFootprint(
                    candidateFootprint
                )
            );

            generatedObjectCount++;

            return true;
        }

        return false;
    }

    // ======================================================
    // POSICIÓN ALEATORIA
    // ======================================================

    private Vector2 GetRandomPointInsideDisk()
    {
        float angle =
            UnityEngine.Random.Range(
                0f,
                Mathf.PI * 2f
            );

        float radius =
            Mathf.Sqrt(
                UnityEngine.Random.value
            ) *
            generationRadius;

        return new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        ) *
        radius;
    }

    // ======================================================
    // CARRETERA MÁS CERCANA
    // ======================================================

    private void FindNearestRoadPoint(
        Vector2 poincarePosition,
        out Vector2 nearestRoadPoint,
        out float nearestDistance
    )
    {
        Vector2 unityPosition =
            poincarePosition *
            diskScale;

        nearestRoadPoint = Vector2.zero;

        float nearestSquaredDistance =
            float.MaxValue;

        foreach (HyperbolicGeodesic geodesic
                 in geodesics)
        {
            for (int sample = 0;
                 sample <= samplesPerGeodesic;
                 sample++)
            {
                float t =
                    sample /
                    (float)samplesPerGeodesic;

                Vector2 roadPoint =
                    geodesic.GetPointAt(t) *
                    diskScale;

                float squaredDistance =
                    (
                        unityPosition -
                        roadPoint
                    ).sqrMagnitude;

                if (squaredDistance <
                    nearestSquaredDistance)
                {
                    nearestSquaredDistance =
                        squaredDistance;

                    nearestRoadPoint =
                        roadPoint;
                }
            }
        }

        nearestDistance =
            Mathf.Sqrt(
                nearestSquaredDistance
            );
    }

    // ======================================================
    // OBTENER TAMAÑO REAL DEL OBJETO
    // ======================================================

    private bool TryGetObjectBounds(
        GameObject target,
        out Bounds combinedBounds
    )
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>();

        if (renderers == null ||
            renderers.Length == 0)
        {
            combinedBounds =
                new Bounds();

            return false;
        }

        bool boundsInitialized = false;
        combinedBounds = new Bounds();

        foreach (Renderer objectRenderer
                 in renderers)
        {
            if (objectRenderer == null ||
                !objectRenderer.enabled)
            {
                continue;
            }

            if (!boundsInitialized)
            {
                combinedBounds =
                    objectRenderer.bounds;

                boundsInitialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(
                    objectRenderer.bounds
                );
            }
        }

        return boundsInitialized;
    }

    // ======================================================
    // RADIO RESPECTO A LA CARRETERA
    // ======================================================

    private float GetFootprintRadius(
        Bounds bounds,
        Vector3 pivotPosition
    )
    {
        Vector2 pivot =
            new Vector2(
                pivotPosition.x,
                pivotPosition.z
            );

        Vector2 cornerA =
            new Vector2(
                bounds.min.x,
                bounds.min.z
            );

        Vector2 cornerB =
            new Vector2(
                bounds.min.x,
                bounds.max.z
            );

        Vector2 cornerC =
            new Vector2(
                bounds.max.x,
                bounds.min.z
            );

        Vector2 cornerD =
            new Vector2(
                bounds.max.x,
                bounds.max.z
            );

        float radiusA =
            Vector2.Distance(
                pivot,
                cornerA
            );

        float radiusB =
            Vector2.Distance(
                pivot,
                cornerB
            );

        float radiusC =
            Vector2.Distance(
                pivot,
                cornerC
            );

        float radiusD =
            Vector2.Distance(
                pivot,
                cornerD
            );

        return Mathf.Max(
            radiusA,
            radiusB,
            radiusC,
            radiusD
        );
    }

    // ======================================================
    // RECTÁNGULO DE OCUPACIÓN
    // ======================================================

    private Rect CreateFootprintRectangle(
        Bounds bounds,
        float extraSeparation
    )
    {
        // Cada objeto aporta la mitad del margen.
        // Al comparar dos rectángulos, ambas mitades
        // forman la separación completa.

        float margin =
            extraSeparation *
            0.5f;

        float minimumX =
            bounds.min.x -
            margin;

        float maximumX =
            bounds.max.x +
            margin;

        float minimumZ =
            bounds.min.z -
            margin;

        float maximumZ =
            bounds.max.z +
            margin;

        return Rect.MinMaxRect(
            minimumX,
            minimumZ,
            maximumX,
            maximumZ
        );
    }

    // ======================================================
    // COMPROBAR SUPERPOSICIÓN
    // ======================================================

    private bool OverlapsAnotherObject(
        Rect candidateFootprint
    )
    {
        foreach (OccupiedFootprint occupied
                 in occupiedFootprints)
        {
            if (candidateFootprint.Overlaps(
                    occupied.rectangle,
                    true
                ))
            {
                return true;
            }
        }

        return false;
    }

    // ======================================================
    // PREFAB ALEATORIO VÁLIDO
    // ======================================================

    private GameObject GetRandomValidPrefab(
        GameObject[] prefabs
    )
    {
        if (prefabs == null ||
            prefabs.Length == 0)
        {
            return null;
        }

        int initialIndex =
            UnityEngine.Random.Range(
                0,
                prefabs.Length
            );

        for (int offset = 0;
             offset < prefabs.Length;
             offset++)
        {
            int index =
                (
                    initialIndex +
                    offset
                ) %
                prefabs.Length;

            if (prefabs[index] != null)
            {
                return prefabs[index];
            }
        }

        return null;
    }

    // ======================================================
    // ESCALA DE POINCARÉ
    // ======================================================

    private float GetPoincareScaleFactor(
        Vector2 poincarePosition
    )
    {
        return Mathf.Max(
            0.001f,
            1f -
            poincarePosition.sqrMagnitude
        );
    }

    // ======================================================
    // ORIENTACIÓN HACIA LA CARRETERA
    // ======================================================

    private void OrientNegativeXAxisTowardsRoad(
        Transform target,
        Vector2 objectPosition,
        Vector2 roadPosition
    )
    {
        Vector2 direction =
            roadPosition -
            objectPosition;

        if (direction.sqrMagnitude <
            0.000001f)
        {
            return;
        }

        direction.Normalize();

        float rotationY =
            Mathf.Atan2(
                direction.y,
                -direction.x
            ) *
            Mathf.Rad2Deg;

        target.localRotation =
            Quaternion.Euler(
                0f,
                rotationY,
                0f
            );
    }

    // ======================================================
    // DESTRUIR CANDIDATO RECHAZADO
    // ======================================================

    private void DestroyCandidate(
        GameObject candidate
    )
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

    // ======================================================
    // JERARQUÍA
    // ======================================================

    private void CreateGeneratedRoot()
    {
        GameObject rootObject =
            new GameObject(
                "GeneratedHyperbolicCity"
            );

        generatedRoot =
            rootObject.transform;

        generatedRoot.SetParent(
            transform,
            false
        );
    }

    // ======================================================
    // LIMPIAR
    // ======================================================

    [ContextMenu("Eliminar ciudad")]
    public void ClearCity()
    {
        Transform existingRoot =
            transform.Find(
                "GeneratedHyperbolicCity"
            );

        if (existingRoot != null)
        {
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
        }

        generatedRoot = null;
        occupiedFootprints.Clear();
        generatedObjectCount = 0;
    }
}