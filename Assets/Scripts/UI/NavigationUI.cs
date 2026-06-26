using UnityEngine;
using TMPro;

public class NavigationUI : MonoBehaviour
{
    private TextMeshPro _modeText;

    void Start()
    {
        _modeText = GetComponent<TextMeshPro>();
        ModeController.Instance.OnModeSelected += OnModeChanged;
        UpdateModeText(ModeController.Instance.CurrentMode);
    }

    void OnModeChanged(ModeController.Mode newMode)
    {
        UpdateModeText(newMode);
    }

    void UpdateModeText(ModeController.Mode mode)
    {
        _modeText.text = mode == ModeController.Mode.Browse ? "BROWSE ALBUMS" : "REVIEW PLAYLISTS";
    }

    void OnDestroy()
    {
        if (ModeController.Instance != null)
            ModeController.Instance.OnModeSelected -= OnModeChanged;
    }
}
