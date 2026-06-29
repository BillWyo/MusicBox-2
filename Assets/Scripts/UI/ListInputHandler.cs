using UnityEngine;

public class ListInputHandler : MonoBehaviour
{
    [SerializeField] private ListController _listController;
    private bool _subscribed;

    void Start()
    {
        if (XRInputManager.Instance != null)
        {
            XRInputManager.Instance.OnJoystickMoved += OnJoystickMoved;
            XRInputManager.Instance.OnRightTriggerPressed += OnSelectPressed;
            _subscribed = true;
            Debug.Log("ListInputHandler subscribed to XRInputManager");
        }
        else
        {
            Debug.LogError("ListInputHandler: XRInputManager.Instance is null!");
        }
    }

    void Update()
    {
        if (XRInputManager.Instance != null && !_subscribed)
        {
            XRInputManager.Instance.OnJoystickMoved += OnJoystickMoved;
            XRInputManager.Instance.OnRightTriggerPressed += OnSelectPressed;
            _subscribed = true;
            Debug.Log("ListInputHandler retried subscription in Update()");
        }
    }

    void OnJoystickMoved(Vector2 value)
    {
        if (!gameObject.activeSelf || _listController == null) return;

        if (value.y < -0.5f)
            _listController.ScrollDown();
        else if (value.y > 0.5f)
            _listController.ScrollUp();
    }

    void OnSelectPressed()
    {
        if (!gameObject.activeSelf || _listController == null) return;
        _listController.SelectCenter();
    }

    void OnDestroy()
    {
        if (XRInputManager.Instance != null)
        {
            XRInputManager.Instance.OnJoystickMoved -= OnJoystickMoved;
            XRInputManager.Instance.OnRightTriggerPressed -= OnSelectPressed;
        }
    }
}
