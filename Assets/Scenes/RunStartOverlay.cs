using DG.Tweening;
using UnityEngine;

public class RunStartOverlay : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private GameObject ingameMenu;
    [SerializeField] private GameObject launcherObject;
    [SerializeField] private FruitLauncherBase launcher;
    [SerializeField] private GameObject orbit;

    private static bool hasStartedOnceThisSession;

    private void Awake()
    {
        if (hasStartedOnceThisSession)
        {
            SetStartedStateImmediate();

            if (launcher != null)
                launcher.BeginRun();

            return;
        }

        SetPreStartStateImmediate();
    }

    private void OnEnable()
    {
        GameSignals.RunStarted += OnRunStarted;
    }

    private void OnDisable()
    {
        GameSignals.RunStarted -= OnRunStarted;
    }

    private void OnRunStarted()
    {
        hasStartedOnceThisSession = true;

        if (ingameMenu != null)
            ingameMenu.SetActive(true);

        if (launcherObject != null)
            launcherObject.SetActive(true);

        if (orbit != null)
            orbit.SetActive(true);

        if (overlayGroup != null)
        {
            overlayGroup
                .DOFade(0f, 0.2f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    overlayGroup.interactable = false;
                    overlayGroup.blocksRaycasts = false;

                    if (overlayRoot != null)
                        overlayRoot.SetActive(false);
                });
        }
        else if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    public void StartRunFromTap()
    {
        if (launcherObject != null)
            launcherObject.SetActive(true);

        if (orbit != null)
            orbit.SetActive(true);

        if (launcher != null)
            launcher.BeginRun();
    }

    private void SetPreStartStateImmediate()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 1f;
            overlayGroup.interactable = true;
            overlayGroup.blocksRaycasts = true;
        }

        if (ingameMenu != null)
            ingameMenu.SetActive(false);

        if (launcherObject != null)
            launcherObject.SetActive(false);

        if (orbit != null)
            orbit.SetActive(false);
    }

    private void SetStartedStateImmediate()
    {
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
        }

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        if (ingameMenu != null)
            ingameMenu.SetActive(true);

        if (launcherObject != null)
            launcherObject.SetActive(true);

        if (orbit != null)
            orbit.SetActive(true);
    }

    public static void ResetSessionStartOverlay()
    {
        hasStartedOnceThisSession = false;
    }
}