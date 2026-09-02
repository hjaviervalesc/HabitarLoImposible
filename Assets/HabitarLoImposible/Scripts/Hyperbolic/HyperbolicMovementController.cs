using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class HyperbolicMovementController : MonoBehaviour
{
    // ======================================================
    // SEGMENTO DE CARRETERA PRECÁLCULADO
    // ======================================================

    private struct RoadSegment
    {
        public Vector2 pointA;
        public Vector2 pointB;

        public RoadSegment(
            Vector2 pointA,
            Vector2 pointB
        )
        {
            this.pointA = pointA;
            this.pointB = pointB;
        }
    }

    // ======================================================
    // JUGADOR
    // ======================================================

    [Header("Jugador")]
    public Transform xrOrigin;
    public Transform xrCamera;
    public DynamicMoveProvider moveProvider;

    // ======================================================
    // DISCO
    // ======================================================

    [Header("Disco de Poincaré")]
    public Transform diskCenter;

    [Tooltip(
        "Radio físico del disco. Para un disco " +
        "de diámetro 100 debe ser 50."
    )]
    [Min(0.01f)]
    public float diskScale = 50f;

    [Tooltip(
        "Radio matemático máximo navegable."
    )]
    [Range(0.1f, 0.999f)]
    public float maximumNavigationRadius = 0.98f;

    // ======================================================
    // VELOCIDAD
    // ======================================================

    [Header("Velocidad")]
    [Tooltip(
        "Velocidad en el centro del disco."
    )]
    [Min(0f)]
    public float baseSpeed = 3f;

    [Tooltip(
        "Velocidad mínima cerca de la frontera."
    )]
    [Min(0f)]
    public float minimumSpeed = 0.05f;

    // ======================================================
    // CARRETERAS
    // ======================================================

    [Header("Restricción a las carreteras")]
    [Tooltip(
        "El jugador solo podrá moverse dentro " +
        "de esta distancia respecto a una geodésica."
    )]
    [Min(0.05f)]
    public float allowedRoadHalfWidth = 1.1f;

    [Tooltip(
        "Cantidad de segmentos utilizados para " +
        "aproximar cada geodésica."
    )]
    [Range(4, 40)]
    public int segmentsPerGeodesic = 12;

    [Tooltip(
        "Activa o desactiva la restricción a carreteras."
    )]
    public bool restrictMovementToRoads = true;

    // ======================================================
    // INFORMACIÓN
    // ======================================================

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
    private int roadSegmentCount;

    // ======================================================
    // DATOS INTERNOS
    // ======================================================

    private readonly List<RoadSegment> roadSegments =
        new List<RoadSegment>();

    private bool roadsReady;

    // ======================================================
    // INICIO
    // ======================================================

    private IEnumerator Start()
    {
        FindMissingReferences();
        ValidateConfiguration();

        // Esperamos a que se construyan
        // las geodésicas de la teselación.

        yield return null;

        BuildRoadSegments();
        UpdateMovementSpeed();
    }

    // ======================================================
    // ACTUALIZACIÓN
    // ======================================================

    private void Update()
    {
        UpdateMovementSpeed();
    }

    private void LateUpdate()
    {
        KeepPlayerInsideDisk();

        if (restrictMovementToRoads)
        {
            KeepPlayerOnRoads();
        }
    }

    // ======================================================
    // VELOCIDAD HIPERBÓLICA
    // ======================================================

    private void UpdateMovementSpeed()
    {
        if (moveProvider == null ||
            diskCenter == null)
        {
            return;
        }

        Vector2 poincarePosition =
            GetPlayerPoincarePosition();

        currentMathematicalRadius =
            poincarePosition.magnitude;

        // Factor relativo de Poincaré:
        //
        // factor = 1 - r²

        currentPoincareFactor =
            Mathf.Max(
                0f,
                1f -
                poincarePosition.sqrMagnitude
            );

        currentMovementSpeed =
            baseSpeed *
            currentPoincareFactor;

        currentMovementSpeed =
            Mathf.Max(
                currentMovementSpeed,
                minimumSpeed
            );

        moveProvider.moveSpeed =
            currentMovementSpeed;
    }

    // ======================================================
    // POSICIÓN DEL JUGADOR
    // ======================================================

    private Vector2 GetPlayerPoincarePosition()
    {
        Vector2 localPosition =
            GetPlayerLocalPlanarPosition();

        return localPosition /
               diskScale;
    }

    private Vector2 GetPlayerLocalPlanarPosition()
    {
        Transform playerReference =
            xrCamera != null
                ? xrCamera
                : xrOrigin;

        if (playerReference == null ||
            diskCenter == null)
        {
            return Vector2.zero;
        }

        Vector3 localPosition =
            diskCenter.InverseTransformPoint(
                playerReference.position
            );

        return new Vector2(
            localPosition.x,
            localPosition.z
        );
    }

    // ======================================================
    // CONSTRUIR SEGMENTOS DE CARRETERA
    // ======================================================

    [ContextMenu("Reconstruir segmentos de carreteras")]
    public void BuildRoadSegments()
    {
        roadSegments.Clear();
        roadsReady = false;

        HyperbolicGeodesic[] geodesics =
            FindObjectsByType<HyperbolicGeodesic>(
                FindObjectsSortMode.None
            );

        foreach (HyperbolicGeodesic geodesic
                 in geodesics)
        {
            if (geodesic == null ||
                !geodesic.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector2 previousPoint =
                geodesic.GetPointAt(0f) *
                diskScale;

            for (int segment = 1;
                 segment <= segmentsPerGeodesic;
                 segment++)
            {
                float t =
                    segment /
                    (float)segmentsPerGeodesic;

                Vector2 currentPoint =
                    geodesic.GetPointAt(t) *
                    diskScale;

                roadSegments.Add(
                    new RoadSegment(
                        previousPoint,
                        currentPoint
                    )
                );

                previousPoint =
                    currentPoint;
            }
        }

        roadSegmentCount =
            roadSegments.Count;

        roadsReady =
            roadSegments.Count > 0;

        if (roadsReady)
        {
            Debug.Log(
                $"Carreteras preparadas para locomoción.\n" +
                $"Geodésicas: {geodesics.Length}\n" +
                $"Segmentos: {roadSegments.Count}"
            );
        }
        else
        {
            Debug.LogError(
                "HyperbolicMovementController: " +
                "no se encontraron geodésicas."
            );
        }
    }

    // ======================================================
    // MANTENER AL JUGADOR EN LAS CARRETERAS
    // ======================================================

    private void KeepPlayerOnRoads()
    {
        if (!roadsReady ||
            xrOrigin == null ||
            diskCenter == null)
        {
            return;
        }

        Vector2 playerPosition =
            GetPlayerLocalPlanarPosition();

        Vector2 nearestRoadPoint =
            FindNearestRoadPoint(
                playerPosition,
                out float distanceToRoad
            );

        currentDistanceToRoad =
            distanceToRoad;

        if (distanceToRoad <=
            allowedRoadHalfWidth)
        {
            return;
        }

        Vector2 directionFromRoad =
            playerPosition -
            nearestRoadPoint;

        Vector2 correctedPosition;

        if (directionFromRoad.sqrMagnitude <
            0.000001f)
        {
            correctedPosition =
                nearestRoadPoint;
        }
        else
        {
            correctedPosition =
                nearestRoadPoint +
                directionFromRoad.normalized *
                allowedRoadHalfWidth;
        }

        MoveXROriginToLocalPlanarPosition(
            correctedPosition
        );
    }

    // ======================================================
    // PUNTO MÁS CERCANO DE LAS CARRETERAS
    // ======================================================

    private Vector2 FindNearestRoadPoint(
        Vector2 playerPosition,
        out float nearestDistance
    )
    {
        Vector2 nearestPoint =
            roadSegments[0].pointA;

        float nearestSquaredDistance =
            float.MaxValue;

        foreach (RoadSegment segment
                 in roadSegments)
        {
            Vector2 candidate =
                ClosestPointOnSegment(
                    playerPosition,
                    segment.pointA,
                    segment.pointB
                );

            float squaredDistance =
                (
                    playerPosition -
                    candidate
                ).sqrMagnitude;

            if (squaredDistance <
                nearestSquaredDistance)
            {
                nearestSquaredDistance =
                    squaredDistance;

                nearestPoint =
                    candidate;
            }
        }

        nearestDistance =
            Mathf.Sqrt(
                nearestSquaredDistance
            );

        return nearestPoint;
    }

    private Vector2 ClosestPointOnSegment(
        Vector2 point,
        Vector2 segmentA,
        Vector2 segmentB
    )
    {
        Vector2 segment =
            segmentB -
            segmentA;

        float squaredLength =
            segment.sqrMagnitude;

        if (squaredLength <
            0.000001f)
        {
            return segmentA;
        }

        float t =
            Vector2.Dot(
                point - segmentA,
                segment
            ) /
            squaredLength;

        t =
            Mathf.Clamp01(t);

        return segmentA +
               segment *
               t;
    }

    // ======================================================
    // MANTENER AL JUGADOR DENTRO DEL DISCO
    // ======================================================

    private void KeepPlayerInsideDisk()
    {
        if (xrOrigin == null ||
            diskCenter == null)
        {
            return;
        }

        Vector2 playerPosition =
            GetPlayerLocalPlanarPosition();

        float maximumPhysicalRadius =
            maximumNavigationRadius *
            diskScale;

        if (playerPosition.sqrMagnitude <=
            maximumPhysicalRadius *
            maximumPhysicalRadius)
        {
            return;
        }

        Vector2 correctedPosition =
            playerPosition.normalized *
            maximumPhysicalRadius;

        MoveXROriginToLocalPlanarPosition(
            correctedPosition
        );
    }

    // ======================================================
    // CORREGIR XR ORIGIN
    // ======================================================

    private void MoveXROriginToLocalPlanarPosition(
        Vector2 correctedPlanarPosition
    )
    {
        if (xrOrigin == null ||
            diskCenter == null)
        {
            return;
        }

        Transform playerReference =
            xrCamera != null
                ? xrCamera
                : xrOrigin;

        Vector3 currentLocalPosition =
            diskCenter.InverseTransformPoint(
                playerReference.position
            );

        Vector3 correctedLocalPosition =
            new Vector3(
                correctedPlanarPosition.x,
                currentLocalPosition.y,
                correctedPlanarPosition.y
            );

        Vector3 correctedWorldPosition =
            diskCenter.TransformPoint(
                correctedLocalPosition
            );

        Vector3 correction =
            correctedWorldPosition -
            playerReference.position;

        // La cámara sigue dependiendo del tracking.
        // La corrección se aplica al XR Origin.

        xrOrigin.position +=
            correction;
    }

    // ======================================================
    // REFERENCIAS
    // ======================================================

    private void FindMissingReferences()
    {
        if (xrOrigin == null)
        {
            GameObject xrOriginObject =
                GameObject.Find(
                    "XR Origin"
                );

            if (xrOriginObject != null)
            {
                xrOrigin =
                    xrOriginObject.transform;
            }
        }

        if (xrCamera == null &&
            Camera.main != null)
        {
            xrCamera =
                Camera.main.transform;
        }

        if (moveProvider == null)
        {
            moveProvider =
                FindFirstObjectByType
                    <DynamicMoveProvider>();
        }
    }

    private void ValidateConfiguration()
    {
        if (xrOrigin == null)
        {
            Debug.LogError(
                "Falta asignar XR Origin."
            );
        }

        if (moveProvider == null)
        {
            Debug.LogError(
                "Falta asignar Dynamic Move Provider."
            );
        }

        if (diskCenter == null)
        {
            Debug.LogError(
                "Falta asignar Disk Center."
            );
        }
    }
}