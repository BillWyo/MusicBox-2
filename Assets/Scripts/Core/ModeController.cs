using UnityEngine;

public class ModeController : MonoBehaviour
{
    public static ModeController Instance { get; private set; }

    public enum Mode { Browse, Review }
    public Mode CurrentMode { get; private set; } = Mode.Browse;

    public event System.Action<Mode> OnModeSelected;

    [SerializeField] private GameObject _albumRolodex;
    [SerializeField] private GameObject _playlistRolodex;
    [SerializeField] private GameObject _listPanel;

    private bool _subscribed;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        UpdateUIVisibility(CurrentMode);
        TrySubscribe();
    }

    void Update()
    {
        if (!_subscribed) TrySubscribe();
    }

    void TrySubscribe()
    {
        if (_subscribed || XRInputManager.Instance == null) return;
        XRInputManager.Instance.OnLeftTriggerPressed += ToggleMode;
        _subscribed = true;
        Debug.Log("ModeController subscribed to XRInputManager");
    }

    void ToggleMode()
    {
        Mode newMode = CurrentMode == Mode.Browse ? Mode.Review : Mode.Browse;
        SetMode(newMode);
    }

    public void SetMode(Mode newMode)
    {
        CurrentMode = newMode;
        Debug.Log($"Mode changed to: {newMode}");
        UpdateUIVisibility(newMode);
        OnModeSelected?.Invoke(newMode);
    }

    void UpdateUIVisibility(Mode mode)
    {
        if (_albumRolodex != null)
            _albumRolodex.SetActive(mode == Mode.Browse);

        if (_playlistRolodex != null)
            _playlistRolodex.SetActive(mode == Mode.Review);

        if (_listPanel != null)
            _listPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (XRInputManager.Instance != null)
            XRInputManager.Instance.OnLeftTriggerPressed -= ToggleMode;
    }
}
