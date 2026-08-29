using UnityEngine;

public class TestRotation : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        Debug.Log(
            "ROTANDO: " + target.name +
            " | ID SCRIPT: " + GetInstanceID()
        );

        target.Rotate(
            10f * Time.deltaTime,
            0f,
            0f,
            Space.Self
        );
    }
}