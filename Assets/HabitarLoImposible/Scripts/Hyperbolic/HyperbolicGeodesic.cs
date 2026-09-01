using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HyperbolicGeodesic : MonoBehaviour
{
    [Header("Puntos en coordenadas del hiperboloide")]
    public Vector2 pointA = new Vector2(-1f, 1f);
    public Vector2 pointB = new Vector2(1f, 1f);

    [Header("Representación")]
    public float diskScale = 50f;

    [Header("Calidad del arco")]
    [Range(10, 200)]
    public int segments = 80;

    private LineRenderer line;

    private bool isDiameter;

    private Vector2 arcCenter;
    private float arcRadius;
    private float arcAngleStart;
    private float arcAngleDelta;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        DrawGeodesic();
    }

    public void DrawGeodesic()
    {
        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }

        Vector2 poincareA = GetPoincarePoint(pointA);
        Vector2 poincareB = GetPoincarePoint(pointB);

        isDiameter = IsDiameter(poincareA, poincareB);

        if (isDiameter)
        {
            DrawDiameter(poincareA, poincareB);
        }
        else
        {
            DrawArc(poincareA, poincareB);
        }
    }

    // ======================================================
    // CÁLCULO GEOMÉTRICO
    // ======================================================

    public static bool IsDiameter(Vector2 pointA, Vector2 pointB)
    {
        float determinant =
            pointA.x * pointB.y -
            pointA.y * pointB.x;

        return Mathf.Abs(determinant) < 0.0001f;
    }

    public static bool TryGetSupportingCircle(
        Vector2 pointA,
        Vector2 pointB,
        out Vector2 center,
        out float radius
    )
    {
        float determinant =
            pointA.x * pointB.y -
            pointA.y * pointB.x;

        float denominator = 2f * determinant;

        if (Mathf.Abs(denominator) < 0.000001f)
        {
            center = Vector2.zero;
            radius = 0f;

            return false;
        }

        float a =
            (pointA.sqrMagnitude + 1f) * pointB.y -
            (pointB.sqrMagnitude + 1f) * pointA.y;

        float b =
            (pointB.sqrMagnitude + 1f) * pointA.x -
            (pointA.sqrMagnitude + 1f) * pointB.x;

        center = new Vector2(
            a / denominator,
            b / denominator
        );

        radius = Vector2.Distance(center, pointA);

        return true;
    }

    // ======================================================
    // DIBUJO
    // ======================================================

    private void DrawDiameter(Vector2 pointA, Vector2 pointB)
    {
        line.positionCount = 2;

        line.SetPosition(0, ToUnityPosition(pointA));
        line.SetPosition(1, ToUnityPosition(pointB));
    }

    private void DrawArc(Vector2 pointA, Vector2 pointB)
    {
        if (!TryGetSupportingCircle(
                pointA,
                pointB,
                out Vector2 center,
                out float radius
            ))
        {
            isDiameter = true;
            DrawDiameter(pointA, pointB);

            return;
        }

        arcCenter = center;
        arcRadius = radius;

        float angleA = Mathf.Atan2(
            pointA.y - center.y,
            pointA.x - center.x
        );

        float angleB = Mathf.Atan2(
            pointB.y - center.y,
            pointB.x - center.x
        );

        float deltaAngle = Mathf.DeltaAngle(
            angleA * Mathf.Rad2Deg,
            angleB * Mathf.Rad2Deg
        ) * Mathf.Deg2Rad;

        arcAngleStart = angleA;
        arcAngleDelta = deltaAngle;

        line.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = angleA + deltaAngle * t;

            Vector2 point =
                center +
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * radius;

            line.SetPosition(
                i,
                ToUnityPosition(point)
            );
        }
    }

    // ======================================================
    // CONVERSIONES Y CONSULTAS
    // ======================================================

    private Vector3 ToUnityPosition(Vector2 point)
    {
        return new Vector3(
            point.x * diskScale,
            0.5f,
            point.y * diskScale
        );
    }

    public Vector2 GetPoincarePoint(Vector2 hyperbolicPoint)
    {
        HyperbolicPoint point =
            new HyperbolicPoint(
                hyperbolicPoint.x,
                hyperbolicPoint.y
            );

        return point.ToPoincareDisk();
    }

    public Vector2 GetTangentAt(float t)
    {
        t = Mathf.Clamp01(t);

        if (isDiameter)
        {
            Vector2 direction =
                GetPoincarePoint(pointB) -
                GetPoincarePoint(pointA);

            return direction.normalized;
        }

        float angle =
            arcAngleStart +
            arcAngleDelta * t;

        Vector2 radiusDirection =
            new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            );

        Vector2 tangent =
            new Vector2(
                -radiusDirection.y,
                radiusDirection.x
            );

        Vector2 directionAB =
            GetPoincarePoint(pointB) -
            GetPoincarePoint(pointA);

        if (Vector2.Dot(tangent, directionAB) < 0f)
        {
            tangent = -tangent;
        }

        return tangent.normalized;
    }

    public Vector2 GetPointAt(float t)
    {
        t = Mathf.Clamp01(t);

        if (isDiameter)
        {
            Vector2 poincareA =
                GetPoincarePoint(pointA);

            Vector2 poincareB =
                GetPoincarePoint(pointB);

            return Vector2.Lerp(
                poincareA,
                poincareB,
                t
            );
        }

        float angle =
            arcAngleStart +
            arcAngleDelta * t;

        return arcCenter +
               new Vector2(
                   Mathf.Cos(angle),
                   Mathf.Sin(angle)
               ) * arcRadius;
    }
}