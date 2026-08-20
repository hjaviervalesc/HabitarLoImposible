using UnityEngine;

public class StaircaseTrajectory : MonoBehaviour
{
    [Header("Trajectory")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float stepDepth = 0.35f;
    [SerializeField] private float verticalOffset = 5f;

    public Vector3 GetPosition(int stepIndex)
    {
        float distance = stepIndex * stepDepth;

        float angle = distance / radius;

        float x = Mathf.Sin(angle) * radius;

        float y = verticalOffset + (1f - Mathf.Cos(angle)) * radius;

        float z = 0f;

        return new Vector3(x, y, z);
    }

    public Quaternion GetRotation(int stepIndex)
    {
        float distance = stepIndex * stepDepth;

        float angle = distance / radius;

        return Quaternion.Euler(
            0f,
            0f,
            -angle * Mathf.Rad2Deg
        );
    }

    public int GetClosestStepIndex(Vector3 worldPosition)
    {
        Vector3 localPosition =
            transform.InverseTransformPoint(worldPosition);

        float angle =
            Mathf.Atan2(
                localPosition.x,
                radius - localPosition.y + verticalOffset
            );

        float distance = angle * radius;

        int stepIndex =
            Mathf.RoundToInt(distance / stepDepth);

        return Mathf.Max(0, stepIndex);
    }
}