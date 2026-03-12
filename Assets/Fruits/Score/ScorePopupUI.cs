using System.Collections.Generic;
using UnityEngine;

public class ScorePopupUI : MonoBehaviour
{
    [Header("Spawn Area")]
    [SerializeField] private RectTransform spawnArea;

    [Header("Prefab / Pool")]
    [SerializeField] private ScorePopupItemUI popupPrefab;
    [SerializeField] private int initialPoolSize = 6;

    [Header("Animation")]
    [SerializeField] private float moveUp = 30f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float startScale = 0.85f;
    [SerializeField] private float peakScale = 1.08f;

    [Header("Random Spawn Offset")]
    [SerializeField] private float randomXRange = 30f;
    [SerializeField] private float randomYRange = 12f;

    [Header("Combo")]
    [SerializeField] private bool showComboOnlyIfAboveOne = true;

    private readonly Queue<ScorePopupItemUI> available = new Queue<ScorePopupItemUI>();
    private readonly List<ScorePopupItemUI> allItems = new List<ScorePopupItemUI>();
    private Vector2 baseAnchoredPos;

    private void Awake()
    {
        if (!spawnArea)
            spawnArea = transform as RectTransform;

        if (spawnArea)
            baseAnchoredPos = spawnArea.anchoredPosition;

        BuildPool();
    }

    private void OnEnable()
    {
        GameSignals.ScoreFeedbackRequested += OnScoreFeedbackRequested;
        GameSignals.RunStarted += OnRunStarted;
    }

    private void OnDisable()
    {
        GameSignals.ScoreFeedbackRequested -= OnScoreFeedbackRequested;
        GameSignals.RunStarted -= OnRunStarted;
    }

    private void OnRunStarted()
    {
        ResetPoolVisuals();
    }

    private void BuildPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
            CreateNewItem();
    }

    private ScorePopupItemUI CreateNewItem()
    {
        ScorePopupItemUI item = Instantiate(popupPrefab, spawnArea.parent);
        item.gameObject.SetActive(false);

        RectTransform itemRect = item.transform as RectTransform;
        if (itemRect)
        {
            itemRect.anchorMin = spawnArea.anchorMin;
            itemRect.anchorMax = spawnArea.anchorMax;
            itemRect.pivot = spawnArea.pivot;
            itemRect.anchoredPosition = baseAnchoredPos;
            itemRect.localScale = Vector3.one;
        }

        allItems.Add(item);
        available.Enqueue(item);
        return item;
    }

    private void OnScoreFeedbackRequested(int gainedPoints, int combo, int newTotal)
    {
        ShowPopup(gainedPoints, combo);
    }

    private void ShowPopup(int gainedPoints, int combo)
    {
        ScorePopupItemUI item = GetItem();

        float randomX = Random.Range(-randomXRange, randomXRange);
        float randomY = Random.Range(-randomYRange, randomYRange);
        Vector2 startPos = baseAnchoredPos + new Vector2(randomX, randomY);

        item.Play(
            gainedPoints,
            combo,
            startPos,
            moveUp,
            duration,
            startScale,
            peakScale,
            showComboOnlyIfAboveOne,
            ReturnToPool
        );
    }

    private ScorePopupItemUI GetItem()
    {
        if (available.Count > 0)
            return available.Dequeue();

        return CreateNewItem();
    }

    private void ReturnToPool(ScorePopupItemUI item)
    {
        if (!available.Contains(item))
            available.Enqueue(item);
    }

    private void ResetPoolVisuals()
    {
        available.Clear();

        for (int i = 0; i < allItems.Count; i++)
        {
            if (!allItems[i]) continue;
            allItems[i].StopAndHide();
            available.Enqueue(allItems[i]);
        }
    }
}