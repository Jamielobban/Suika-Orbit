using UnityEngine;

public class MergeVFXSet : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ParticleSystem burst;
    [SerializeField] private ParticleSystem sparkles;
    [SerializeField] private ParticleSystem ring;

    public bool IsBusy { get; private set; }

    public void Play(Vector2 pos, float scale, int extraBurst, int extraSparkles)
    {
        gameObject.SetActive(true);
        IsBusy = true;

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        transform.localScale = Vector3.one * scale;

        // hard reset each system
        PlayOne(burst, extraBurst);
        PlayOne(sparkles, extraSparkles);
        PlayOne(ring, 0);
    }

    private void PlayOne(ParticleSystem ps, int extraEmit)
    {
        if (!ps) return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear(true);
        ps.Simulate(0f, true, true, true);

        ps.Play(true);
        if (extraEmit > 0) ps.Emit(extraEmit);
    }

    private void Update()
    {
        // busy while any system still alive
        bool alive =
            (burst && burst.IsAlive(true)) ||
            (sparkles && sparkles.IsAlive(true)) ||
            (ring && ring.IsAlive(true));

        if (!alive)
            IsBusy = false;
    }
}