using UnityEngine;

public class StairOrientation : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CharacterController characterController;

    [Header("Detección")]
    [SerializeField] private LayerMask stairMask;
    [SerializeField] private float raycastDistance = 2f;

    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 5f;

    private Vector3 currentNormal = Vector3.up;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        DetectStairSurface();
        RotateToSurface();
    }

    private void DetectStairSurface()
    {
        Vector3 origin = characterController.bounds.center;

        if (Physics.Raycast(
            origin,
            -transform.up,
            out RaycastHit hit,
            raycastDistance,
            stairMask,
            QueryTriggerInteraction.Ignore))
        {
            currentNormal = hit.normal;
        }
        else
        {
            // Si no encontramos escalera,
            // volvemos progresivamente a la vertical normal.
            currentNormal = Vector3.up;
        }
    }

    private void RotateToSurface()
    {
        /*
         * Queremos que el eje Y del XR Origin
         * coincida con la normal de la superficie.
         */
        Quaternion targetRotation = Quaternion.FromToRotation(
            transform.up,
            currentNormal
        ) * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    //private void OnDrawGizmos()
    //{
    //    if (characterController == null)
    //        return;

    //    Vector3 origin = characterController.bounds.center;

    //    // Rayo utilizado para detectar la escalera
    //    Gizmos.color = Color.yellow;

    //    Gizmos.DrawLine(
    //        origin,
    //        origin - transform.up * raycastDistance
    //    );

    //    // Normal detectada
    //    Gizmos.color = Color.green;

    //    Gizmos.DrawLine(
    //        origin,
    //        origin + currentNormal
    //    );
    //}
}