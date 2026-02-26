using UnityEngine;
using MoreMountains.Feedbacks;

public class FruitJuiceMMF : MonoBehaviour
{
    [SerializeField] private MMF_Player mergeSquash;

    private void Reset()
    {
        if (!mergeSquash)
            mergeSquash = GetComponent<MMF_Player>();
    }

    public void PlayMergeSquash()
    {
        if (!mergeSquash) return;

        mergeSquash.PlayFeedbacks();
    }
}