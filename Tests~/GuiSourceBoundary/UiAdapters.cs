// UI storage adapters only. No calibration, controller, model or transport implementations.
namespace UnityEngine
{
    public sealed class GameObject { public bool activeSelf; public void SetActive(bool value) => activeSelf = value; }
    public sealed class WaitForSeconds { public WaitForSeconds(float seconds) { } }
}
namespace Michsky.MUIP
{
    public sealed class ProgressBar { public float currentPercent; }
    public sealed class CustomDropdown { public bool enabled = true; }
    public sealed class ButtonManager { public string buttonText; }
}
namespace TMPro { public sealed class TMP_Text { } }
