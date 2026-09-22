using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

/// <summary>
/// Adapta la velocidad del jugador a su posición en el disco de Poincaré y restringe la navegación al disco y, opcionalmente, a las carreteras.
/// </summary>
public class HyperbolicMovementController : MonoBehaviour
{
    /// <summary>
    /// Representa un tramo recto de carretera mediante sus extremos en el plano local del disco.
    /// </summary>
    private struct RoadSegment
    {
        public Vector2 pointA;
        public Vector2 pointB;

        /// <summary>
        /// Inicializa los extremos de un segmento de carretera.
        /// </summary>
        public RoadSegment(Vector2 pointA, Vector2 pointB)
        {
            this.pointA = pointA;
            this.pointB = pointB;
        }
    }

    [Header("Espacio hiperbólico")]
    [SerializeField]
    private HyperbolicSpace hyperbolicSpace;

    [Tooltip("Transform que representa el centro y " + "la orientación del disco.")]
    public Transform diskCenter;

    [Header("Jugador")]
    public Transform xrOrigin;

    public Transform xrCamera;

    public DynamicMoveProvider moveProvider;

    [Header("Activación")]
    [Tooltip("Se activa automáticamente al entrar " + "en la zona del disco.")]
    [SerializeField]
    private bool hyperbolicMovementActive;

    [Header("Límite de navegación")]
    [Range(0.1f, 0.999f)]
    public float maximumNavigationRadius = 0.98f;

    [Header("Velocidad")]
    [Tooltip("Velocidad en el centro.")]
    [Min(0f)]
    public float baseSpeed = 3f;

    [Tooltip("Velocidad mínima cerca del borde.")]
    [Min(0f)]
    public float minimumSpeed = 0.05f;

    [Header("Restricción a carreteras")]
    [Tooltip("Mitad de la zona navegable de la carretera " + "en el centro del disco.")]
    [Min(0.05f)]
    public float allowedRoadHalfWidth = 1.1f;

    [Range(4, 40)]
    public int segmentsPerGeodesic = 12;

    public bool restrictMovementToRoads = true;

    [Header("Información")]
    [SerializeField]
    private float currentMathematicalRadius;

    [SerializeField]
    private float currentPoincareFactor;

    [SerializeField]
    private float currentMovementSpeed;

    [SerializeField]
    private float currentDistanceToRoad;

    [SerializeField]
    private float currentAllowedRoadHalfWidth;

    [SerializeField]
    private int roadSegmentCount;

    private readonly List<RoadSegment> roadSegments = new List<RoadSegment>();

    private bool roadsReady;
    private float normalMovementSpeed;

    public bool HyperbolicMovementActive => hyperbolicMovementActive;

    public float CurrentMovementSpeed => currentMovementSpeed;

    /// <summary>
    /// Obtiene la proporción entre la velocidad actual y la velocidad base para ajustar la animación, o devuelve uno cuando el movimiento hiperbólico está inactivo o la velocidad base es casi nula.
    /// </summary>
    public float AnimationMovementFactor
    {
        get
        {
            if (!hyperbolicMovementActive)
            {
                return 1f;
            }

            if (baseSpeed <= 0.001f)
            {
                return 1f;
            }

            return currentMovementSpeed / baseSpeed;
        }
    }

    /// <summary>
    /// Busca y valida las referencias, guarda la velocidad normal, desactiva el movimiento hiperbólico y espera un fotograma antes de construir los segmentos de carretera.
    /// </summary>
    private IEnumerator Start()
    {
        FindMissingReferences();

        if (!ValidateConfiguration())
        {
            enabled = false;
            yield break;
        }

        normalMovementSpeed = moveProvider.moveSpeed;

        hyperbolicMovementActive = false;

        yield return null;

        BuildRoadSegments();
    }

    /// <summary>
    /// Actualiza la velocidad mientras el movimiento hiperbólico está activo.
    /// </summary>
    private void Update()
    {
        if (!hyperbolicMovementActive)
        {
            return;
        }

        UpdateMovementSpeed();
    }

    /// <summary>
    /// Aplica el límite del disco y, si corresponde, la restricción a carreteras mientras el movimiento hiperbólico está activo.
    /// </summary>
    private void LateUpdate()
    {
        if (!hyperbolicMovementActive)
        {
            return;
        }

        KeepPlayerInsideDisk();

        if (restrictMovementToRoads)
        {
            KeepPlayerOnRoads();
        }
    }

    /// <summary>
    /// Activa el ajuste hiperbólico de velocidad y prepara las carreteras, o restaura la velocidad normal y reinicia la información al desactivarlo.
    /// </summary>
    public void SetHyperbolicMovementActive(bool active)
    {
        if (hyperbolicMovementActive == active)
        {
            return;
        }

        if (active)
        {
            normalMovementSpeed = moveProvider.moveSpeed;

            hyperbolicMovementActive = true;

            if (!roadsReady)
            {
                BuildRoadSegments();
            }

            UpdateMovementSpeed();
        }
        else
        {
            hyperbolicMovementActive = false;

            moveProvider.moveSpeed = normalMovementSpeed;

            currentMathematicalRadius = 0f;
            currentPoincareFactor = 0f;
            currentMovementSpeed = normalMovementSpeed;

            currentDistanceToRoad = 0f;
            currentAllowedRoadHalfWidth = 0f;
        }
    }


    /// <summary>
    /// Restaura la velocidad normal si el componente se desactiva mientras el movimiento hiperbólico está activo.
    /// </summary>
    private void OnDisable()
    {
        if (moveProvider != null && hyperbolicMovementActive)
        {
            moveProvider.moveSpeed = normalMovementSpeed;
        }
    }

    /// <summary>
    /// Calcula la velocidad a partir del factor de escala en la posición del jugador, aplica la velocidad mínima y actualiza el proveedor de movimiento.
    /// </summary>
    private void UpdateMovementSpeed()
    {
        if (moveProvider == null || hyperbolicSpace == null)
        {
            return;
        }

        Vector2 poincarePosition = GetPlayerPoincarePosition();

        currentMathematicalRadius = poincarePosition.magnitude;

        currentPoincareFactor = hyperbolicSpace.GetScaleFactor(poincarePosition);

        currentMovementSpeed = Mathf.Max(baseSpeed * currentPoincareFactor, minimumSpeed);

        moveProvider.moveSpeed = currentMovementSpeed;
    }

    /// <summary>
    /// Convierte la posición planar local del jugador a coordenadas del disco de Poincaré.
    /// </summary>
    private Vector2 GetPlayerPoincarePosition()
    {
        Vector2 localPosition = GetPlayerLocalPlanarPosition();

        return hyperbolicSpace.UnityToPoincare(localPosition);
    }

    /// <summary>
    /// Obtiene la posición X-Z relativa al disco usando la cámara o, si no está asignada, el XR Origin.
    /// </summary>
    private Vector2 GetPlayerLocalPlanarPosition()
    {
        Transform playerReference = xrCamera != null ? xrCamera : xrOrigin;

        if (playerReference == null || diskCenter == null)
        {
            return Vector2.zero;
        }

        Vector3 localPosition = diskCenter.InverseTransformPoint(playerReference.position);

        return new Vector2(localPosition.x, localPosition.z);
    }

    /// <summary>
    /// Aproxima las geodésicas activas mediante segmentos rectos y actualiza la disponibilidad y el número de segmentos de carretera.
    /// </summary>
    [ContextMenu("Reconstruir segmentos de carreteras")]
    public void BuildRoadSegments()
    {
        if (hyperbolicSpace == null)
        {
            FindMissingReferences();
        }

        roadSegments.Clear();
        roadsReady = false;

        if (hyperbolicSpace == null)
        {
            return;
        }

        HyperbolicGeodesic[] geodesics = FindObjectsByType<HyperbolicGeodesic>(FindObjectsSortMode.None);

        foreach (HyperbolicGeodesic geodesic in geodesics)
        {
            if (geodesic == null || !geodesic.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector2 previousPoint = hyperbolicSpace.PoincareToUnity2D(geodesic.GetPointAt(0f));

            for (int segment = 1; segment <= segmentsPerGeodesic; segment++)
            {
                float t = segment / (float)segmentsPerGeodesic;

                Vector2 currentPoint = hyperbolicSpace.PoincareToUnity2D(geodesic.GetPointAt(t));

                roadSegments.Add(new RoadSegment(previousPoint, currentPoint));

                previousPoint = currentPoint;
            }
        }

        roadSegmentCount = roadSegments.Count;

        roadsReady = roadSegments.Count > 0;

        if (!roadsReady)
        {
            Debug.LogError("HyperbolicMovementController: " + "no se encontraron carreteras.", this);
        }
    }

    /// <summary>
    /// Corrige la posición del jugador si supera la anchura permitida respecto a la carretera más cercana, ajustada por el factor de escala.
    /// </summary>
    private void KeepPlayerOnRoads()
    {
        if (!roadsReady || xrOrigin == null || diskCenter == null || hyperbolicSpace == null)
        {
            return;
        }

        Vector2 playerPosition = GetPlayerLocalPlanarPosition();

        Vector2 nearestRoadPoint = FindNearestRoadPoint(playerPosition, out float distanceToRoad);

        currentDistanceToRoad = distanceToRoad;

        Vector2 nearestRoadPoincare = hyperbolicSpace.UnityToPoincare(nearestRoadPoint);

        currentAllowedRoadHalfWidth = allowedRoadHalfWidth * hyperbolicSpace.GetScaleFactor(nearestRoadPoincare);

        if (distanceToRoad <= currentAllowedRoadHalfWidth)
        {
            return;
        }

        Vector2 directionFromRoad = playerPosition - nearestRoadPoint;

        Vector2 correctedPosition;

        if (directionFromRoad.sqrMagnitude < 0.000001f)
        {
            correctedPosition = nearestRoadPoint;
        }
        else
        {
            correctedPosition = nearestRoadPoint + directionFromRoad.normalized * currentAllowedRoadHalfWidth;
        }

        MoveXROriginToLocalPlanarPosition(correctedPosition);
    }

    /// <summary>
    /// Busca el punto más cercano entre todos los segmentos de carretera y devuelve su distancia al jugador.
    /// </summary>
    private Vector2 FindNearestRoadPoint(Vector2 playerPosition, out float nearestDistance)
    {
        if (roadSegments.Count == 0)
        {
            nearestDistance = float.MaxValue;

            return playerPosition;
        }

        Vector2 nearestPoint = roadSegments[0].pointA;

        float nearestSquaredDistance = float.MaxValue;

        foreach (RoadSegment segment in roadSegments)
        {
            Vector2 candidate = ClosestPointOnSegment(playerPosition, segment.pointA, segment.pointB);

            float squaredDistance = (playerPosition - candidate).sqrMagnitude;

            if (squaredDistance < nearestSquaredDistance)
            {
                nearestSquaredDistance = squaredDistance;

                nearestPoint = candidate;
            }
        }

        nearestDistance = Mathf.Sqrt(nearestSquaredDistance);

        return nearestPoint;
    }

    /// <summary>
    /// Proyecta un punto sobre un segmento y limita la proyección a sus extremos.
    /// </summary>
    private Vector2 ClosestPointOnSegment(Vector2 point, Vector2 segmentA, Vector2 segmentB)
    {
        Vector2 segment = segmentB - segmentA;

        float squaredLength = segment.sqrMagnitude;

        if (squaredLength < 0.000001f)
        {
            return segmentA;
        }

        float t = Vector2.Dot(point - segmentA, segment) / squaredLength;

        t = Mathf.Clamp01(t);

        return segmentA + segment * t;
    }

    /// <summary>
    /// Corrige la posición del jugador cuando excede el radio máximo de navegación del disco.
    /// </summary>
    private void KeepPlayerInsideDisk()
    {
        if (xrOrigin == null || diskCenter == null || hyperbolicSpace == null)
        {
            return;
        }

        Vector2 playerPosition = GetPlayerLocalPlanarPosition();

        Vector2 poincarePosition = hyperbolicSpace.UnityToPoincare(playerPosition);

        Vector2 correctedPoincare = hyperbolicSpace.ClampInsideDisk(poincarePosition, maximumNavigationRadius);

        if ((correctedPoincare - poincarePosition).sqrMagnitude < 0.000001f)
        {
            return;
        }

        Vector2 correctedUnityPosition = hyperbolicSpace.PoincareToUnity2D(correctedPoincare);

        MoveXROriginToLocalPlanarPosition(correctedUnityPosition);
    }

    /// <summary>
    /// Desplaza el XR Origin para situar la referencia del jugador en la posición planar indicada, conservando su altura local respecto al disco.
    /// </summary>
    private void MoveXROriginToLocalPlanarPosition(Vector2 correctedPlanarPosition)
    {
        if (xrOrigin == null || diskCenter == null)
        {
            return;
        }

        Transform playerReference = xrCamera != null ? xrCamera : xrOrigin;

        Vector3 currentLocalPosition = diskCenter.InverseTransformPoint(playerReference.position);

        Vector3 correctedLocalPosition = new Vector3(correctedPlanarPosition.x, currentLocalPosition.y, correctedPlanarPosition.y);

        Vector3 correctedWorldPosition = diskCenter.TransformPoint(correctedLocalPosition);

        Vector3 correction = correctedWorldPosition - playerReference.position;

        xrOrigin.position += correction;
    }

    /// <summary>
    /// Busca las referencias no asignadas del espacio hiperbólico, el centro del disco, el jugador, la cámara y el proveedor de movimiento.
    /// </summary>
    private void FindMissingReferences()
    {
        if (hyperbolicSpace == null)
        {
            hyperbolicSpace = GetComponentInParent<HyperbolicSpace>();
        }

        if (diskCenter == null && hyperbolicSpace != null)
        {
            diskCenter = hyperbolicSpace.transform;
        }

        if (xrOrigin == null)
        {
            GameObject xrOriginObject = GameObject.Find("XR Origin");

            if (xrOriginObject != null)
            {
                xrOrigin = xrOriginObject.transform;
            }
        }

        if (xrCamera == null && Camera.main != null)
        {
            xrCamera = Camera.main.transform;
        }

        if (moveProvider == null)
        {
            moveProvider = FindFirstObjectByType<DynamicMoveProvider>();
        }
    }

    /// <summary>
    /// Comprueba las referencias necesarias e informa de las que faltan antes de permitir la inicialización.
    /// </summary>
    private bool ValidateConfiguration()
    {
        bool valid = true;

        if (hyperbolicSpace == null)
        {
            Debug.LogError("Falta asignar HyperbolicSpace.", this);

            valid = false;
        }

        if (xrOrigin == null)
        {
            Debug.LogError("Falta asignar XR Origin.", this);

            valid = false;
        }

        if (moveProvider == null)
        {
            Debug.LogError("Falta asignar Dynamic Move Provider.", this);

            valid = false;
        }

        if (diskCenter == null)
        {
            Debug.LogError("Falta asignar Disk Center.", this);

            valid = false;
        }

        return valid;
    }
}