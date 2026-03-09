using UnityEngine;

public class UISfx : MonoBehaviour
{
    public static UISfx I { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip backClip;
    [SerializeField] private AudioClip popupClip;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    public void PlayClick()
    {
        Play(clickClip);
    }

    public void PlayBack()
    {
        Play(backClip);
    }

    public void PlayPopup()
    {
        Play(popupClip);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }
}