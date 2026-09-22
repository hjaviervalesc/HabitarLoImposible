using UnityEngine;

/// <summary>
/// Activa o desactiva la deformación del tablero según el lado por el que el jugador atraviesa el trigger.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DeformationGateway : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private DeformationSectionManager deformationManager;

    [SerializeField]
    private CharacterController playerCharacterController;

    [SerializeField]
    private Transform xrCamera;

    [Header("Lados de la transición")]
    [Tooltip("GameObject vacío situado fuera del trigger, en el lado del tablero deformable.")]
    [SerializeField]
    private Transform deformationSidePoint;

    [Tooltip("GameObject vacío situado fuera del trigger, en el lado donde la deformación debe estar desactivada.")]
    [SerializeField]
    private Transform outsideSidePoint;

    [Header("Información")]
    [SerializeField]
    private bool playerInside;

    [SerializeField]
    private bool deformationActive;

    [SerializeField]
    private float distanceToDeformationSide;

    [SerializeField]
    private float distanceToOutsideSide;

    private BoxCollider gatewayCollider;

    /// <summary>
    /// Configura el collider como trigger y busca automáticamente las referencias que no estén asignadas.
    /// </summary>
    private void Awake()
    {
        gatewayCollider = GetComponent<BoxCollider>();
        gatewayCollider.isTrigger = true;

        if (deformationManager == null)
        {
            deformationManager = FindFirstObjectByType<DeformationSectionManager>();
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

    /// <summary>
    /// Consulta si el componente de deformación está habilitado sin modificar su estado inicial.
    /// </summary>
    private void Start()
    {
        if (deformationManager != null)
        {
            deformationActive = deformationManager.enabled;
        }
    }

    /// <summary>
    /// Registra la entrada del jugador en el gateway sin cambiar el estado de la deformación.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInside = true;
    }

    /// <summary>
    /// Determina el lado de salida mediante la distancia a los puntos de referencia y activa o desactiva la deformación.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInside = false;

        if (deformationManager == null)
        {
            Debug.LogError("No se ha asignado DeformationSectionManager.", this);
            return;
        }

        if (deformationSidePoint == null || outsideSidePoint == null)
        {
            Debug.LogError("Falta asignar Deformation Side Point u Outside Side Point.", this);
            return;
        }

        Vector3 playerPosition = GetPlayerPosition();

        distanceToDeformationSide = Vector3.Distance(playerPosition, deformationSidePoint.position);
        distanceToOutsideSide = Vector3.Distance(playerPosition, outsideSidePoint.position);

        bool exitedTowardsDeformation = distanceToDeformationSide < distanceToOutsideSide;

        deformationManager.SetDeformationActive(exitedTowardsDeformation);
        deformationActive = exitedTowardsDeformation;
    }

    /// <summary>
    /// Obtiene la posición de la cámara XR o, si no está asignada, la del CharacterController; devuelve cero si faltan ambas referencias.
    /// </summary>
    private Vector3 GetPlayerPosition()
    {
        if (xrCamera != null)
        {
            return xrCamera.position;
        }

        if (playerCharacterController != null)
        {
            return playerCharacterController.transform.position;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Comprueba si el collider corresponde al CharacterController del jugador o tiene ese componente en su jerarquía de padres.
    /// </summary>
    private bool IsPlayer(Collider other)
    {
        if (other == null || playerCharacterController == null)
        {
            return false;
        }

        if (other == playerCharacterController)
        {
            return true;
        }

        CharacterController detectedController = other.GetComponentInParent<CharacterController>();

        return detectedController == playerCharacterController;
    }

    /// <summary>
    /// Dibuja los puntos de referencia y sus conexiones con el gateway cuando el objeto está seleccionado en el editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (deformationSidePoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(deformationSidePoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, deformationSidePoint.position);
        }

        if (outsideSidePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(outsideSidePoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, outsideSidePoint.position);
        }
    }
}