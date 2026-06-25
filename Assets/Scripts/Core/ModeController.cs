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

    public void SetMode(Mode newMode)
    {
        CurrentMode = newMode;
        OnModeSelected?.Invoke(newMode);
    }
}
