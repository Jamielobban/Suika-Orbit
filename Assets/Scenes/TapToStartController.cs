using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class TapToStartController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject startOverlayRoot;
    [SerializeField] private CanvasGroup startOverlayGroup;

    [Header("Gameplay")]
    [SerializeField] private FruitLauncherBase launcher;

    private bool started;

    private void Awake()
    {
        started = false;

        if (startOverlayRoot != null)
            startOverlayRoot.SetActive(true);

        if (startOverlayGroup != null)
        {
            startOverlayGroup.alpha = 1f;
            startOverlayGroup.interactable = true;
            startOverlayGroup.blocksRaycasts = true;
        }
    }

    private void Update()
    {
        if (started) return;

        bool tapped =
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (tapped)
            StartGame();
    }

    public void StartGame()
    {
        if (started) return;
        started = true;

        if (launcher != null)
            launcher.BeginRun();

        if (startOverlayGroup != null)
        {
            startOverlayGroup
                .DOFade(0f, 0.2f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    startOverlayGroup.interactable = false;
                    startOverlayGroup.blocksRaycasts = false;

                    if (startOverlayRoot != null)
                        startOverlayRoot.SetActive(false);
                });
        }
        else if (startOverlayRoot != null)
        {
            startOverlayRoot.SetActive(false);
        }
    }
}