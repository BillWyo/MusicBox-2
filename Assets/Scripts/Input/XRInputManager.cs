using UnityEngine;
using UnityEngine.XR;

public class XRInputManager : MonoBehaviour
{
    public static XRInputManager Instance { get; private set; }

    public event System.Action OnLeftTriggerPressed;
    public event System.Action OnAButtonPressed;
    public event System.Action OnBButtonPressed;
    public event System.Action<Vector2> OnJoystickMoved;

    private bool _wasLeftTriggerPressed;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        Debug.Log("XRInputManager initialized");
    }

    void Update()
    {
        PollInputs();
    }

    void PollInputs()
    {
        #if UNITY_EDITOR
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Left trigger pressed (editor test)");
            OnLeftTriggerPressed?.Invoke();
        }
        #endif

        var leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            bool isPressed = triggerValue > 0.5f;
            if (isPressed && !_wasLeftTriggerPressed)
            {
                Debug.Log("Left trigger pressed");
                OnLeftTriggerPressed?.Invoke();
            }
            _wasLeftTriggerPressed = isPressed;
        }
    }
}
