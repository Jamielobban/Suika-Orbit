using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Tools;

public class MMSoundSlidersInit : MonoBehaviour
{
    [SerializeField] private Slider master;
    [SerializeField] private Slider music;
    [SerializeField] private Slider sfx;

    private void OnEnable()
    {
        if (!MMSoundManager.HasInstance) return;

        var so = MMSoundManager.Instance.settingsSo;
        if (so == null) return;

        // Important: sync from mixer into the SO values
        so.GetTrackVolumes();

        master.SetValueWithoutNotify(
            so.GetTrackVolume(MMSoundManager.MMSoundManagerTracks.Master));

        music.SetValueWithoutNotify(
            so.GetTrackVolume(MMSoundManager.MMSoundManagerTracks.Music));

        sfx.SetValueWithoutNotify(
            so.GetTrackVolume(MMSoundManager.MMSoundManagerTracks.Sfx));

    }
}