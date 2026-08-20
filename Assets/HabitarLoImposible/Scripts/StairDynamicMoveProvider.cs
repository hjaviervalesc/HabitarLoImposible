using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class StairDynamicMoveProvider : DynamicMoveProvider
{
    [Header("Stair Movement")]
    [SerializeField] private LayerMask stairMask;
    [SerializeField] private float raycastDistance = 2f;

    private Vector3 stairNormal = Vector3.up;
    private bool onStair;

    protected override Vector3 ComputeDesiredMove(Vector2 input)
    {
        // Primero dejamos que DynamicMoveProvider
        // calcule el movimiento normal.
        Vector3 desiredMove = base.ComputeDesiredMove(input);

        // Comprobamos si estamos sobre la escalera.
        DetectStair();

        // Si no estamos en una escalera,
        // mantenemos el movimiento original.
        if (!onStair)
            return desiredMove;

        // Proyectamos el movimiento sobre
        // la superficie de la escalera.
        Vector3 stairMove =
            Vector3.ProjectOnPlane(
                desiredMove,
                stairNormal
            );

        return stairMove;
    }

    private void DetectStair()
    {
        XROrigin xrOrigin = mediator.xrOrigin;

        if (xrOrigin == null)
        {
            onStair = false;
            stairNormal = Vector3.up;
            return;
        }

        // Origin es GameObject en tu versión de XRI.
        Transform originTransform =
            xrOrigin.Origin.transform;

        Vector3 rayOrigin =
            originTransform.position +
            originTransform.up * 0.2f;

        if (Physics.Raycast(
            rayOrigin,
            -originTransform.up,
            out RaycastHit hit,
            raycastDistance,
            stairMask,
            QueryTriggerInteraction.Ignore))
        {
            onStair = true;
            stairNormal = hit.normal;
        }
        else
        {
            onStair = false;
            stairNormal = Vector3.up;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (mediator == null ||
            mediator.xrOrigin == null)
            return;

        Transform originTransform =
            mediator.xrOrigin.Origin.transform;

        Vector3 origin =
            originTransform.position +
            originTransform.up * 0.2f;

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            origin,
            origin -
            originTransform.up *
            raycastDistance
        );

        if (onStair)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawLine(
                origin,
                origin + stairNormal
            );
        }
    }
}