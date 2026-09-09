using Unity.XR.CoreUtils;
using UnityEngine;

public class StairGravityOrientation : MonoBehaviour
{
    [Header("REFERENCIAS")]

    [SerializeField]
    private XROrigin xrOrigin;

    [SerializeField]
    private CharacterController characterController;


    [Header("DETECCIÓN")]

    [Tooltip(
        "Capa utilizada exclusivamente por todos " +
        "los peldaños de las escaleras."
    )]
    [SerializeField]
    private LayerMask stairsLayer;

    [Tooltip(
        "Distancia adicional de detección " +
        "bajo los pies."
    )]
    [SerializeField]
    private float groundCheckDistance = 0.4f;

    [Tooltip(
        "Proporción del radio del CharacterController " +
        "utilizada en la detección."
    )]
    [Range(0.1f, 1f)]
    [SerializeField]
    private float sphereRadiusMultiplier = 0.6f;


    [Header("ORIENTACIÓN")]

    [Tooltip("Velocidad de giro en grados por segundo.")]
    [SerializeField]
    private float rotationSpeed = 60f;


    [Header("DEPURACIÓN")]

    [SerializeField]
    private bool showDebugRay = true;


    private Vector3 targetUp;
    private bool hasTargetUp;


    private void Awake()
    {
        if (characterController == null)
        {
            characterController =
                GetComponent<CharacterController>();
        }

        if (characterController == null &&
            xrOrigin != null)
        {
            characterController =
                xrOrigin.GetComponent<CharacterController>();
        }

        if (xrOrigin != null)
        {
            targetUp = xrOrigin.transform.up;
        }
    }


    private void LateUpdate()
    {
        if (xrOrigin == null ||
            characterController == null)
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


    private void DetectCurrentStep()
    {
        /*
         * En cada frame partimos de que no existe
         * ningún peldaño debajo del jugador.
         */
        hasTargetUp = false;

        Transform origin =
            xrOrigin.transform;

        Vector3 controllerCenter =
            characterController.transform
                .TransformPoint(
                    characterController.center
                );

        Vector3 downDirection =
            -origin.up;

        float sphereRadius =
            characterController.radius *
            sphereRadiusMultiplier;

        float detectionDistance =
            characterController.height * 0.5f +
            groundCheckDistance;

        if (showDebugRay)
        {
            Debug.DrawRay(
                controllerCenter,
                downDirection *
                detectionDistance,
                Color.red
            );
        }

        bool detected =
            Physics.SphereCast(
                controllerCenter,
                sphereRadius,
                downDirection,
                out RaycastHit hit,
                detectionDistance,
                stairsLayer,
                QueryTriggerInteraction.Ignore
            );

        if (!detected)
        {
            return;
        }

        /*
         * Cualquier collider perteneciente a la
         * capa Stairs se considera un peldaño.
         */
        Transform currentStep =
            hit.collider.transform;

        /*
         * La nueva vertical del jugador corresponde
         * al eje Y local del peldaño detectado.
         */
        targetUp =
            currentStep.up;

        hasTargetUp = true;

        if (showDebugRay)
        {
            Debug.DrawRay(
                currentStep.position,
                targetUp,
                Color.green
            );
        }
    }


    private void RotatePlayer()
    {
        Transform origin =
            xrOrigin.transform;

        Quaternion targetRotation =
            Quaternion.FromToRotation(
                origin.up,
                targetUp
            ) *
            origin.rotation;

        origin.rotation =
            Quaternion.RotateTowards(
                origin.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }
}