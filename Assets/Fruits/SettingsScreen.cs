using System.Collections;
using UnityEngine;
using DG.Tweening;

public class SettingsScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup overlay;

    [Header("Animation")]
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.2f;

    private bool transitioning;

    private void Awake()
    {
        if (panel)
            panel.localScale = Vector3.zero;

        if (overlay)
            overlay.alpha = 0f;

        if (settingsPanel)
            settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (transitioning) return;

        settingsPanel.SetActive(true);
        GameInput.Lock();

        panel.localScale = Vector3.one * 0.8f;
        if (overlay) overlay.alpha = 0f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (overlay)
            seq.Append(overlay.DOFade(1f, openDuration));

        if (panel)
            seq.Join(panel.DOScale(1f, openDuration).SetEase(Ease.OutBack));
    }

    public void CloseSettings()
    {
        if (transitioning) return;

        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        transitioning = true;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (panel)
            seq.Join(panel.DOScale(0.9f, closeDuration).SetEase(Ease.InBack));

        if (overlay)
            seq.Join(overlay.DOFade(0f, closeDuration));

        yield return seq.WaitForCompletion();

        settingsPanel.SetActive(false);
        GameInput.Unlock();

        transitioning = false;
    }
}