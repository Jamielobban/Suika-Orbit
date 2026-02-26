using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class CometMover : MonoBehaviour
{
    public float speed;
    public float turnDegPerSec;
    public float maxLife;

    private Vector2 dir;
    private float alive;
    private TrailRenderer tr;
    private System.Action<CometMover> returnToPool;

    private void Awake()
    {
        tr = GetComponent<TrailRenderer>();
    }

    public void Spawn(
        Vector2 startPos,
        Vector2 initialDir,
        float speed_,
        float turnDegPerSec_,
        float maxLife_,
        System.Action<CometMover> returnToPool_)
    {
        transform.position = startPos;
        transform.rotation = Quaternion.identity;

        speed = speed_;
        turnDegPerSec = turnDegPerSec_;
        maxLife = maxLife_;

        dir = initialDir.normalized;
        alive = 0f;

        returnToPool = returnToPool_;

        if (tr)
        {
            tr.Clear();
            tr.emitting = true;
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        alive += Time.deltaTime;

        if (alive >= maxLife)
        {
            Despawn();
            return;
        }

        // Constant curve turning
        float a = turnDegPerSec * Mathf.Deg2Rad * Time.deltaTime;
        float s = Mathf.Sin(a);
        float c = Mathf.Cos(a);

        dir = new Vector2(
            c * dir.x - s * dir.y,
            s * dir.x + c * dir.y
        ).normalized;

        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    public void Despawn()
    {
        if (tr) tr.emitting = false;

        gameObject.SetActive(false);
        returnToPool?.Invoke(this);
    }
}
