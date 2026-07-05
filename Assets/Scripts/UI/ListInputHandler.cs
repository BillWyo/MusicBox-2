using UnityEngine;

public class ListInputHandler : MonoBehaviour
{
    [SerializeField] private ListController _trackListController;
    [SerializeField] private ListController _playlistController;
    private bool _subscribed;
    private int _focusedListIndex = 0; // 0 = tracklist (right), 1 = playlist (left)

    public void SetListControllers(ListController trackList, ListController playlist)
    {
        _trackListController = trackList;
        _playlistController = playlist;
        UpdateFocus();
    }

    void Start()
    {
        if (XRInputManager.Instance != null)
        {
            XRInputManager.Instance.OnJoystickMoved += OnJoystickMoved;
            XRInputManager.Instance.OnRightTriggerPressed += OnSelectPressed;
            _subscribed = true;
            Debug.Log("ListInputHandler subscribed to XRInputManager");
            UpdateFocus();
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
            UpdateFocus();
        }
    }

    void OnJoystickMoved(Vector2 value)
    {
        if (!gameObject.activeSelf) return;

        // X axis: left/right focus switching
        if (value.x < -0.5f)
            SwitchFocus(1); // Left -> Playlist
        else if (value.x > 0.5f)
            SwitchFocus(0); // Right -> Tracklist

        // Y axis: scroll focused list
        ListController focused = GetFocusedList();
        if (focused == null) return;

        if (value.y < -0.5f)
            focused.ScrollDown();
        else if (value.y > 0.5f)
            focused.ScrollUp();
    }

    void OnSelectPressed()
    {
        if (!gameObject.activeSelf) return;

        ListController focused = GetFocusedList();
        if (focused == null) return;

        focused.SelectCenter();
    }

    void SwitchFocus(int listIndex)
    {
        if (_focusedListIndex != listIndex)
        {
            _focusedListIndex = listIndex;
            UpdateFocus();
        }
    }

    ListController GetFocusedList()
    {
        return _focusedListIndex == 0 ? _trackListController : _playlistController;
    }

    void UpdateFocus()
    {
        if (_trackListController != null)
            _trackListController.SetFocused(_focusedListIndex == 0);
        if (_playlistController != null)
            _playlistController.SetFocused(_focusedListIndex == 1);
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
