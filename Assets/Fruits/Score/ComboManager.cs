using UnityEngine;

public class ComboManager : MonoBehaviour
{
    public static ComboManager I { get; private set; }

    [SerializeField] private float comboWindow = 0.5f;
    [SerializeField] private int maxCombo = 20;

    public int ComboCount { get; private set; }
    public float LastMergeTime { get; private set; }

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        ResetCombo();
    }

    public int RegisterMerge(Vector2 worldPos, int newLevel)
    {
        float now = Time.time;

        if (now - LastMergeTime <= comboWindow)
            ComboCount = Mathf.Min(ComboCount + 1, maxCombo);
        else
            ComboCount = 1;

        LastMergeTime = now;
        return ComboCount;
    }

    public void ResetCombo()
    {
        ComboCount = 0;
        LastMergeTime = -999f;
    }
}
