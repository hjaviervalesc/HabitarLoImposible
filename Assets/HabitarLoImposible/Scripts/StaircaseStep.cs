using UnityEngine;

public class StaircaseStep : MonoBehaviour
{
    public int StepIndex { get; private set; }

    public void Initialize(int index)
    {
        StepIndex = index;
    }
}