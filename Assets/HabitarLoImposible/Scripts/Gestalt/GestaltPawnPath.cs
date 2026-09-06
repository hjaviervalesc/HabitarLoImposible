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
            OriginalWorldPosition =
                originalWorldPosition;
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

        ClearPath();

        Vector3 start =
            pathStart.position;

        Vector3 end =
            houseEntrance.position;

        Vector3 pathDirection =
            end - start;

        // El paseo se distribuye sobre el plano XZ.
        pathDirection.y = 0f;

        if (pathDirection.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning(
                "El inicio y el final del paseo coinciden.",
                this
            );

            return;
        }

        pathDirection.Normalize();

        Vector3 lateralDirection =
            new Vector3(
                -pathDirection.z,
                0f,
                pathDirection.x
            );

        for (int i = 0;
             i < pawnPairs;
             i++)
        {
            float normalizedIndex =
                (float)i / (pawnPairs - 1);

            float pathProgress =
                1f - Mathf.Pow(
                    1f - normalizedIndex,
                    spacingExponent
                );

            Vector3 pathPosition =
                Vector3.Lerp(
                    start,
                    end,
                    pathProgress
                );

            /*
             * Todos comienzan sobre la altura inicial
             * del tablero. La deformación se añade después.
             */
            pathPosition.y =
                start.y + verticalOffset;

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
                lateralDirection *
                currentWidth *
                0.5f;

            Vector3 rightPosition =
                pathPosition +
                lateralDirection *
                currentWidth *
                0.5f;

            CreatePawn(
                leftPosition,
                currentScale,
                pathDirection
            );

            CreatePawn(
                rightPosition,
                currentScale,
                pathDirection
            );
        }

        UpdatePawnPositions();
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
                Quaternion.LookRotation(direction),
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