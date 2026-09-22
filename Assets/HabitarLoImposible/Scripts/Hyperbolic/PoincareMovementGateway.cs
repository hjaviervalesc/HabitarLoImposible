using UnityEngine;

/// <summary>
/// Gestiona la transición entre las escaleras y el disco de Poincaré, activando o desactivando el movimiento hiperbólico según el lado por el que sale el jugador.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class PoincareMovementGateway : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private HyperbolicMovementController movementController;
    [SerializeField] private CharacterController playerCharacterController;
    [SerializeField] private Transform xrCamera;

    [Header("Lados de la transición")]
    [Tooltip("GameObject vacío colocado fuera del trigger, en el lado del disco.")]
    [SerializeField] private Transform diskSidePoint;

    [Tooltip("GameObject vacío colocado fuera del trigger, en el lado de las escaleras.")]
    [SerializeField] private Transform stairsSidePoint;

    [Header("Información")]
    [SerializeField] private bool playerInside;
    [SerializeField] private float distanceToDiskSide;
    [SerializeField] private float distanceToStairsSide;

    private BoxCollider gatewayCollider;

    private void Awake()
    {
        gatewayCollider = GetComponent<BoxCollider>();
        gatewayCollider.isTrigger = true;

        if (movementController == null)
        {
            movementController = FindFirstObjectByType<HyperbolicMovementController>();
        }

        if (playerCharacterController == null)
        {
            playerCharacterController = FindFirstObjectByType<CharacterController>();
        }

        if (xrCamera == null && Camera.main != null)
        {
            xrCamera = Camera.main.transform;
        }
    }

    private void Start()
    {
        if (movementController != null)
        {
            movementController.SetHyperbolicMovementActive(false);
        }
    }

    /// <summary>
    /// Desactiva las restricciones del movimiento hiperbólico cuando el jugador entra en la zona de transición.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInside = true;
        movementController.SetHyperbolicMovementActive(false);
    }

    /// <summary>
    /// Determina el lado por el que ha salido el jugador y activa el movimiento hiperbólico únicamente si se dirige hacia el disco.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInside = false;

        if (diskSidePoint == null || stairsSidePoint == null)
        {
            Debug.LogError("Falta asignar Disk Side Point o Stairs Side Point.", this);
            return;
        }

        Vector3 playerPosition = GetPlayerPosition();

        distanceToDiskSide = Vector3.Distance(playerPosition, diskSidePoint.position);
        distanceToStairsSide = Vector3.Distance(playerPosition, stairsSidePoint.position);

        bool exitedTowardsDisk = distanceToDiskSide < distanceToStairsSide;

        movementController.SetHyperbolicMovementActive(exitedTowardsDisk);
    }

    /// <summary>
    /// Obtiene la posición real del jugador utilizando preferentemente la cámara del sistema XR.
    /// </summary>
    private Vector3 GetPlayerPosition()
    {
        if (xrCamera != null)
        {
            return xrCamera.position;
        }

        return playerCharacterController.transform.position;
    }

    /// <summary>
    /// Comprueba si el collider detectado pertenece al controlador del jugador.
    /// </summary>
    private bool IsPlayer(Collider other)
    {
        if (other == null || playerCharacterController == null || movementController == null)
        {
            return false;
        }

        return other == playerCharacterController;
    }

    /// <summary>
    /// Representa visualmente en el editor los puntos correspondientes al disco y a las escaleras.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (diskSidePoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(diskSidePoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, diskSidePoint.position);
        }

        if (stairsSidePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(stairsSidePoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, stairsSidePoint.position);
        }
    }
}