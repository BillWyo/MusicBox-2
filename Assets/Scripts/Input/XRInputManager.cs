using UnityEngine;
using UnityEngine.XR;

public class XRInputManager : MonoBehaviour
{
    public static XRInputManager Instance { get; private set; }

    public event System.Action OnLeftTriggerPressed;      // Spacebar: Toggle Browse/Review
    public event System.Action OnRightTriggerPressed;     // Enter: Select item
    public event System.Action OnBackPressed;             // B key: Back/Close
    public event System.Action OnXButtonPressed;          // X key: Save (Left Primary)
    public event System.Action OnLeftContextMenu;         // Left Secondary (Y) - TODO
    public event System.Action OnRightContextMenu;        // Right Secondary (B) - TODO
    public event System.Action<Vector2> OnJoystickMoved;
    public event System.Action<int> OnJoystickTap;        // (direction: -1 left, 1 right)
    public event System.Action<int> OnJoystickHold;       // (direction: -1 left, 1 right)

    private bool _wasLeftTriggerPressed;
    private bool _wasRightTriggerPressed;
    private bool _wasBackPressed;
    private bool _wasXButtonPressed;
    private bool _wasLeftContextPressed;
    private bool _wasRightContextPressed;
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

        // Left Trigger (Spacebar) - Toggle Browse/Review
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Left trigger pressed (Toggle mode)");
            OnLeftTriggerPressed?.Invoke();
        }

        // Left Joystick X-axis only (A/D) - Carousel scroll
        Vector2 joystick = Vector2.zero;
        if (keyboard.aKey.isPressed) joystick.x = -1f;
        if (keyboard.dKey.isPressed) joystick.x = 1f;
        if (joystick != Vector2.zero)
            OnJoystickMoved?.Invoke(joystick);

        // Track left joystick X-axis for tap vs hold (carousel)
        bool joystickXPressed = (keyboard.aKey.isPressed || keyboard.dKey.isPressed);
        int joystickDirection = keyboard.aKey.isPressed ? -1 : (keyboard.dKey.isPressed ? 1 : 0);

        if (joystickXPressed && !_joystickPressed)
        {
            _joystickPressed = true;
            _joystickPressStartTime = Time.time;
        }
        else if (!joystickXPressed && _joystickPressed)
        {
            _joystickPressed = false;
            float pressDuration = Time.time - _joystickPressStartTime;
            int direction = keyboard.aKey.isPressed ? -1 : 1;

            if (pressDuration < TAP_DURATION_THRESHOLD)
            {
                OnJoystickTap?.Invoke(direction);
            }
            else
            {
                OnJoystickHold?.Invoke(direction);
            }
        }

        // Right Joystick Y-axis only (W/S) - List scroll
        joystick = Vector2.zero;
        if (keyboard.wKey.isPressed) joystick.y = 1f;
        if (keyboard.sKey.isPressed) joystick.y = -1f;
        if (joystick != Vector2.zero)
            OnJoystickMoved?.Invoke(joystick);

        // Right Trigger (Enter) - Select
        if (keyboard.enterKey.wasPressedThisFrame)
        {
            Debug.Log("Right trigger pressed (Select)");
            OnRightTriggerPressed?.Invoke();
        }

        // Right Primary (B) - Back/Close
        if (keyboard.bKey.wasPressedThisFrame)
        {
            Debug.Log("Right A button pressed (Back/Close)");
            OnBackPressed?.Invoke();
        }

        // Left Primary (X) - Save
        if (keyboard.xKey.wasPressedThisFrame)
        {
            Debug.Log("Left X button pressed (Save)");
            OnXButtonPressed?.Invoke();
        }

        // Right Secondary (Backspace) - Context menu
        if (keyboard.backspaceKey.wasPressedThisFrame)
        {
            Debug.Log("Right B button pressed (Context menu)");
            OnRightContextMenu?.Invoke();
        }
    }

    void PollDeviceInputs()
    {
        var leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        // Left Trigger (Spacebar) - Toggle Browse/Review
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue))
        {
            bool isPressed = leftTriggerValue > 0.5f;
            if (isPressed && !_wasLeftTriggerPressed)
            {
                Debug.Log("Left trigger pressed (Toggle mode)");
                OnLeftTriggerPressed?.Invoke();
            }
            _wasLeftTriggerPressed = isPressed;
        }

        // Left Joystick X-axis only (Carousel scroll)
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftJoystickValue))
        {
            Vector2 leftJoystickX = new Vector2(leftJoystickValue.x, 0);
            if (leftJoystickX != _lastJoystickValue)
            {
                OnJoystickMoved?.Invoke(leftJoystickX);
                _lastJoystickValue = leftJoystickX;
            }

            // Track joystick press for tap vs hold detection (X-axis only for carousel)
            bool joystickXPressed = Mathf.Abs(leftJoystickValue.x) > 0.5f;
            int joystickDirection = leftJoystickValue.x < -0.5f ? -1 : (leftJoystickValue.x > 0.5f ? 1 : 0);

            if (joystickXPressed && !_joystickPressed)
            {
                _joystickPressed = true;
                _joystickPressStartTime = Time.time;
            }
            else if (!joystickXPressed && _joystickPressed)
            {
                _joystickPressed = false;
                float pressDuration = Time.time - _joystickPressStartTime;
                int direction = leftJoystickValue.x < -0.5f ? -1 : 1;

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

        // Left Primary (X) - Save
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool xPressed))
        {
            if (xPressed && !_wasXButtonPressed)
            {
                Debug.Log("Left X button pressed (Save)");
                OnXButtonPressed?.Invoke();
            }
            _wasXButtonPressed = xPressed;
        }

        // Left Secondary (Y) - Context menu
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool yPressed))
        {
            if (yPressed && !_wasLeftContextPressed)
            {
                Debug.Log("Left Y button pressed (Context menu)");
                OnLeftContextMenu?.Invoke();
            }
            _wasLeftContextPressed = yPressed;
        }

        // Right Joystick Y-axis only (List scroll)
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightJoystickValue))
        {
            Vector2 rightJoystickY = new Vector2(0, rightJoystickValue.y);
            if (rightJoystickY != Vector2.zero)
            {
                OnJoystickMoved?.Invoke(rightJoystickY);
            }
        }

        // Right Trigger (Enter) - Select
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue))
        {
            bool isPressed = rightTriggerValue > 0.5f;
            if (isPressed && !_wasRightTriggerPressed)
            {
                Debug.Log("Right trigger pressed (Select)");
                OnRightTriggerPressed?.Invoke();
            }
            _wasRightTriggerPressed = isPressed;
        }

        // Right Primary (A) - Back/Close
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
        {
            if (aPressed && !_wasBackPressed)
            {
                Debug.Log("Right A button pressed (Back/Close)");
                OnBackPressed?.Invoke();
            }
            _wasBackPressed = aPressed;
        }

        // Right Secondary (B) - Context menu
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bPressed))
        {
            if (bPressed && !_wasRightContextPressed)
            {
                Debug.Log("Right B button pressed (Context menu)");
                OnRightContextMenu?.Invoke();
            }
            _wasRightContextPressed = bPressed;
        }
    }
}

