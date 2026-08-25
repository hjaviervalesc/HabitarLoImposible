using UnityEngine;

public class HouseProgressiveRotation : MonoBehaviour
{
    [Header("Referencias")]
    public Transform houseRoot;
    public Transform xrOrigin;

    [Header("Primera escalera")]
    public Transform stair1Start;
    public Transform stair1End;

    [Header("Segunda escalera")]
    public Transform stair2Start;
    public Transform stair2End;

    [Header("Rotaciones")]
    public float rotation1Start = -180f;
    public float rotation1End = -90f;

    public float rotation2Start = -90f;
    public float rotation2End = 0f;

    [Header("Suavizado")]
    public float smoothSpeed = 5f;

    private float targetRotation;

    void Start()
    {
        targetRotation = rotation1Start;

        Vector3 euler = houseRoot.localEulerAngles;
        euler.z = rotation1Start;
        houseRoot.localEulerAngles = euler;
    }

    void Update()
    {
        float target = targetRotation;

        // -----------------------------
        // ESCALERA 1
        // -----------------------------

        float progress1 = GetProgress(
            xrOrigin.position,
            stair1Start.position,
            stair1End.position
        );

        if (progress1 > 0f && progress1 < 1f)
        {
            target = Mathf.Lerp(
                rotation1Start,
                rotation1End,
                progress1
            );
        }

        // -----------------------------
        // ESCALERA 2
        // -----------------------------

        float progress2 = GetProgress(
            xrOrigin.position,
            stair2Start.position,
            stair2End.position
        );

        if (progress2 > 0f && progress2 < 1f)
        {
            target = Mathf.Lerp(
                rotation2Start,
                rotation2End,
                progress2
            );
        }

        targetRotation = target;

        // -----------------------------
        // ROTACIÓN SUAVE
        // -----------------------------

        Vector3 currentEuler = houseRoot.localEulerAngles;

        float newZ = Mathf.LerpAngle(
            currentEuler.z,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );

        houseRoot.localEulerAngles = new Vector3(
            currentEuler.x,
            currentEuler.y,
            newZ
        );
    }

    float GetProgress(Vector3 playerPosition, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;

        float length = direction.magnitude;

        if (length < 0.001f)
            return 0f;

        Vector3 normalizedDirection = direction / length;

        float distance = Vector3.Dot(
            playerPosition - start,
            normalizedDirection
        );

        return Mathf.Clamp01(distance / length);
    }
}