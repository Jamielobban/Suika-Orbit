using UnityEngine;

public class WatchAdButton : MonoBehaviour
{
    [SerializeField] private MergePowerup mergePowerup; // drag your merge powerup script here

    public void WatchAdForMerge()
    {
        AdsManager.I.ShowRewarded(() =>
        {
            mergePowerup.TryApplyRandomMerges(3);
        });
    }
}