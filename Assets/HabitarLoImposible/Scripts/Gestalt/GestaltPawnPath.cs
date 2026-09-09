using System.Collections.Generic;
using UnityEngine;

public class GestaltPawnPath : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private Transform pathStart;

    [SerializeField]
    private Transform houseEntrance;

    [SerializeField]
    private GameObject pawnPrefab;

    [SerializeField]
    private DeformationSectionManager deformationManager;

    [Header("Cantidad")]
    [Min(2)]
    [SerializeField]
    private int pawnPairs = 12;

    [Header("Anchura del paseo")]
    [SerializeField]
    private float initialWidth = 6f;

    [SerializeField]
    private float finalWidth = 2f;

    [Header("Curvatura")]
    [Tooltip(
        "Desplazamiento lateral máximo del paseo. " +
        "Usa 0 para obtener un camino recto."
    )]
    [SerializeField]
    private float curveAmount = 1.5f;

    [Tooltip(
        "Cantidad de cambios de dirección de la curva. " +
        "Valores entre 1 y 2 suelen producir un resultado suave."
    )]
    [Min(0.1f)]
    [SerializeField]
    private float curveWaves = 1.5f;

    [Tooltip(
        "Permite desplazar los puntos en los que aparecen " +
        "las curvas sin mover los extremos del recorrido."
    )]
    [SerializeField]
    private float curvePhase = 0f;

    [Header("Escala")]
    [SerializeField]
    private float initialScale = 1f;

    [SerializeField]
    private float finalScale = 1f;

    [Header("Distribución")]
    [Tooltip(
        "Un valor mayor que 1 aproxima progresivamente " +
        "los peones a la casa."
    )]
    [Min(0.1f)]
    [SerializeField]
    private float spacingExponent = 1.5f;

    [Header("Altura")]
    [Tooltip(
        "Separación vertical entre la base del peón " +
        "y la superficie del tablero."
    )]
    [SerializeField]
    private float verticalOffset = 0f;

    [Header("Generación")]
    [SerializeField]
    private bool generateOnStart = true;

    private readonly List<PawnData> generatedPawns =
        new List<PawnData>();

    private class PawnData
    {
        public Transform PawnTransform;
        public Vector3 OriginalWorldPosition;

        public PawnData(
            Transform pawnTransform,
            Vector3 originalWorldPosition)
        {
            PawnTransform = pawnTransform;
            OriginalWorldPosition = originalWorldPosition;
        }
    }

    private void Start()
    {
        if (generateOnStart)
        {
            GeneratePath();
        }
        else
        {
            RegisterExistingPawns();
        }
    }

    /*
     * LateUpdate se ejecuta después de que el manager
     * haya actualizado la deformación del tablero.
     */
    private void LateUpdate()
    {
        UpdatePawnPositions();
    }

    [ContextMenu("Generate Path")]
    public void GeneratePath()
    {
        if (pathStart == null ||
            houseEntrance == null ||
            pawnPrefab == null)
        {
            Debug.LogWarning(
                "Faltan referencias en GestaltPawnPath.",
                this
            );

            return;
        }

        Vector3 start = pathStart.position;
        Vector3 end = houseEntrance.position;

        Vector3 straightDirection = end - start;

        // El paseo se distribuye sobre el plano XZ.
        straightDirection.y = 0f;

        if (straightDirection.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning(
                "El inicio y el final del paseo coinciden.",
                this
            );

            return;
        }

        ClearPath();

        straightDirection.Normalize();

        Vector3 baseLateralDirection =
            new Vector3(
                -straightDirection.z,
                0f,
                straightDirection.x
            );

        for (int i = 0; i < pawnPairs; i++)
        {
            float normalizedIndex =
                (float)i / (pawnPairs - 1);

            /*
             * Modifica la separación entre parejas.
             * Con valores mayores que 1, los peones se
             * concentran progresivamente cerca de la casa.
             */
            float pathProgress =
                1f - Mathf.Pow(
                    1f - normalizedIndex,
                    spacingExponent
                );

            Vector3 pathPosition =
                GetCurvedPathPosition(
                    start,
                    end,
                    baseLateralDirection,
                    pathProgress
                );

            /*
             * Calculamos la dirección local de la curva.
             * Así cada pareja sigue visualmente el recorrido
             * en lugar de mirar siempre en línea recta.
             */
            Vector3 localPathDirection =
                GetCurvedPathDirection(
                    start,
                    end,
                    baseLateralDirection,
                    pathProgress
                );

            Vector3 localLateralDirection =
                new Vector3(
                    -localPathDirection.z,
                    0f,
                    localPathDirection.x
                ).normalized;

            float currentWidth =
                Mathf.Lerp(
                    initialWidth,
                    finalWidth,
                    pathProgress
                );

            float currentScale =
                Mathf.Lerp(
                    initialScale,
                    finalScale,
                    pathProgress
                );

            Vector3 leftPosition =
                pathPosition -
                localLateralDirection *
                currentWidth *
                0.5f;

            Vector3 rightPosition =
                pathPosition +
                localLateralDirection *
                currentWidth *
                0.5f;

            CreatePawn(
                leftPosition,
                currentScale,
                localPathDirection
            );

            CreatePawn(
                rightPosition,
                currentScale,
                localPathDirection
            );
        }

        UpdatePawnPositions();
    }

    /// <summary>
    /// Obtiene una posición sobre el recorrido curvado.
    /// La envolvente hace que la curvatura sea cero
    /// tanto al principio como al final.
    /// </summary>
    private Vector3 GetCurvedPathPosition(
        Vector3 start,
        Vector3 end,
        Vector3 lateralDirection,
        float progress)
    {
        progress = Mathf.Clamp01(progress);

        Vector3 position =
            Vector3.Lerp(start, end, progress);

        /*
         * La envolvente vale 0 en ambos extremos y 1
         * aproximadamente en el centro del recorrido.
         */
        float curveEnvelope =
            Mathf.Sin(progress * Mathf.PI);

        float curveOscillation =
            Mathf.Sin(
                progress *
                Mathf.PI *
                2f *
                curveWaves +
                curvePhase
            );

        float lateralOffset =
            curveAmount *
            curveEnvelope *
            curveOscillation;

        position +=
            lateralDirection *
            lateralOffset;

        /*
         * Los peones parten de la altura inicial.
         * La deformación del tablero se añade después.
         */
        position.y =
            start.y + verticalOffset;

        return position;
    }

    /// <summary>
    /// Calcula la dirección local de la curva comparando
    /// dos posiciones muy próximas del recorrido.
    /// </summary>
    private Vector3 GetCurvedPathDirection(
        Vector3 start,
        Vector3 end,
        Vector3 lateralDirection,
        float progress)
    {
        const float sampleDistance = 0.01f;

        float previousProgress =
            Mathf.Clamp01(
                progress - sampleDistance
            );

        float nextProgress =
            Mathf.Clamp01(
                progress + sampleDistance
            );

        Vector3 previousPosition =
            GetCurvedPathPosition(
                start,
                end,
                lateralDirection,
                previousProgress
            );

        Vector3 nextPosition =
            GetCurvedPathPosition(
                start,
                end,
                lateralDirection,
                nextProgress
            );

        Vector3 direction =
            nextPosition - previousPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = end - start;
            direction.y = 0f;
        }

        return direction.normalized;
    }

    private void CreatePawn(
        Vector3 position,
        float scale,
        Vector3 direction)
    {
        GameObject pawn =
            Instantiate(
                pawnPrefab,
                position,
                Quaternion.LookRotation(
                    direction,
                    Vector3.up
                ),
                transform
            );

        pawn.transform.localScale =
            pawnPrefab.transform.localScale *
            scale;

        PawnData pawnData =
            new PawnData(
                pawn.transform,
                position
            );

        generatedPawns.Add(pawnData);
    }

    /// <summary>
    /// Coloca cada peón en su posición inicial más
    /// la deformación actual del tablero.
    /// </summary>
    private void UpdatePawnPositions()
    {
        if (deformationManager == null)
        {
            return;
        }

        for (int i = generatedPawns.Count - 1;
             i >= 0;
             i--)
        {
            PawnData pawnData =
                generatedPawns[i];

            if (pawnData.PawnTransform == null)
            {
                generatedPawns.RemoveAt(i);
                continue;
            }

            Vector3 deformation =
                deformationManager
                    .GetWorldDeformationAtPosition(
                        pawnData.OriginalWorldPosition
                    );

            pawnData.PawnTransform.position =
                pawnData.OriginalWorldPosition +
                deformation;
        }
    }

    /// <summary>
    /// Registra los peones que ya sean hijos
    /// de este objeto.
    /// </summary>
    private void RegisterExistingPawns()
    {
        generatedPawns.Clear();

        for (int i = 0;
             i < transform.childCount;
             i++)
        {
            Transform pawn =
                transform.GetChild(i);

            PawnData pawnData =
                new PawnData(
                    pawn,
                    pawn.position
                );

            generatedPawns.Add(pawnData);
        }
    }

    [ContextMenu("Clear Path")]
    public void ClearPath()
    {
        generatedPawns.Clear();

        for (int i = transform.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject child =
                transform.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }
}