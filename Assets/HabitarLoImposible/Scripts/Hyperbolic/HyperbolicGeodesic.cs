using UnityEngine;

/// <summary>
/// Calcula una geodésica en el disco de Poincaré como un diámetro o un arco de circunferencia ortogonal a su frontera.
/// </summary>
public class HyperbolicGeodesic : MonoBehaviour
{
    [Header("Puntos en el disco de Poincaré")]
    [Tooltip("Debe cumplir x² + y² < 1.")]
    [SerializeField]
    private Vector2 pointA = new Vector2(-0.35f, 0.25f);

    [Tooltip("Debe cumplir x² + y² < 1.")]
    [SerializeField]
    private Vector2 pointB = new Vector2(0.35f, 0.25f);

    private bool isDiameter;

    private Vector2 arcCenter;
    private float arcRadius;
    private float arcAngleStart;
    private float arcAngleDelta;

    public Vector2 PointA
    {
        get => pointA;
        set => pointA = value;
    }

    public Vector2 PointB
    {
        get => pointB;
        set => pointB = value;
    }

    public bool IsDiameterGeodesic => isDiameter;

    /// <summary>
    /// Valida los puntos y calcula los datos del diámetro o del arco que representa la geodésica entre ambos.
    /// </summary>
    public void CalculateGeodesic()
    {
        if (!IsInsideDisk(pointA) || !IsInsideDisk(pointB))
        {
            Debug.LogError("HyperbolicGeodesic: los puntos deben " + "estar dentro del disco de Poincaré.", this);

            isDiameter = true;
            ClearArcData();

            return;
        }

        if (Vector2.Distance(pointA, pointB) < 0.000001f)
        {
            Debug.LogError("HyperbolicGeodesic: los puntos de la " + "geodésica no pueden ser iguales.", this);

            isDiameter = true;
            ClearArcData();

            return;
        }

        isDiameter = IsDiameter(pointA, pointB);

        if (isDiameter)
        {
            ClearArcData();
            return;
        }

        if (!TryGetSupportingCircle(pointA, pointB, out arcCenter, out arcRadius))
        {
            isDiameter = true;
            ClearArcData();

            return;
        }

        float angleA = Mathf.Atan2(pointA.y - arcCenter.y, pointA.x - arcCenter.x);
        float angleB = Mathf.Atan2(pointB.y - arcCenter.y, pointB.x - arcCenter.x);

        arcAngleStart = angleA;
        arcAngleDelta = Mathf.DeltaAngle(angleA * Mathf.Rad2Deg, angleB * Mathf.Rad2Deg) * Mathf.Deg2Rad;
    }

    /// <summary>
    /// Comprueba si un punto pertenece al interior del disco unidad.
    /// </summary>
    public static bool IsInsideDisk(Vector2 point)
    {
        return point.sqrMagnitude < 1f;
    }

    /// <summary>
    /// Restablece a cero el centro, el radio y los ángulos del arco.
    /// </summary>
    private void ClearArcData()
    {
        arcCenter = Vector2.zero;
        arcRadius = 0f;
        arcAngleStart = 0f;
        arcAngleDelta = 0f;
    }

    /// <summary>
    /// Comprueba si los puntos y el origen están aproximadamente alineados, en cuyo caso la geodésica se representa como un diámetro.
    /// </summary>
    public static bool IsDiameter(Vector2 pointA, Vector2 pointB)
    {
        float determinant = pointA.x * pointB.y - pointA.y * pointB.x;

        return Mathf.Abs(determinant) < 0.0001f;
    }

    /// <summary>
    /// Calcula el centro y el radio de la circunferencia que pasa por ambos puntos y es ortogonal a la frontera del disco unidad.
    /// </summary>
    public static bool TryGetSupportingCircle(Vector2 pointA, Vector2 pointB, out Vector2 center, out float radius)
    {
        float determinant = pointA.x * pointB.y - pointA.y * pointB.x;
        float denominator = 2f * determinant;

        if (Mathf.Abs(denominator) < 0.000001f)
        {
            center = Vector2.zero;
            radius = 0f;

            return false;
        }

        float a = (pointA.sqrMagnitude + 1f) * pointB.y - (pointB.sqrMagnitude + 1f) * pointA.y;
        float b = (pointB.sqrMagnitude + 1f) * pointA.x - (pointA.sqrMagnitude + 1f) * pointB.x;

        center = new Vector2(a / denominator, b / denominator);
        radius = Vector2.Distance(center, pointA);

        return true;
    }

    /// <summary>
    /// Obtiene un punto de la geodésica para un parámetro limitado entre cero y uno, correspondientes a pointA y pointB.
    /// </summary>
    public Vector2 GetPointAt(float t)
    {
        t = Mathf.Clamp01(t);

        if (isDiameter)
        {
            return Vector2.Lerp(pointA, pointB, t);
        }

        float angle = arcAngleStart + arcAngleDelta * t;

        return arcCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arcRadius;
    }

    /// <summary>
    /// Obtiene la dirección tangente normalizada a la geodésica en el punto indicado, orientada de pointA hacia pointB.
    /// </summary>
    public Vector2 GetTangentAt(float t)
    {
        t = Mathf.Clamp01(t);

        if (isDiameter)
        {
            return (pointB - pointA).normalized;
        }

        float angle = arcAngleStart + arcAngleDelta * t;

        Vector2 radiusDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 tangent = new Vector2(-radiusDirection.y, radiusDirection.x);
        Vector2 directionAB = pointB - pointA;

        if (Vector2.Dot(tangent, directionAB) < 0f)
        {
            tangent = -tangent;
        }

        return tangent.normalized;
    }
}