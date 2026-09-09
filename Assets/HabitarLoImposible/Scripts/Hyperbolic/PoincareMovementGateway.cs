using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PoincareMovementGateway : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private HyperbolicMovementController movementController;

    [SerializeField]
    private CharacterController playerCharacterController;

    [SerializeField]
    private Transform xrCamera;

    [Header("Lados de la transición")]
    [Tooltip(
        "GameObject vacío colocado fuera del trigger, " +
        "en el lado del disco."
    )]
    [SerializeField]
    private Transform diskSidePoint;

    [Tooltip(
        "GameObject vacío colocado fuera del trigger, " +
        "en el lado de las escaleras."
    )]
    [SerializeField]
    private Transform stairsSidePoint;

    [Header("Información")]
    [SerializeField]
    private bool playerInside;

    [SerializeField]
    private float distanceToDiskSide;

    [SerializeField]
    private float distanceToStairsSide;

    private BoxCollider gatewayCollider;

    private void Awake()
    {
        gatewayCollider =
            GetComponent<BoxCollider>();

        gatewayCollider.isTrigger = true;

        if (movementController == null)
        {
            movementController =
                FindFirstObjectByType<
                    HyperbolicMovementController
                >();
        }

        if (playerCharacterController == null)
        {
            playerCharacterController =
                FindFirstObjectByType<
                    CharacterController
                >();
        }

        if (xrCamera == null &&
            Camera.main != null)
        {
            xrCamera = Camera.main.transform;
        }
    }

    private void Start()
    {
        /*
         * El jugador comienza en el tablero.
         */
        if (movementController != null)
        {
            movementController
                .SetHyperbolicMovementActive(false);
        }
    }

    private void OnTriggerEnter(
        Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInside = true;

        /*
         * Dentro de la zona de transición se eliminan:
         *
         * - El límite del disco.
         * - La restricción a las carreteras.
         * - La reducción de velocidad.
         */
        movementController
            .SetHyperbolicMovementActive(false);

        Debug.Log(
            "Zona de transición: movimiento " +
            "hiperbólico desactivado.",
            this
        );
    }

    private void OnTriggerExit(
        Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInside = false;

        if (diskSidePoint == null ||
            stairsSidePoint == null)
        {
            Debug.LogError(
                "Falta asignar Disk Side Point o " +
                "Stairs Side Point.",
                this
            );

            return;
        }

        Vector3 playerPosition =
            GetPlayerPosition();

        distanceToDiskSide =
            Vector3.Distance(
                playerPosition,
                diskSidePoint.position
            );

        distanceToStairsSide =
            Vector3.Distance(
                playerPosition,
                stairsSidePoint.position
            );

        bool exitedTowardsDisk =
            distanceToDiskSide <
            distanceToStairsSide;

        movementController
            .SetHyperbolicMovementActive(
                exitedTowardsDisk
            );

        if (exitedTowardsDisk)
        {
            Debug.Log(
                "Salida hacia el disco: movimiento " +
                "hiperbólico activado.",
                this
            );
        }
        else
        {
            Debug.Log(
                "Salida hacia las escaleras: movimiento " +
                "hiperbólico desactivado.",
                this
            );
        }
    }

    private Vector3 GetPlayerPosition()
    {
        /*
         * En XR es mejor utilizar la cámara porque
         * representa la posición real del jugador.
         */
        if (xrCamera != null)
        {
            return xrCamera.position;
        }

        return playerCharacterController
            .transform.position;
    }

    private bool IsPlayer(
        Collider other)
    {
        if (other == null ||
            playerCharacterController == null ||
            movementController == null)
        {
            return false;
        }

        return other ==
            playerCharacterController;
    }

    private void OnDrawGizmosSelected()
    {
        if (diskSidePoint != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawSphere(
                diskSidePoint.position,
                0.15f
            );

            Gizmos.DrawLine(
                transform.position,
                diskSidePoint.position
            );
        }

        if (stairsSidePoint != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawSphere(
                stairsSidePoint.position,
                0.15f
            );

            Gizmos.DrawLine(
                transform.position,
                stairsSidePoint.position
            );
        }
    }
}