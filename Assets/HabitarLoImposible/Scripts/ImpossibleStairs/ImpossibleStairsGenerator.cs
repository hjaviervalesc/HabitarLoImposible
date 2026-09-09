using UnityEngine;

public class ImpossibleStairsGenerator : MonoBehaviour
{
    [Header("REFERENCIAS")]

    [SerializeField]
    private GameObject cubePrefab;

    [Tooltip("Padre de los escalones. Si está vacío se utiliza este GameObject.")]
    [SerializeField]
    private Transform stepsParent;


    [Header("NÚMERO DE ESCALONES")]

    [Min(1)]
    [SerializeField]
    private int numberOfSteps = 40;


    [Header("POSICIÓN")]

    [Tooltip("Posición local del primer escalón.")]
    [SerializeField]
    private Vector3 firstStepPosition = Vector3.zero;

    [Tooltip("Desplazamiento constante entre escalones.")]
    [SerializeField]
    private Vector3 positionOffset =
        new Vector3(-0.72f, -0.44f, 0f);


    [Header("ROTACIÓN")]

    [Tooltip("Rotación inicial sobre el eje Z.")]
    [SerializeField]
    private float initialRotationZ = 0f;

    [Tooltip("Giro añadido a cada nuevo escalón.")]
    [SerializeField]
    private float rotationPerStep = 3.75f;


    [Header("ESCALA")]

    [SerializeField]
    private Vector3 stepScale =
        new Vector3(1f, 0.73f, 2.5f);


    private void Start()
    {
        GenerateSteps();
    }


    private void GenerateSteps()
    {
        if (cubePrefab == null)
        {
            Debug.LogError(
                "ImpossibleStairsGenerator: no se ha asignado Cube Prefab.",
                this
            );

            return;
        }

        if (stepsParent == null)
            stepsParent = transform;

        for (int i = 0; i < numberOfSteps; i++)
        {
            /*
             * Cada escalón conserva la misma relación de posición
             * respecto al escalón anterior.
             */
            Vector3 stepPosition =
                firstStepPosition + positionOffset * i;

            /*
             * Cada escalón añade progresivamente una rotación
             * sobre el eje Z.
             */
            float rotationZ =
                initialRotationZ + rotationPerStep * i;

            Quaternion stepRotation =
                Quaternion.Euler(0f, 0f, rotationZ);

            GameObject newStep = Instantiate(cubePrefab);

            newStep.transform.SetParent(stepsParent, false);

            newStep.name = $"ImpossibleStep_{i}";

            newStep.transform.localPosition = stepPosition;
            newStep.transform.localRotation = stepRotation;
            newStep.transform.localScale = stepScale;
        }
    }
}