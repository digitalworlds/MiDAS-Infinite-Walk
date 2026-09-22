using UnityEngine;
using UnityEngine.InputSystem;

public class XRDebug : MonoBehaviour
{
    public InputActionProperty leftTrigger;
    public InputActionProperty rightTrigger;

    void Update()
    {
        float lt = leftTrigger.action.ReadValue<float>();
        float rt = rightTrigger.action.ReadValue<float>();
        Debug.Log($"Left Trigger: {lt} | Right Trigger: {rt}");
    }
}
