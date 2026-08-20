using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class StairGravity : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CharacterController characterController;

    [Header("Detección")]
    [SerializeField] private LayerMask stairMask;
    [SerializeField] private float raycastDistance = 2f;

    [Header("Gravedad")]
    [SerializeField] private float gravityStrength = 9.81f;

    private Vector3 surfaceNormal = Vector3.up;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        DetectSurface();
        ApplyGravity();
    }

    private void DetectSurface()
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
            surfaceNormal = hit.normal;
        }
        else
        {
            surfaceNormal = transform.up;
        }
    }

    private void ApplyGravity()
    {
        Vector3 gravityDirection = -surfaceNormal;

        characterController.Move(
            gravityDirection *
            gravityStrength *
            Time.deltaTime
        );
    }

    private void OnDrawGizmos()
    {
        if (characterController == null)
            return;

        Vector3 origin = characterController.bounds.center;

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            origin,
            origin - surfaceNormal * raycastDistance
        );
    }
}