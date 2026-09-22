using Unity.XR.CoreUtils;
using UnityEngine;

/// <summary>
/// Orienta progresivamente el XR Origin según el peldaño detectado bajo el jugador, siempre que el primer impacto del SphereCast pertenezca a una capa de escaleras.
/// </summary>
public class StairGravityOrientation : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private XROrigin xrOrigin;

    [SerializeField]
    private CharacterController characterController;

    [Header("Detección")]
    [Tooltip("Capa utilizada exclusivamente por los peldaños.")]
    [SerializeField]
    private LayerMask stairsLayer;

    [Tooltip("Capas que detecta el SphereCast. Incluir escaleras " + "y obstáculos. Excluir las capas del jugador y del avatar.")]
    [SerializeField]
    private LayerMask detectionLayers = ~0;

    [Tooltip("Distancia adicional de detección bajo los pies.")]
    [SerializeField]
    private float groundCheckDistance = 0.4f;

    [Tooltip("Proporción del radio del CharacterController " + "utilizada en la detección.")]
    [Range(0.1f, 1f)]
    [SerializeField]
    private float sphereRadiusMultiplier = 0.6f;

    [Header("Orientación")]
    [Tooltip("Velocidad de giro en grados por segundo.")]
    [SerializeField]
    private float rotationSpeed = 60f;

    private Vector3 targetUp;
    private bool hasTargetUp;

    /// <summary>
    /// Busca el CharacterController si no está asignado e inicializa la dirección vertical objetivo con la del XR Origin.
    /// </summary>
    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (characterController == null && xrOrigin != null)
        {
            characterController = xrOrigin.GetComponent<CharacterController>();
        }

        if (xrOrigin != null)
        {
            targetUp = xrOrigin.transform.up;
        }
    }

    /// <summary>
    /// Comprueba las referencias, detecta el peldaño y aplica el giro si se ha obtenido una dirección vertical válida.
    /// </summary>
    private void LateUpdate()
    {
        if (xrOrigin == null || characterController == null)
        {
            return;
        }

        DetectCurrentStep();

        if (!hasTargetUp)
        {
            return;
        }

        RotatePlayer();
    }

    /// <summary>
    /// Realiza un SphereCast hacia abajo respecto al XR Origin y obtiene la vertical del primer collider detectado únicamente si pertenece a stairsLayer.
    /// </summary>
    private void DetectCurrentStep()
    {
        hasTargetUp = false;

        Transform origin = xrOrigin.transform;

        Vector3 controllerCenter = characterController.transform.TransformPoint(characterController.center);
        Vector3 downDirection = -origin.up;

        float sphereRadius = characterController.radius * sphereRadiusMultiplier;
        float detectionDistance = characterController.height * 0.5f + groundCheckDistance;

        bool detected = Physics.SphereCast(controllerCenter, sphereRadius, downDirection, out RaycastHit hit, detectionDistance, detectionLayers, QueryTriggerInteraction.Ignore);

        if (!detected)
        {
            return;
        }

        bool isStair = (stairsLayer.value & (1 << hit.collider.gameObject.layer)) != 0;

        if (!isStair)
        {
            return;
        }

        targetUp = hit.collider.transform.up;
        hasTargetUp = true;
    }

    /// <summary>
    /// Alinea progresivamente la vertical del XR Origin con la del peldaño detectado utilizando la velocidad de giro configurada.
    /// </summary>
    private void RotatePlayer()
    {
        Transform origin = xrOrigin.transform;

        Quaternion targetRotation = Quaternion.FromToRotation(origin.up, targetUp) * origin.rotation;

        origin.rotation = Quaternion.RotateTowards(origin.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}