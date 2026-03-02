using UnityEngine;

public class SettingsScreen : MonoBehaviour
{
    public GameObject settingsPanel;
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        GameInput.Lock();
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        GameInput.Unlock();
    }
}
