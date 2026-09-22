using UnityEngine;

/// <summary>
/// Hace que el cuerpo del avatar siga la posición y la orientación
/// horizontal de la cámara del visor XR.
/// </summary>
public class AvatarBodyFollower : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private Transform avatarBody;

    [Header("Posición")]
    [SerializeField] private float bodyBackwardOffset = 0.2f;
    [SerializeField] private float followSpeed = 15f;

    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 10f;

    private float initialLocalHeight;

    /// <summary>
    /// Almacena la altura local inicial del cuerpo.
    /// </summary>
    private void Start()
    {
        initialLocalHeight = avatarBody.localPosition.y;
    }

    /// <summary>
    /// Actualiza la posición y la rotación del cuerpo después
    /// del movimiento de la cámara.
    /// </summary>
    private void LateUpdate()
    {
        if (xrCamera == null || avatarBody == null)
        {
            return;
        }

        Transform bodyParent = avatarBody.parent;

        Vector3 cameraForward = bodyParent.InverseTransformDirection(xrCamera.forward);
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.001f)
        {
            return;
        }

        cameraForward.Normalize();

        Vector3 cameraLocalPosition = bodyParent.InverseTransformPoint(xrCamera.position);
        Vector3 targetPosition = cameraLocalPosition - cameraForward * bodyBackwardOffset;
        targetPosition.y = initialLocalHeight;

        avatarBody.localPosition = Vector3.Lerp(avatarBody.localPosition, targetPosition, followSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
        avatarBody.localRotation = Quaternion.Slerp(avatarBody.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}