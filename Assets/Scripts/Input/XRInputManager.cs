using UnityEngine;
using UnityEngine.XR;

public class XRInputManager : MonoBehaviour
{
    public static XRInputManager Instance { get; private set; }

    public event System.Action OnLeftTriggerPressed;
    public event System.Action OnAButtonPressed;
    public event System.Action OnBButtonPressed;
    public event System.Action<Vector2> OnJoystickMoved;
    public event System.Action<int> OnJoystickTap;      // (direction: -1 left, 1 right)
    public event System.Action<int> OnJoystickHold;     // (direction: -1 left, 1 right)

    private bool _wasLeftTriggerPressed;
    private bool _wasAButtonPressed;
    private bool _wasBButtonPressed;
    private Vector2 _lastJoystickValue;

    // Joystick press tracking for tap vs hold
    private bool _joystickPressed;
    private float _joystickPressStartTime;
    private const float TAP_DURATION_THRESHOLD = 0.2f;

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

        Vector2 joystick = Vector2.zero;
        if (keyboard.aKey.isPressed) joystick.x = -1f;
        if (keyboard.dKey.isPressed) joystick.x = 1f;
        if (keyboard.wKey.isPressed) joystick.y = 1f;
        if (keyboard.sKey.isPressed) joystick.y = -1f;
        OnJoystickMoved?.Invoke(joystick);

        // Track joystick press for tap vs hold detection (X-axis only for carousel)
        bool joystickXPressed = (keyboard.aKey.isPressed || keyboard.dKey.isPressed);
        int joystickDirection = keyboard.aKey.isPressed ? -1 : (keyboard.dKey.isPressed ? 1 : 0);

        if (joystickXPressed && !_joystickPressed)
        {
            // Just pressed
            _joystickPressed = true;
            _joystickPressStartTime = Time.time;
        }
        else if (!joystickXPressed && _joystickPressed)
        {
            // Just released
            _joystickPressed = false;
            float pressDuration = Time.time - _joystickPressStartTime;
            int direction = keyboard.aKey.isPressed ? -1 : 1; // Use last direction

            if (pressDuration < TAP_DURATION_THRESHOLD)
            {
                OnJoystickTap?.Invoke(direction);
            }
            else
            {
                OnJoystickHold?.Invoke(direction);
            }
        }

        if (keyboard.enterKey.wasPressedThisFrame)
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

            // Track joystick press for tap vs hold detection (X-axis only for carousel)
            bool joystickXPressed = Mathf.Abs(joystickValue.x) > 0.5f;
            int joystickDirection = joystickValue.x < -0.5f ? -1 : (joystickValue.x > 0.5f ? 1 : 0);

            if (joystickXPressed && !_joystickPressed)
            {
                // Just pressed
                _joystickPressed = true;
                _joystickPressStartTime = Time.time;
            }
            else if (!joystickXPressed && _joystickPressed)
            {
                // Just released
                _joystickPressed = false;
                float pressDuration = Time.time - _joystickPressStartTime;
                int direction = joystickValue.x < -0.5f ? -1 : 1; // Use last direction

                if (pressDuration < TAP_DURATION_THRESHOLD)
                {
                    OnJoystickTap?.Invoke(direction);
                }
                else
                {
                    OnJoystickHold?.Invoke(direction);
                }
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

