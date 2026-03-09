using UnityEngine;

/// <summary>
/// Global point gravity toward a target transform.
/// - No triggers or colliders.
/// - Unity gravity is disabled (gravityScale = 0).
/// - Constant inward pull (NOT inverse-square).
/// - Radial damping only (preserves circular / sideways motion).
/// - Exposes ComputeAcceleration() so preview == gameplay.
/// </summary>
public class PointGravity2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform center;

    [Header("Which bodies")]
    [SerializeField] private LayerMask fruitMask;

    [Header("Gravity")]
    [Tooltip("Constant inward acceleration strength.")]
    [SerializeField] private float gravityStrength = 25f;

    [Tooltip("Prevents weirdness extremely close to center.")]
    [SerializeField] private float minDistance = 0.05f;

    [Header("Damping")]
    [Tooltip("Damps motion TOWARD / AWAY from center.")]
    [SerializeField] private float radialDamping = 3.0f;

    [Tooltip("Damps sideways (orbital) motion. Keep very small or zero.")]
    [SerializeField] private float tangentialDamping = 0.05f;

    [Header("Limits")]
    [SerializeField] private float maxSpeed = 12f;

    [Header("Runtime")]
    [SerializeField] private bool runAutomatically = true;

    public Transform Center => center ? center : transform;
    public float MaxSpeed => maxSpeed;

    private void Awake()
    {
        if (!center) center = transform;

        gravityStrength = Mathf.Max(0f, gravityStrength);
        radialDamping = Mathf.Max(0f, radialDamping);
        tangentialDamping = Mathf.Max(0f, tangentialDamping);
        maxSpeed = Mathf.Max(0.01f, maxSpeed);
        minDistance = Mathf.Max(0.001f, minDistance);
    }

    /// <summary>
    /// Computes acceleration at a position with a given velocity.
    /// Used by BOTH runtime physics and trajectory preview.
    /// </summary>
    public Vector2 ComputeAcceleration(Vector2 position, Vector2 velocity)
    {
        Vector2 toCenter = (Vector2)Center.position - position;
        float dist = toCenter.magnitude;

        if (dist < minDistance)
            return Vector2.zero;

        Vector2 radialDir = toCenter / dist;

        Vector2 acc = Vector2.zero;

        // --- Constant inward pull ---
        acc += radialDir * gravityStrength;

        // --- Split velocity ---
        float vRad = Vector2.Dot(velocity, radialDir);
        Vector2 radialVel = radialDir * vRad;
        Vector2 tangentialVel = velocity - radialVel;

        // --- Damping ---
        acc += -radialVel * radialDamping;
        if (tangentialDamping > 0f)
            acc += -tangentialVel * tangentialDamping;

        return acc;
    }

    /// <summary>
    /// Applies gravity to a rigidbody.
    /// </summary>
    public void ApplyTo(Rigidbody2D rb)
    {
        if (!rb) return;
        if (rb.bodyType != RigidbodyType2D.Dynamic) return;
        if (((1 << rb.gameObject.layer) & fruitMask) == 0) return;

        // Ensure Unity gravity is not involved
        rb.gravityScale = 0f;

        // Clamp speed
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude > maxSpeed * maxSpeed)
            rb.linearVelocity = v.normalized * maxSpeed;

        Vector2 acc = ComputeAcceleration(rb.position, rb.linearVelocity);
        rb.AddForce(acc * rb.mass, ForceMode2D.Force);
    }

    private void FixedUpdate()
    {
        if (!runAutomatically) return;

        var rbs = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        for (int i = 0; i < rbs.Length; i++)
            ApplyTo(rbs[i]);
    }

    private void OnDrawGizmosSelected()
    {
        Transform c = center ? center : transform;
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.25f);
        Gizmos.DrawWireSphere(c.position, 0.3f);
    }
}
