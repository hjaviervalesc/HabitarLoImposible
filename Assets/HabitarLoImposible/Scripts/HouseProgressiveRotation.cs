using UnityEngine;

public class HouseProgressiveRotation : MonoBehaviour
{
    [Header("REFERENCIAS")]
    public Transform houseRoot;
    public Transform xrOrigin;

    [Header("PADRES DE LAS ESCALERAS")]
    public Transform stairs1;
    public Transform stairs2;

    [Header("ESCALERA 1")]
    public Transform stair1Start;
    public Transform stair1End;

    [Header("ESCALERA 2")]
    public Transform stair2Start;
    public Transform stair2End;

    [Header("GIRO")]
    public float stair1Rotation = 90f;
    public float stair2Rotation = 90f;

    [Header("SUAVIZADO")]
    public float smoothSpeed = 5f;

    [Header("ANCHURA DE DETECCIÓN")]
    public float stairWidth = 2f;

    // ---------------------------------------------------------
    // ROTACIONES
    // ---------------------------------------------------------

    private Quaternion initialRotation;

    private Quaternion stair1EndRotation;
    private Quaternion stair2EndRotation;

    private Quaternion targetRotation;

    private bool stair1Finished = false;
    private bool stair2Started = false;

    void Start()
    {
        if (houseRoot == null)
            return;

        // -----------------------------------------------------
        // GUARDAMOS EXACTAMENTE LA ROTACIÓN INICIAL
        // -----------------------------------------------------

        initialRotation = houseRoot.localRotation;

        // -----------------------------------------------------
        // ESCALERA 1
        //
        // Giramos 90 grados respecto a la posición inicial.
        // -----------------------------------------------------

        stair1EndRotation =
            initialRotation *
            Quaternion.Euler(stair1Rotation, 0f, 0f);

        // -----------------------------------------------------
        // ESCALERA 2
        //
        // Otros 90 grados respecto al final de la escalera 1.
        // -----------------------------------------------------

        stair2EndRotation =
            stair1EndRotation *
            Quaternion.Euler(stair2Rotation, 0f, 0f);

        targetRotation = initialRotation;

        Debug.Log(
            "ROTACIÓN INICIAL: " +
            initialRotation.eulerAngles.x
        );

        Debug.Log(
            "FINAL ESCALERA 1: " +
            stair1EndRotation.eulerAngles.x
        );

        Debug.Log(
            "FINAL ESCALERA 2: " +
            stair2EndRotation.eulerAngles.x
        );
    }

    void Update()
    {
        if (houseRoot == null || xrOrigin == null)
            return;

        // =====================================================
        // ESCALERA 1
        // =====================================================

        float progress1;

        if (!stair1Finished &&
            GetStairProgress(
                stairs1,
                stair1Start,
                stair1End,
                out progress1))
        {
            targetRotation =
                Quaternion.Slerp(
                    initialRotation,
                    stair1EndRotation,
                    progress1
                );

            Debug.Log(
                "ESCALERA 1 | Progreso: " +
                progress1 +
                " | Rotación: " +
                targetRotation.eulerAngles.x
            );

            // -------------------------------------------------
            // Cuando llega al final de la escalera 1
            // bloqueamos esta fase.
            // -------------------------------------------------

            if (progress1 >= 0.999f)
            {
                stair1Finished = true;

                targetRotation =
                    stair1EndRotation;

                Debug.Log(
                    "FIN ESCALERA 1 | Rotación: " +
                    stair1EndRotation.eulerAngles.x
                );
            }
        }

        // =====================================================
        // ESCALERA 2
        // =====================================================

        float progress2;

        if (GetStairProgress(
            stairs2,
            stair2Start,
            stair2End,
            out progress2))
        {
            if (!stair2Started)
            {
                stair2Started = true;

                Debug.Log(
                    "INICIO ESCALERA 2 | Rotación actual: " +
                    houseRoot.localRotation.eulerAngles.x
                );
            }

            // -------------------------------------------------
            // CURVA DEL PROGRESO
            //
            // Hace que la rotación sea más lenta al principio
            // y más rápida al final.
            // -------------------------------------------------

            float adjustedProgress2 =
                Mathf.Pow(progress2, 1.5f);

            targetRotation =
                Quaternion.Slerp(
                    stair1EndRotation,
                    stair2EndRotation,
                    adjustedProgress2
                );

            Debug.Log(
                "ESCALERA 2 | Progreso: " +
                progress2 +
                " | Progreso ajustado: " +
                adjustedProgress2 +
                " | Rotación: " +
                targetRotation.eulerAngles.x
            );
        }
        // =====================================================
        // ROTACIÓN SUAVE
        // =====================================================

        houseRoot.localRotation =
            Quaternion.Slerp(
                houseRoot.localRotation,
                targetRotation,
                Time.deltaTime * smoothSpeed
            );
    }

    // =========================================================
    // CALCULA EL PROGRESO DENTRO DE UNA ESCALERA
    // =========================================================

    bool GetStairProgress(
        Transform stairParent,
        Transform start,
        Transform end,
        out float progress)
    {
        progress = 0f;

        if (stairParent == null ||
            start == null ||
            end == null)
        {
            return false;
        }

        // -----------------------------------------------------
        // ESPACIO LOCAL DE LA ESCALERA
        // -----------------------------------------------------

        Vector3 playerLocal =
            stairParent.InverseTransformPoint(
                xrOrigin.position
            );

        Vector3 startLocal =
            stairParent.InverseTransformPoint(
                start.position
            );

        Vector3 endLocal =
            stairParent.InverseTransformPoint(
                end.position
            );

        // -----------------------------------------------------
        // DIRECCIÓN
        // -----------------------------------------------------

        Vector3 direction =
            endLocal - startLocal;

        float length =
            direction.magnitude;

        if (length < 0.001f)
            return false;

        Vector3 normalizedDirection =
            direction.normalized;

        // -----------------------------------------------------
        // DISTANCIA DESDE START
        // -----------------------------------------------------

        float distance =
            Vector3.Dot(
                playerLocal - startLocal,
                normalizedDirection
            );

        // -----------------------------------------------------
        // PUNTO MÁS CERCANO
        // -----------------------------------------------------

        Vector3 closestPoint =
            startLocal +
            normalizedDirection * distance;

        float perpendicularDistance =
            Vector3.Distance(
                playerLocal,
                closestPoint
            );

        // -----------------------------------------------------
        // COMPROBAR SI ESTÁ EN LA ESCALERA
        // -----------------------------------------------------

        if (distance < 0f ||
            distance > length ||
            perpendicularDistance > stairWidth)
        {
            return false;
        }

        // -----------------------------------------------------
        // PROGRESO 0 → 1
        // -----------------------------------------------------

        progress =
            Mathf.Clamp01(
                distance / length
            );

        return true;
    }
}