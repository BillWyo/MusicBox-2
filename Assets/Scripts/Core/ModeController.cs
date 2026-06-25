using UnityEngine;

public class ModeController : MonoBehaviour
{
    public static ModeController Instance { get; private set; }

    public enum Mode { Browse, Review }
    public Mode CurrentMode { get; private set; } = Mode.Browse;

    public event System.Action<Mode> OnModeSelected;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        if (XRInputManager.Instance != null)
            XRInputManager.Instance.OnLeftTriggerPressed += ToggleMode;
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
        OnModeSelected?.Invoke(newMode);
    }

    void OnDestroy()
    {
        if (XRInputManager.Instance != null)
            XRInputManager.Instance.OnLeftTriggerPressed -= ToggleMode;
    }
}
