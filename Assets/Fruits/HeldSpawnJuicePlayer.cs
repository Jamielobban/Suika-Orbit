using UnityEngine;
using MoreMountains.Feedbacks;

public class HeldSpawnJuicePlayer : MonoBehaviour
{
    [SerializeField] private MMF_Player player;

    private MMF_Scale squash;

    private void Awake()
    {
        if (!player) player = GetComponent<MMF_Player>();

        squash = player.GetFeedbackOfType<MMF_Scale>();
    }

    public void PlayOn(Transform target)
    {
        if (!player || !target) return;
       
        //Debug.Log("What");
        squash.AnimateScaleTarget = target.gameObject.transform.GetChild(0);

        player.Initialization();

        player.PlayFeedbacks();
    }
}