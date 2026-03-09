using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSfx : MonoBehaviour
{
    [SerializeField] private bool useBackSound;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlaySound);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlaySound);
    }

    private void PlaySound()
    {
        if (UISfx.I == null) return;

        if (useBackSound) UISfx.I.PlayBack();
        else UISfx.I.PlayClick();
    }
}