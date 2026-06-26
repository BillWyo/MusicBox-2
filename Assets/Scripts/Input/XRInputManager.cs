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
    private bool _wasAButtonPressed;
    private bool _wasBButtonPressed;
    private Vector2 _lastJoystickValue;

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
        PollEditorInputs();
        #else
        PollDeviceInputs();
        #endif
    }

    void PollEditorInputs()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Left trigger pressed (editor test)");
            OnLeftTriggerPressed?.Invoke();
        }

        if (keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            Vector2 joystick = Vector2.zero;
            if (keyboard.leftArrowKey.isPressed) joystick.x = -1f;
            if (keyboard.rightArrowKey.isPressed) joystick.x = 1f;
            if (joystick != _lastJoystickValue)
            {
                OnJoystickMoved?.Invoke(joystick);
                _lastJoystickValue = joystick;
            }
        }
        else if (_lastJoystickValue != Vector2.zero)
        {
            OnJoystickMoved?.Invoke(Vector2.zero);
            _lastJoystickValue = Vector2.zero;
        }

        if (keyboard.returnKey.wasPressedThisFrame)
        {
            Debug.Log("A button pressed (editor test)");
            OnAButtonPressed?.Invoke();
        }

        if (keyboard.backspaceKey.wasPressedThisFrame)
        {
            Debug.Log("B button pressed (editor test)");
            OnBButtonPressed?.Invoke();
        }
    }

    void PollDeviceInputs()
    {
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

        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystickValue))
        {
            if (joystickValue != _lastJoystickValue)
            {
                OnJoystickMoved?.Invoke(joystickValue);
                _lastJoystickValue = joystickValue;
            }
        }

        var rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
        {
            if (aPressed && !_wasAButtonPressed)
            {
                Debug.Log("A button pressed");
                OnAButtonPressed?.Invoke();
            }
            _wasAButtonPressed = aPressed;
        }

        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bPressed))
        {
            if (bPressed && !_wasBButtonPressed)
            {
                Debug.Log("B button pressed");
                OnBButtonPressed?.Invoke();
            }
            _wasBButtonPressed = bPressed;
        }
    }
}

