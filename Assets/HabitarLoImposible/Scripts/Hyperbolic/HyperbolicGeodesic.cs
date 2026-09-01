using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HyperbolicGeodesic : MonoBehaviour
{
    [Header("Puntos en coordenadas hiperbólicas")]
    public Vector2 pointA = new Vector2(-1f, 1f);
    public Vector2 pointB = new Vector2(1f, 1f);

    [Header("Representación")]
    public float diskScale = 50f;

    [Header("Calidad del arco")]
    [Range(10, 200)]
    public int segments = 80;

    // Line Renderer
    private LineRenderer line;

    // Información geométrica de la geodésica
    private bool isDiameter;

    // Información del arco
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
        // --------------------------------------------------
        // 1. Crear los puntos en el hiperboloide
        // --------------------------------------------------

        HyperbolicPoint A =
            new HyperbolicPoint(
                pointA.x,
                pointA.y
            );

        HyperbolicPoint B =
            new HyperbolicPoint(
                pointB.x,
                pointB.y
            );


        // --------------------------------------------------
        // 2. Proyectarlos al disco de Poincaré
        // --------------------------------------------------

        Vector2 pA =
            A.ToPoincareDisk();

        Vector2 pB =
            B.ToPoincareDisk();


        // --------------------------------------------------
        // 3. Determinar si la geodésica es un diámetro
        // --------------------------------------------------

        float determinant =
            pA.x * pB.y -
            pA.y * pB.x;

        isDiameter =
            Mathf.Abs(determinant) < 0.0001f;


        // --------------------------------------------------
        // 4. Dibujar la geodésica
        // --------------------------------------------------

        if (isDiameter)
        {
            DrawDiameter(pA, pB);
        }
        else
        {
            DrawArc(pA, pB);
        }
    }


    // ======================================================
    // DIÁMETRO
    // ======================================================

    private void DrawDiameter(Vector2 pA, Vector2 pB)
    {
        line.positionCount = 2;

        line.SetPosition(
            0,
            ToUnityPosition(pA)
        );

        line.SetPosition(
            1,
            ToUnityPosition(pB)
        );
    }


    // ======================================================
    // ARCO DE CIRCUNFERENCIA
    // ======================================================

    private void DrawArc(Vector2 pA, Vector2 pB)
    {
        // --------------------------------------------------
        // 1. Calcular el centro de la circunferencia
        // --------------------------------------------------

        float denominator =
            2f *
            (
                pA.x * pB.y -
                pA.y * pB.x
            );


        // Si por algún motivo los puntos están prácticamente
        // alineados con el origen, utilizamos un diámetro.

        if (Mathf.Abs(denominator) < 0.000001f)
        {
            isDiameter = true;

            DrawDiameter(pA, pB);

            return;
        }


        float a =
            (pA.sqrMagnitude + 1f) * pB.y -
            (pB.sqrMagnitude + 1f) * pA.y;


        float b =
            (pB.sqrMagnitude + 1f) * pA.x -
            (pA.sqrMagnitude + 1f) * pB.x;


        Vector2 center =
            new Vector2(
                a / denominator,
                b / denominator
            );


        // --------------------------------------------------
        // 2. Calcular el radio
        // --------------------------------------------------

        float radius =
            Vector2.Distance(
                center,
                pA
            );


        // Guardamos la información para poder utilizarla
        // posteriormente para calcular tangentes.

        arcCenter = center;
        arcRadius = radius;


        // --------------------------------------------------
        // 3. Calcular los ángulos de A y B
        // --------------------------------------------------

        float angleA =
            Mathf.Atan2(
                pA.y - center.y,
                pA.x - center.x
            );


        float angleB =
            Mathf.Atan2(
                pB.y - center.y,
                pB.x - center.x
            );


        // --------------------------------------------------
        // 4. Calcular el arco menor entre A y B
        // --------------------------------------------------

        float deltaAngle =
            Mathf.DeltaAngle(
                angleA * Mathf.Rad2Deg,
                angleB * Mathf.Rad2Deg
            ) * Mathf.Deg2Rad;


        // Guardamos los datos del arco.

        arcAngleStart = angleA;
        arcAngleDelta = deltaAngle;


        // --------------------------------------------------
        // 5. Dibujar el arco
        // --------------------------------------------------

        line.positionCount =
            segments + 1;


        for (int i = 0; i <= segments; i++)
        {
            float t =
                i / (float)segments;


            float angle =
                angleA +
                deltaAngle * t;


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
    // CONVERSIÓN POINCARÉ → UNITY
    // ======================================================

    private Vector3 ToUnityPosition(Vector2 point)
    {
        return new Vector3(
            point.x * diskScale,
            0.5f,
            point.y * diskScale
        );
    }


    // ======================================================
    // OBTENER PUNTO EN EL DISCO
    // ======================================================

    public Vector2 GetPoincarePoint(
        Vector2 hyperbolicPoint
    )
    {
        HyperbolicPoint point =
            new HyperbolicPoint(
                hyperbolicPoint.x,
                hyperbolicPoint.y
            );

        return point.ToPoincareDisk();
    }


    // ======================================================
    // TANGENTE REAL DE LA GEODÉSICA
    // ======================================================

    public Vector2 GetTangentAt(float t)
    {
        // --------------------------------------------------
        // CASO 1: DIÁMETRO
        // --------------------------------------------------

        if (isDiameter)
        {
            Vector2 direction =
                GetPoincarePoint(pointB) -
                GetPoincarePoint(pointA);

            return direction.normalized;
        }


        // --------------------------------------------------
        // CASO 2: ARCO
        // --------------------------------------------------

        float angle =
            arcAngleStart +
            arcAngleDelta * t;


        // Vector desde el centro hasta el punto.

        Vector2 radiusDirection =
            new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            );


        // La tangente es perpendicular al radio.

        Vector2 tangent =
            new Vector2(
                -radiusDirection.y,
                radiusDirection.x
            );


        // --------------------------------------------------
        // Asegurar que apunta de A hacia B
        // --------------------------------------------------

        Vector2 directionAB =
            GetPoincarePoint(pointB) -
            GetPoincarePoint(pointA);


        if (Vector2.Dot(
                tangent,
                directionAB
            ) < 0f)
        {
            tangent = -tangent;
        }


        return tangent.normalized;
    }


    // ======================================================
    // OBTENER POSICIÓN EN LA GEODÉSICA
    // ======================================================

    public Vector2 GetPointAt(float t)
    {
        t = Mathf.Clamp01(t);


        // --------------------------------------------------
        // DIÁMETRO
        // --------------------------------------------------

        if (isDiameter)
        {
            Vector2 A =
                GetPoincarePoint(pointA);

            Vector2 B =
                GetPoincarePoint(pointB);

            return Vector2.Lerp(
                A,
                B,
                t
            );
        }


        // --------------------------------------------------
        // ARCO
        // --------------------------------------------------

        float angle =
            arcAngleStart +
            arcAngleDelta * t;


        return arcCenter +
               new Vector2(
                   Mathf.Cos(angle),
                   Mathf.Sin(angle)
               ) *
               arcRadius;
    }
}