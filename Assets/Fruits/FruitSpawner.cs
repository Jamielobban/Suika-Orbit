using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FruitSpawner : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference pointAction; // Gameplay/Point (<Pointer>/position)
    [SerializeField] private InputActionReference pressAction; // Gameplay/Press (<Pointer>/press)

    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private FruitDatabase database;

    [Header("Spawn Area")]
    [SerializeField] private Transform topY;
    [SerializeField] private float minX = -2.2f;
    [SerializeField] private float maxX = 2.2f;

    [Header("Timing")]
    [SerializeField] private float spawnDelay = 0.35f;

    [Header("Spawn Levels")]
    [SerializeField] private int spawnLevelMin = 0;
    [SerializeField] private int spawnLevelMax = 2;

    [Header("Preview Queue")]
    [SerializeField] private int previewCount = 4;

    [Header("Hold")]
    [Tooltip("If true, player can hold/swap once per drop cycle.")]
    [SerializeField] private bool enableHold = true;

    [Header("Overlap Check")]
    [SerializeField] private LayerMask fruitMask;
    [SerializeField] private float overlapRadius = 0.25f;

    // held fruit (the one you are aiming)
    private Fruit heldFruit;
    private int heldLevel = -1;

    // one-slot hold
    private int holdSlotLevel = -1;     // -1 means empty
    private bool holdUsedThisCycle = false;

    // preview queue
    private readonly Queue<int> nextQueue = new();

    private float targetX;

    // spawn scheduling safety
    private bool spawnQueued;
    private Coroutine spawnRoutine;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    private void OnEnable()
    {
        pointAction.action.Enable();
        pressAction.action.Enable();

        // Drop on release
        pressAction.action.canceled += OnPressCanceled;
    }

    private void OnDisable()
    {
        pressAction.action.canceled -= OnPressCanceled;

        pointAction.action.Disable();
        pressAction.action.Disable();
    }

    private void Start()
    {
        targetX = 0f;
        InitQueue();
        SpawnHeldFromQueue();
    }

    private void Update()
    {
        // Always update targetX so it never “sticks” on spam taps
        Vector2 screenPos = pointAction.action.ReadValue<Vector2>();
        targetX = Mathf.Clamp(ScreenToWorldX(screenPos), minX, maxX);

        if (heldFruit)
        {
            float x = Mathf.Lerp(heldFruit.transform.position.x, targetX, 20f * Time.deltaTime);
            heldFruit.transform.position = new Vector3(x, topY.position.y, 0f);
        }
    }

    // ---------- PUBLIC: call from a UI button ----------
    // Add a UI button on mobile labeled "HOLD" and wire it to this function.
    public void HoldOrSwap()
    {
        if (!enableHold) return;
        if (!heldFruit) return;
        if (spawnQueued) return; // don't allow during spawn delay
        if (holdUsedThisCycle) return;

        int currentLevel = heldLevel;

        // If slot is empty, store current and pull a new one from queue
        if (holdSlotLevel < 0)
        {
            holdSlotLevel = currentLevel;
            GameSignals.RaiseHoldChanged(holdSlotLevel); // you’ll add this to GameSignals if you want UI

            DestroyHeldImmediate();

            // Spawn a new held from queue immediately (does NOT consume a "drop")
            SpawnHeldFromQueue();

            holdUsedThisCycle = true;
            return;
        }

        // Slot has something: swap it with current
        int tmp = holdSlotLevel;
        holdSlotLevel = currentLevel;
        GameSignals.RaiseHoldChanged(holdSlotLevel);

        DestroyHeldImmediate();
        SpawnHeldSpecific(tmp);

        holdUsedThisCycle = true;
    }

    // ---------- DROP ----------
    private void OnPressCanceled(InputAction.CallbackContext ctx)
    {
        TryDropHeld();
    }

    private void TryDropHeld()
    {
        if (!heldFruit || spawnQueued) return;

        bool blocked = Physics2D.OverlapCircle(heldFruit.transform.position, overlapRadius, fruitMask);
        if (blocked) return;

        var dropped = heldFruit;
        heldFruit = null;

        var rb = dropped.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;

        GameSignals.RaiseFruitDropped(dropped);

        // allow hold again only after a real drop
        holdUsedThisCycle = false;

        // schedule next spawn (one only)
        spawnQueued = true;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnAfterDelay(spawnDelay));
    }

    private IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnHeldFromQueue();
    }

    // ---------- QUEUE ----------
    private void InitQueue()
    {
        nextQueue.Clear();
        for (int i = 0; i < previewCount; i++)
            nextQueue.Enqueue(RollNextLevel());

        // Broadcast queue to UI
        GameSignals.RaiseNextFruitQueueChanged(nextQueue);
    }

    private int ConsumeNext()
    {
        int level = nextQueue.Dequeue();
        nextQueue.Enqueue(RollNextLevel());

        GameSignals.RaiseNextFruitQueueChanged(nextQueue);
        return level;
    }

    private int RollNextLevel()
    {
        return Random.Range(spawnLevelMin, spawnLevelMax + 1);
    }

    // ---------- SPAWN ----------
    private void SpawnHeldFromQueue()
    {
        spawnQueued = false;

        int level = ConsumeNext();
        SpawnHeldSpecific(level);

        // New cycle starts: allow hold once for this held fruit
        holdUsedThisCycle = false;
    }

    private void SpawnHeldSpecific(int level)
    {
        Fruit prefab = database.GetPrefab(level);
        if (!prefab)
        {
            Debug.LogError($"No fruit prefab for level {level}");
            return;
        }

        Vector3 pos = new Vector3(targetX, topY.position.y, 0f);
        heldFruit = Instantiate(prefab, pos, Quaternion.identity);

        heldLevel = level;
        heldFruit.level = level;
        heldFruit.database = database;

        //var g = heldFruit.GetComponent<FruitGravityMode>();
        //if (g) g.SetMode(FruitGravityMode.Mode.Normal);

        var rb = heldFruit.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void DestroyHeldImmediate()
    {
        if (!heldFruit) return;
        Destroy(heldFruit.gameObject);
        heldFruit = null;
        heldLevel = -1;
    }

    // ---------- UTIL ----------
    private float ScreenToWorldX(Vector2 screenPos)
    {
        float z = -cam.transform.position.z;
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        return wp.x;
    }

    private void OnDrawGizmosSelected()
    {
        if (!heldFruit) return;
        Gizmos.DrawWireSphere(heldFruit.transform.position, overlapRadius);
    }
}
