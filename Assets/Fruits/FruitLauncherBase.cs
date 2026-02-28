using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class FruitLauncherBase : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] protected InputActionReference pointAction;
    [SerializeField] protected InputActionReference pressAction;

    [Header("Refs")]
    [SerializeField] protected Camera cam;
    [SerializeField] protected FruitDatabase database;

    [Header("Launcher")]
    [SerializeField] protected Transform muzzle;
    [SerializeField] protected Transform aimPivot;
    [SerializeField] private HeldSpawnJuicePlayer heldSpawnJuice;

    [Header("Aim Strategy")]
    [SerializeField] protected AimStrategy aimStrategy;

    [Header("Speed")]
    [SerializeField] protected float minSpeed = 3f;
    [SerializeField] protected float maxSpeed = 12f;
    [SerializeField] protected AnimationCurve powerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Spawn")]
    [SerializeField] protected float baseSpawnDelay = 0.35f;
    [SerializeField] protected int spawnLevelMin = 0;
    [SerializeField] protected int spawnLevelMax = 2;

    [Header("Preview Queue")]
    [SerializeField] protected int previewCount = 4;

    [Header("Hold")]
    [SerializeField] protected bool enableHold = true;

    [Header("Blocked Check")]
    [SerializeField] protected LayerMask fruitMask;
    [SerializeField] protected float overlapRadius = 0.25f;

    [Header("Tap-to-Aim")]
    [SerializeField] protected float tapRadiusPixels = 90f;

    [Header("Aim Smoothing")]
    [SerializeField] protected float aimSmoothing = 0.06f;

    [Header("Pull Visual")]
    [SerializeField] private LineRenderer pullLine;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color maxColor = Color.red;
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float maxPullDistance = 5f;
    [SerializeField] private float maxPulseSpeed = 8f;
    [SerializeField] private float maxPulseAmount = 0.15f;

    protected Fruit heldFruit;
    protected int heldLevel = -1;

    protected readonly Queue<int> nextQueue = new();

    protected bool spawnQueued;
    protected Coroutine spawnRoutine;
    protected bool isAiming;
    protected bool gameOver;

    private Vector2 currentPointerScreen;
    private Vector2 currentPointerWorld;

    protected Vector2 smoothedPointerWorld;
    protected Vector2 pointerVel;
    protected bool pointerInit;

    private readonly Collider2D[] overlapResults = new Collider2D[8];

    protected virtual void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    protected virtual void OnEnable()
    {
        pointAction.action.Enable();
        pressAction.action.Enable();

        pressAction.action.performed += OnPressPerformed;
        pressAction.action.canceled += OnPressCanceled;

        GameSignals.GameOver += OnGameOver;
    }

    protected virtual void OnDisable()
    {
        pressAction.action.performed -= OnPressPerformed;
        pressAction.action.canceled -= OnPressCanceled;

        pointAction.action.Disable();
        pressAction.action.Disable();

        GameSignals.GameOver -= OnGameOver;
    }

    protected virtual void Start()
    {
        InitQueue();
        SpawnHeldFromQueue();
        UpdateAimAndPreview();
    }

    protected virtual void Update()
    {
        if (gameOver) return;

        currentPointerScreen = pointAction.action.ReadValue<Vector2>();
        currentPointerWorld = ScreenToWorld(currentPointerScreen);

        UpdateAimAndPreview();
    }

    // ✅ FIXED: direct pointer read here
    protected virtual void OnPressPerformed(InputAction.CallbackContext ctx)
    {
        if (gameOver || !heldFruit || spawnQueued || !muzzle || !cam)
            return;

        Vector2 pointerScreen = pointAction.action.ReadValue<Vector2>();
        Vector2 muzzleScreen = cam.WorldToScreenPoint(muzzle.position);

        if ((pointerScreen - muzzleScreen).sqrMagnitude >
            tapRadiusPixels * tapRadiusPixels)
            return;

        isAiming = true;
        pointerInit = false;
    }

    protected virtual void OnPressCanceled(InputAction.CallbackContext ctx)
    {
        if (!isAiming) return;

        isAiming = false;
        pointerInit = false;

        TryReleaseHeld();
    }

    protected virtual void UpdateAimAndPreview()
    {
        if (!muzzle || !aimStrategy) return;

        if (!pointerInit)
        {
            smoothedPointerWorld = currentPointerWorld;
            pointerVel = Vector2.zero;
            pointerInit = true;
        }

        smoothedPointerWorld = Vector2.SmoothDamp(
            smoothedPointerWorld,
            currentPointerWorld,
            ref pointerVel,
            aimSmoothing
        );

        AimResult a = aimStrategy.Evaluate(
            muzzle.position,
            smoothedPointerWorld,
            isAiming,
            heldLevel
        );

        if (aimPivot)
        {
            float ang = Mathf.Atan2(a.dir.y, a.dir.x) * Mathf.Rad2Deg;
            aimPivot.rotation = Quaternion.Euler(0, 0, ang);
        }

        if (heldFruit)
            heldFruit.transform.position = muzzle.position;

        UpdatePullLine(smoothedPointerWorld, a);

        UpdatePreviewVisual(a);
    }

    protected abstract void UpdatePreviewVisual(AimResult a);

    private void UpdatePullLine(Vector2 pointerWorld, AimResult a)
    {
        if (!pullLine) return;

        if (!isAiming)
        {
            pullLine.enabled = false;
            return;
        }

        pullLine.enabled = true;

        Vector2 start = muzzle.position;
        Vector2 drag = pointerWorld - start;

        float dist = drag.magnitude;
        float clampedDist = Mathf.Min(dist, maxPullDistance);
        Vector2 end = start + drag.normalized * clampedDist;

        pullLine.positionCount = 2;
        pullLine.SetPosition(0, start);
        pullLine.SetPosition(1, end);

        pullLine.startWidth = lineWidth;
        pullLine.endWidth = lineWidth;

        Color c = Color.Lerp(normalColor, maxColor, a.power01);

        if (a.power01 >= 0.99f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * maxPulseSpeed) * maxPulseAmount;
            pullLine.startWidth = lineWidth * pulse;
            pullLine.endWidth = lineWidth * pulse;
            c = maxColor;
        }

        pullLine.startColor = c;
        pullLine.endColor = c;
    }

    protected virtual void TryReleaseHeld()
    {
        if (gameOver || !heldFruit || spawnQueued)
            return;

        if (IsMuzzleBlockedByOtherFruit())
            return;

        AimResult a = aimStrategy.Evaluate(
            muzzle.position,
            smoothedPointerWorld,
            true,
            heldLevel
        );

        float shaped = powerCurve != null ?
            powerCurve.Evaluate(a.power01) :
            a.power01;

        float speed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(shaped));

        Fruit shot = heldFruit;

        heldFruit.GetComponent<CircleCollider2D>().enabled = true;
        Destroy(heldFruit.GetComponent<MMAutoRotate>());

        heldFruit = null;

        Rigidbody2D rb = shot.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearVelocity = a.dir * speed;

        GameSignals.RaiseFruitDropped(shot);

        spawnQueued = true;
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnAfterDelay(baseSpawnDelay));
    }

    protected bool IsMuzzleBlockedByOtherFruit()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            muzzle.position,
            overlapRadius,
            overlapResults,
            fruitMask
        );

        for (int i = 0; i < count; i++)
        {
            var col = overlapResults[i];
            if (!col) continue;

            if (heldFruit && col.transform == heldFruit.transform)
                continue;

            if (heldFruit && col.transform.IsChildOf(heldFruit.transform))
                continue;

            return true;
        }

        return false;
    }

    protected IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnHeldFromQueue();
    }

    protected void InitQueue()
    {
        nextQueue.Clear();

        for (int i = 0; i < previewCount; i++)
            nextQueue.Enqueue(Random.Range(spawnLevelMin, spawnLevelMax + 1));

        GameSignals.RaiseNextFruitQueueChanged(nextQueue);
    }

    protected int ConsumeNext()
    {
        int level = nextQueue.Dequeue();
        nextQueue.Enqueue(Random.Range(spawnLevelMin, spawnLevelMax + 1));
        GameSignals.RaiseNextFruitQueueChanged(nextQueue);
        return level;
    }

    protected void SpawnHeldFromQueue()
    {
        spawnQueued = false;
        SpawnHeldSpecific(ConsumeNext());
        isAiming = false;
        pointerInit = false;
    }

    protected void SpawnHeldSpecific(int level)
    {
        if (!database || !muzzle) return;

        Fruit prefab = database.GetPrefab(level);
        if (!prefab) return;

        heldFruit = Instantiate(prefab, muzzle.position, Quaternion.identity);
        heldLevel = level;

        heldFruit.GetComponent<CircleCollider2D>().enabled = false;

        if (heldSpawnJuice != null)
            heldSpawnJuice.PlayOn(heldFruit.transform);

        Rigidbody2D rb = heldFruit.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    protected void DestroyHeldImmediate()
    {
        if (!heldFruit) return;
        Destroy(heldFruit.gameObject);
        heldFruit = null;
        heldLevel = -1;
    }

    protected virtual void OnGameOver()
    {
        gameOver = true;
        DestroyHeldImmediate();
        enabled = false;
    }

    protected Vector2 ScreenToWorld(Vector2 screenPos)
    {
        float z = -cam.transform.position.z;
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        return wp;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!muzzle || !cam) return;

        float z = -cam.transform.position.z;

        Vector3 muzzleScreen = cam.WorldToScreenPoint(muzzle.position);
        Vector3 edgeScreen = muzzleScreen + new Vector3(tapRadiusPixels, 0f, 0f);

        Vector3 worldCenter = cam.ScreenToWorldPoint(new Vector3(muzzleScreen.x, muzzleScreen.y, z));
        Vector3 worldEdge = cam.ScreenToWorldPoint(new Vector3(edgeScreen.x, edgeScreen.y, z));

        float worldTapRadius = Vector3.Distance(worldCenter, worldEdge);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(muzzle.position, worldTapRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(muzzle.position, overlapRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(muzzle.position, 5f);
    }
#endif
}