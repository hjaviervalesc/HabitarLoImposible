using UnityEngine;

public class CircularInvisibleBarrier : MonoBehaviour
{
    [Header("Forma del círculo")]
    [Min(0.1f)]
    [SerializeField]
    private float radius = 50f;

    [Min(3)]
    [SerializeField]
    private int segments = 64;

    [Header("Dimensiones de la barrera")]
    [Min(0.01f)]
    [SerializeField]
    private float barrierThickness = 0.2f;

    [Min(0.1f)]
    [SerializeField]
    private float barrierHeight = 2f;

    [Tooltip(
        "Altura local de la base de la barrera."
    )]
    [SerializeField]
    private float baseHeight = 0f;

    [Header("Generación")]
    [SerializeField]
    private bool generateOnStart = true;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateBarriers();
        }
    }

    [ContextMenu("Generate Barriers")]
    public void GenerateBarriers()
    {
        ClearBarriers();

        float angleStep =
            360f / segments;

        /*
         * Longitud de cada segmento calculada mediante
         * la cuerda del círculo.
         */
        float segmentLength =
            2f *
            radius *
            Mathf.Sin(
                Mathf.PI / segments
            );

        for (int i = 0; i < segments; i++)
        {
            float angle =
                i * angleStep;

            float angleRadians =
                angle * Mathf.Deg2Rad;

            Vector3 localPosition =
                new Vector3(
                    Mathf.Cos(angleRadians) * radius,
                    baseHeight + barrierHeight * 0.5f,
                    Mathf.Sin(angleRadians) * radius
                );

            GameObject barrier =
                new GameObject(
                    "Barrier_" + i
                );

            barrier.transform.SetParent(
                transform,
                false
            );

            barrier.transform.localPosition =
                localPosition;

            /*
             * Orienta cada collider siguiendo
             * la tangente del círculo.
             */
            barrier.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    -angle,
                    0f
                );

            BoxCollider boxCollider =
                barrier.AddComponent<BoxCollider>();

            boxCollider.size =
                new Vector3(
                    barrierThickness,
                    barrierHeight,
                    segmentLength * 1.05f
                );

            boxCollider.isTrigger = false;
        }
    }

    [ContextMenu("Clear Barriers")]
    public void ClearBarriers()
    {
        for (int i = transform.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                transform.GetChild(i);

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}