using System;
using UnityEngine;
using MoreMountains.Feedbacks;

public class FruitMergedFeedbackPlayer : MonoBehaviour
{
    [Serializable]
    public struct LevelShake
    {
        public int level;        // newLevel from RaiseFruitMerged
        public float duration;
        public float amplitude;
        public float frequency;
    }

    [Header("Refs")]
    [SerializeField] private MMF_Player feedbacks;
    [SerializeField] private bool moveToMergePos = true;

    [Header("Per-Level Shake")]
    [SerializeField] private LevelShake[] shakes;

    [Header("Fallback")]
    [SerializeField]
    private LevelShake fallback = new LevelShake
    {
        level = -1,
        duration = 0.25f,
        amplitude = 0.6f,
        frequency = 2.5f
    };

    private MMF_CameraShake camShake;

    private void Awake()
    {
        //if (!feedbacks)
            //feedbacks = GetComponent<MMFeedbacks>();

        // 🔥 THE LINE YOU ASKED FOR
        camShake = feedbacks.GetFeedbackOfType<MMF_CameraShake>();
    }

    private void OnEnable()
    {
        GameSignals.FruitMerged += OnFruitMerged;
    }

    private void OnDisable()
    {
        GameSignals.FruitMerged -= OnFruitMerged;
    }

    private void OnFruitMerged(Vector2 pos, int newLevel, int combo)
    {
        if (!feedbacks)
            return;

        if (moveToMergePos)
        {
            feedbacks.transform.position =
                new Vector3(pos.x, pos.y, feedbacks.transform.position.z);
        }

        LevelShake s = GetShake(newLevel);
        Debug.Log(s.level);
        // ✅ Set shake values dynamically
        camShake.CameraShakeProperties.Duration = s.duration;
        camShake.CameraShakeProperties.Amplitude = s.amplitude;
        camShake.CameraShakeProperties.Frequency = s.frequency;

        // Optional combo scaling (uncomment if wanted)
        // float comboMul = 1f + Mathf.Clamp(combo - 1, 0, 5) * 0.1f;
        // camShake.ShakeAmplitude *= comboMul;

        feedbacks.PlayFeedbacks(pos);
    }

    private LevelShake GetShake(int level)
    {
        if (shakes != null)
        {
            for (int i = 0; i < shakes.Length; i++)
            {
                if (shakes[i].level == level)
                    return shakes[i];
            }
        }

        return fallback;
    }
}