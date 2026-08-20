using System.Collections.Generic;
using UnityEngine;

public class ImpossibleStaircaseManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private GameObject stepPrefab;
    [SerializeField] private StaircaseTrajectory trajectory;

    [Header("Step")]
    [SerializeField] private float stepWidth = 1f;
    [SerializeField] private float stepDepth = 0.35f;
    [SerializeField] private float stepHeight = 0.15f;

    [Header("Staircase")]
    [SerializeField] private int visibleSteps = 10;
    [SerializeField] private float stepVerticalDistance = 0.18f;

    private int currentPlayerStep = 0;
    [SerializeField] private int stepsBehind = 2;

    private readonly List<StaircaseStep> activeSteps = new();

    private int nextStepIndex = 0;

    private void Start()
    {
        GenerateInitialStaircase();
    }

    private void Update()
    {
        UpdateStaircase();
    }

    private void GenerateInitialStaircase()
    {
        for (int i = 0; i < visibleSteps; i++)
        {
            CreateStep();
        }
    }
    private void UpdateStaircase()
    {
        if (activeSteps.Count == 0)
            return;

        int playerStep =
            trajectory.GetClosestStepIndex(xrOrigin.position);

        if (playerStep <= currentPlayerStep)
            return;

        currentPlayerStep = playerStep;

        int minimumStepToKeep =
            currentPlayerStep - stepsBehind;

        while (activeSteps.Count > 0 &&
               activeSteps[0].StepIndex < minimumStepToKeep)
        {
            RemoveOldestStep();
        }

        int highestStepNeeded =
            currentPlayerStep + (visibleSteps - stepsBehind);

        while (nextStepIndex <= highestStepNeeded)
        {
            CreateStep();
        }
    }
    private void CreateStep()
    {
        GameObject stepObject = Instantiate(stepPrefab, transform);

        stepObject.name = "Step_" + nextStepIndex;

        stepObject.transform.localScale = new Vector3(
            stepWidth,
            stepHeight,
            stepDepth
        );

        stepObject.transform.localPosition =
    trajectory.GetPosition(nextStepIndex);

        stepObject.transform.localRotation =
            trajectory.GetRotation(nextStepIndex);

        StaircaseStep step = stepObject.GetComponent<StaircaseStep>();

        if (step == null)
        {
            step = stepObject.AddComponent<StaircaseStep>();
        }

        step.Initialize(nextStepIndex);

        activeSteps.Add(step);

        nextStepIndex++;
    }

    private void RemoveOldestStep()
    {
        StaircaseStep oldestStep = activeSteps[0];

        activeSteps.RemoveAt(0);

        Destroy(oldestStep.gameObject);
    }
}