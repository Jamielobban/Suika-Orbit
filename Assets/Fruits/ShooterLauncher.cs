// ShooterLauncher.cs (only power stability; NO move-based rebuild gate)
using UnityEngine;

public class ShooterLauncher : FruitLauncherBase
{
    [Header("Trajectory Preview")]
    [SerializeField] private bool showTrajectory = true;
    [SerializeField] private int steps = 25;
    [SerializeField] private float dt = 0.06f;
    [SerializeField] private LayerMask hitMask;

    [Header("Dotted Preview")]
    [SerializeField] private DottedPreviewRenderer dottedPreview;

    [Header("Point Gravity Source")]
    [Tooltip("If null, we auto-find the first PointGravity2D in the scene.")]
    [SerializeField] private PointGravity2D pointGravity;

    protected override void Awake()
    {
        base.Awake();
        if (!pointGravity) pointGravity = FindFirstObjectByType<PointGravity2D>();
    }

    protected override void UpdatePreviewVisual(AimResult a)
    {
        bool on = showTrajectory && heldFruit != null && !spawnQueued && !gameOver && isAiming;

        if (!on)
        {
            if (dottedPreview) dottedPreview.HideAll();
            return;
        }

        float pulled01 = powerCurve != null ? powerCurve.Evaluate(a.power01) : a.power01;
        pulled01 = Mathf.Clamp01(pulled01);

        if (!pointGravity || !pointGravity.Center)
        {
            Vector3[] fallback = new Vector3[2];
            fallback[0] = muzzle.position;
            fallback[1] = (Vector2)muzzle.position + a.dir * 2f;

            if (dottedPreview) dottedPreview.RenderFullPath(fallback, pulled01);
            return;
        }

        float speed = Mathf.Lerp(minSpeed, maxSpeed, pulled01);
        Vector3[] path = BuildTrajectoryPoints(muzzle.position, a.dir * speed, pointGravity);

        if (dottedPreview)
            dottedPreview.RenderFullPath(path, pulled01);
    }

    private Vector3[] BuildTrajectoryPoints(Vector2 start, Vector2 v0, PointGravity2D g)
    {
        int count = Mathf.Max(2, steps);
        Vector3[] pts = new Vector3[count];

        Vector2 pos = start;
        Vector2 vel = v0;

        pts[0] = pos;
        int finalCount = count;

        for (int i = 1; i < count; i++)
        {
            Vector2 acc = g.ComputeAcceleration(pos, vel);
            vel += acc * dt;

            float maxV = g.MaxSpeed;
            if (maxV > 0.01f && vel.sqrMagnitude > maxV * maxV)
                vel = vel.normalized * maxV;

            Vector2 next = pos + vel * dt;

            if (hitMask.value != 0)
            {
                Vector2 seg = next - pos;
                float dist = seg.magnitude;

                if (dist > 1e-4f)
                {
                    var hit = Physics2D.Raycast(pos, seg / dist, dist, hitMask);
                    if (hit.collider != null)
                    {
                        pts[i] = hit.point;
                        finalCount = i + 1;
                        break;
                    }
                }
            }

            pos = next;
            pts[i] = pos;
        }

        if (finalCount != count)
        {
            Vector3[] trimmed = new Vector3[finalCount];
            System.Array.Copy(pts, trimmed, finalCount);
            return trimmed;
        }

        return pts;
    }
}