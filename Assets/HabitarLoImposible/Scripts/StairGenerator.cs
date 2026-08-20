using UnityEngine;

public class StairGenerator : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject cubePrefab;

    [Header("Escalera")]
    [SerializeField] private int numberOfSteps = 30;
    [SerializeField] private float stepWidth = 2f;
    [SerializeField] private float stepHeight = 0.20f;
    [SerializeField] private float stepDepth = 0.40f;

    [Header("Inclinación")]
    [SerializeField] private float stairAngle = 25f;

    [Header("Separación")]
    [SerializeField] private float gap = 0f;

    [Header("Generación")]
    [SerializeField] private bool generateOnStart = true;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateStair();
        }
    }

    public void GenerateStair()
    {
        ClearStair();

        if (cubePrefab == null)
        {
            Debug.LogError(
                "StairGenerator: No hay Cube Prefab asignado."
            );

            return;
        }

        /*
         * Creamos un punto de referencia para la escalera.
         * Los escalones se generan siguiendo este sistema.
         */
        Quaternion stairRotation =
            Quaternion.Euler(stairAngle, 0f, 0f);

        for (int i = 0; i < numberOfSteps; i++)
        {
            GameObject step =
                Instantiate(cubePrefab, transform);

            step.name = "Step_" + i;

            /*
             * Posición local del escalón.
             *
             * X = anchura
             * Y = altura
             * Z = profundidad
             */
            float localX = 0f;

            float localY =
                i * stepHeight;

            float localZ =
                i * (stepDepth + gap);

            Vector3 localPosition =
                new Vector3(
                    localX,
                    localY,
                    localZ
                );

            /*
             * Rotamos la posición completa
             * alrededor del origen de la escalera.
             */
            Vector3 rotatedPosition =
                stairRotation * localPosition;

            step.transform.localPosition =
                rotatedPosition;

            /*
             * Todos los escalones mantienen
             * la misma orientación que la escalera.
             */
            step.transform.localRotation =
                stairRotation;

            /*
             * Tamaño del escalón.
             */
            step.transform.localScale =
                new Vector3(
                    stepWidth,
                    stepHeight,
                    stepDepth
                );
        }
    }

    public void ClearStair()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(
                transform.GetChild(i).gameObject
            );
        }
    }
}