using UnityEngine;
using Unity.XR.CoreUtils;

public class StairGravityController : MonoBehaviour
{
    [Header("XR")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Escaleras")]
    [SerializeField] private LayerMask stairLayer;
    [SerializeField] private float raycastDistance = 2f;

    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Gravedad")]
    [SerializeField] private float gravity = 9.81f;

    private Vector3 currentUp = Vector3.up;
    private Vector3 currentGravity = Vector3.down;

    private void Reset()
    {
        xrOrigin = GetComponent<XROrigin>();
    }

    private void Start()
    {
        if (xrOrigin == null)
            xrOrigin = GetComponent<XROrigin>();

        currentUp = xrOrigin.Origin.transform.up;
        currentGravity = -currentUp;
    }

    private void Update()
    {
        if (xrOrigin == null || xrOrigin.Origin == null)
            return;

        Transform origin = xrOrigin.Origin.transform;

        DetectStairSurface(origin);
        RotateOrigin(origin);
        ApplyGravity(origin);
    }

    private void DetectStairSurface(Transform origin)
    {
        Vector3 rayOrigin = xrOrigin.Camera.transform.position;

        if (Physics.Raycast(
            rayOrigin,
            -currentUp,
            out RaycastHit hit,
            raycastDistance,
            stairLayer))
        {
            currentUp = hit.normal;
            currentGravity = -currentUp;
        }
        else
        {
            // Fuera de la escalera volvemos a la gravedad normal.
            currentUp = Vector3.up;
            currentGravity = Vector3.down;
        }
    }

    private void RotateOrigin(Transform origin)
    {
        Quaternion targetRotation =
            Quaternion.FromToRotation(origin.up, currentUp)
            * origin.rotation;

        origin.rotation = Quaternion.Slerp(
            origin.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void ApplyGravity(Transform origin)
    {
        origin.position += currentGravity * gravity * Time.deltaTime;
    }

    private void OnDrawGizmos()
    {
        if (xrOrigin == null || xrOrigin.Camera == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawRay(
            xrOrigin.Camera.transform.position,
            currentGravity * raycastDistance);
    }
}