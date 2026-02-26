// FruitLauncherBase.cs
using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] protected Transform aimPivot;   // optional
    [SerializeField] private HeldSpawnJuicePlayer heldSpawnJuice;

    [Header("Aim Strategy")]
    [SerializeField] protected AimStrategy aimStrategy;

    [Header("Speed (Option A)")]
    [SerializeField] protected float minSpeed = 3f;
    [SerializeField] protected float maxSpeed = 12f;
    [SerializeField] protected AnimationCurve powerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Gravity Inversion")]
    [Tooltip("Default gravity scale to restore when not inverted (0 = keep prefab's default).")]
    [SerializeField] protected float defaultGravityScale = 0f;

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

    [Header("Tap-to-Aim (Screen Space)")]
    [Tooltip("Player must press within this many pixels of the muzzle to start aiming.")]
    [SerializeField] protected float tapRadiusPixels = 90f;

    [Header("Aim Smoothing (Mobile)")]
    [Tooltip("Smooths finger jitter. 0.03–0.10 is typical.")]
    [SerializeField] protected float aimSmoothing = 0.06f;

    // held
    protected Fruit heldFruit;
    protected int heldLevel = -1;

    // hold slot
    protected int holdSlotLevel = -1;
    protected bool holdUsedThisCycle;

    // queue
    protected readonly Queue<int> nextQueue = new();

    // state
    protected bool spawnQueued;
    protected Coroutine spawnRoutine;
    protected bool isAiming;
    protected bool gameOver;

    // smoothing
    protected Vector2 smoothedPointerWorld;
    protected Vector2 pointerVel;
    protected bool pointerInit;

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
        UpdateAimAndPreview();
    }

    // ----- public (UI) -----
    public void HoldOrSwap()
    {
        if (gameOver) return;
        if (!enableHold) return;
        if (!heldFruit) return;
        if (spawnQueued) return;
        if (holdUsedThisCycle) return;

        int current = heldLevel;

        if (holdSlotLevel < 0)
        {
            holdSlotLevel = current;
            GameSignals.RaiseHoldChanged(holdSlotLevel);

            DestroyHeldImmediate();
            SpawnHeldFromQueue();
            holdUsedThisCycle = true;
            return;
        }

        int tmp = holdSlotLevel;
        holdSlotLevel = current;
        GameSignals.RaiseHoldChanged(holdSlotLevel);

        DestroyHeldImmediate();
        SpawnHeldSpecific(tmp);
        holdUsedThisCycle = true;
    }

    // ----- input -----
    protected virtual void OnPressPerformed(InputAction.CallbackContext ctx)
    {
        if (gameOver) return;
        if (!heldFruit) return;
        if (spawnQueued) return;
        if (!muzzle || !cam) return;

        Vector2 pointerScreen = pointAction.action.ReadValue<Vector2>();
        Vector2 muzzleScreen = cam.WorldToScreenPoint(muzzle.position);

        if ((pointerScreen - muzzleScreen).sqrMagnitude > tapRadiusPixels * tapRadiusPixels)
            return;

        isAiming = true;

        // reset smoothing so it doesn't lerp from old touch
        pointerInit = false;
    }

    protected virtual void OnPressCanceled(InputAction.CallbackContext ctx)
    {
        if (!isAiming) return;
        isAiming = false;

        // reset smoothing for next time
        pointerInit = false;

        TryReleaseHeld();
    }

    // ----- aim / preview -----
    protected virtual void UpdateAimAndPreview()
    {
        if (!muzzle || !aimStrategy) return;

        // raw finger -> world
        Vector2 rawPointerWorld = ScreenToWorld(pointAction.action.ReadValue<Vector2>());

        // SmoothDamp to kill jitter
        if (!pointerInit)
        {
            smoothedPointerWorld = rawPointerWorld;
            pointerVel = Vector2.zero;
            pointerInit = true;
        }

        smoothedPointerWorld = Vector2.SmoothDamp(
            smoothedPointerWorld,
            rawPointerWorld,
            ref pointerVel,
            aimSmoothing
        );

        Vector2 pointerWorld = smoothedPointerWorld;

        AimResult a = aimStrategy.Evaluate(muzzle.position, pointerWorld, isAiming, heldLevel);

        if (aimPivot)
        {
            float ang = Mathf.Atan2(a.dir.y, a.dir.x) * Mathf.Rad2Deg;
            aimPivot.rotation = Quaternion.Euler(0, 0, ang);
        }

        if (heldFruit) heldFruit.transform.position = muzzle.position;

        UpdatePreviewVisual(a);
    }

    protected abstract void UpdatePreviewVisual(AimResult a);

    // ----- release -----
    protected virtual void TryReleaseHeld()
    {
        if (gameOver) return;
        if (!heldFruit || spawnQueued) return;

        if (IsMuzzleBlockedByOtherFruit()) return;

        Vector2 pointerWorld = ScreenToWorld(pointAction.action.ReadValue<Vector2>());
        AimResult a = aimStrategy.Evaluate(muzzle.position, pointerWorld, true, heldLevel);

        float shaped = powerCurve != null ? powerCurve.Evaluate(a.power01) : a.power01;
        float speed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(shaped));

        Fruit shot = heldFruit;
        heldFruit.GetComponent<CircleCollider2D>().enabled = true;
        Destroy(heldFruit.GetComponent<MMAutoRotate>());
        //Debug.Log(heldFruit.GetComponent<CircleCollider2D>().enabled);
        heldFruit = null;

        Rigidbody2D rb = shot.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;

        ApplyLaunch(rb, a.dir, speed);

        GameSignals.RaiseFruitDropped(shot);

        spawnQueued = true;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnAfterDelay(GetSpawnDelayFor(shot.level)));
    }

    protected bool IsMuzzleBlockedByOtherFruit()
    {
        var hits = Physics2D.OverlapCircleAll(muzzle.position, overlapRadius, fruitMask);
        if (hits == null || hits.Length == 0) return false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i]) continue;

            if (heldFruit && hits[i].transform == heldFruit.transform)
                continue;

            if (heldFruit && hits[i].transform.IsChildOf(heldFruit.transform))
                continue;

            return true;
        }

        return false;
    }

    protected virtual void ApplyLaunch(Rigidbody2D rb, Vector2 dir, float speed)
    {
        rb.linearVelocity = dir * speed;
    }

    protected virtual float GetSpawnDelayFor(int level) => baseSpawnDelay;

    protected IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnHeldFromQueue();
    }

    // ----- queue/spawn -----
    protected void InitQueue()
    {
        nextQueue.Clear();
        for (int i = 0; i < previewCount; i++)
            nextQueue.Enqueue(RollNextLevel());

        GameSignals.RaiseNextFruitQueueChanged(nextQueue);
        GameSignals.RaiseHoldChanged(holdSlotLevel);
    }

    protected int ConsumeNext()
    {
        int level = nextQueue.Dequeue();
        nextQueue.Enqueue(RollNextLevel());
        GameSignals.RaiseNextFruitQueueChanged(nextQueue);
        return level;
    }

    protected int RollNextLevel() => Random.Range(spawnLevelMin, spawnLevelMax + 1);

    protected void SpawnHeldFromQueue()
    {
        spawnQueued = false;

        int level = ConsumeNext();
        SpawnHeldSpecific(level);

        holdUsedThisCycle = false;
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
        //heldFruit.AddComponent<MMAutoRotate>();
       // MMAutoRotate rotation = heldFruit.GetComponent<MMAutoRotate>();
        //rotation.RotationSpeed

        //Debug.Log(heldFruit.GetComponent<CircleCollider2D>().enabled);

        if (heldSpawnJuice != null)
            heldSpawnJuice.PlayOn(heldFruit.transform);

        heldFruit.level = level;
        heldFruit.database = database;

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

    // ----- game over -----
    protected virtual void OnGameOver()
    {
        gameOver = true;

        DestroyHeldImmediate();

        spawnQueued = false;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;

        enabled = false;
    }

    protected Vector2 ScreenToWorld(Vector2 screenPos)
    {
        float z = -cam.transform.position.z;
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        return wp;
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!muzzle || !cam) return;

        // Convert pixel radius → world radius at muzzle depth
        float z = -cam.transform.position.z;

        Vector3 muzzleScreen = cam.WorldToScreenPoint(muzzle.position);

        // screen-space offset
        Vector3 edgeScreen = muzzleScreen + new Vector3(tapRadiusPixels, 0f, 0f);

        // back to world
        Vector3 worldCenter = cam.ScreenToWorldPoint(new Vector3(muzzleScreen.x, muzzleScreen.y, z));
        Vector3 worldEdge = cam.ScreenToWorldPoint(new Vector3(edgeScreen.x, edgeScreen.y, z));

        float worldRadius = Vector3.Distance(worldCenter, worldEdge);

        // main circle
        Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
        Gizmos.DrawWireSphere(muzzle.position, worldRadius);

        // optional: center dot (nice for clarity)
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(muzzle.position, worldRadius * 0.05f);
    }
#endif
}