using Unity.XR.CoreUtils;
using UnityEngine;

public class StairGravityOrientation : MonoBehaviour
{
    [SerializeField]
    private XROrigin xrOrigin;

    [SerializeField]
    private Transform stairs;

    [SerializeField]
    private float rotationSpeed = 30f;

    private Vector3 targetUp;
    private bool hasTargetUp;


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hit.collider.transform.IsChildOf(stairs))
            return;

        targetUp = hit.collider.transform.up;
        hasTargetUp = true;
    }


    private void LateUpdate()
    {
        if (!hasTargetUp)
            return;

        Transform origin = xrOrigin.transform;

        Quaternion targetRotation =
            Quaternion.FromToRotation(
                origin.up,
                targetUp
            ) * origin.rotation;

        origin.rotation = Quaternion.RotateTowards(
            origin.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}