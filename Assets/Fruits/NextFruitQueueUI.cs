using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class NextFruitQueueUI : MonoBehaviour
{
    [SerializeField] private FruitDatabase database;

    [Header("UI")]
    [SerializeField] private RectTransform container; // has HorizontalLayoutGroup
    [SerializeField] private Image slotPrefab;

    [Header("Highlight")]
    [SerializeField] private float firstScale = 1.18f;
    [SerializeField] private float otherScale = 1.0f;
    [SerializeField, Range(0f, 1f)] private float firstAlpha = 1.0f;
    [SerializeField, Range(0f, 1f)] private float alphaFalloff = 0.15f;

    [Header("Scroll Animation (move container)")]
    [Tooltip("How far the row shifts each update. Set to SlotWidth + LayoutGroup spacing.")]
    [SerializeField] private float scrollPixels = 64f;
    [SerializeField] private float scrollDuration = 0.15f;
    [SerializeField] private Ease scrollEase = Ease.OutCubic;
    [Tooltip("If true, new row appears as if it shifted from the right.")]
    [SerializeField] private bool scrollFromRight = true;

    [Header("Punch")]
    [SerializeField] private float punchAmount = 0.12f;
    [SerializeField] private float punchDuration = 0.18f;
    [SerializeField] private float punchElasticity = 0.5f;

    private readonly List<Image> slots = new();
    private bool hasInitialized;

    private void Awake()
    {
        if (!container) container = (RectTransform)transform;
    }

    private void OnEnable()
    {
        GameSignals.NextFruitQueueChanged += OnQueueChanged;
    }

    private void OnDisable()
    {
        GameSignals.NextFruitQueueChanged -= OnQueueChanged;
    }

    private void OnQueueChanged(IReadOnlyList<int> queue)
    {
        if (queue == null) return;

        EnsureSlots(queue.Count);

        // Kill tweens on slots + container so spam updates don't stack
        container.DOKill(true);
        for (int i = 0; i < slots.Count; i++)
        {
            var img = slots[i];
            if (!img) continue;
            img.rectTransform.DOKill(true);
            img.DOKill(true);
        }

        // Update sprites
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < queue.Count)
            {
                Sprite s = database ? database.GetIcon(queue[i]) : null;
                slots[i].sprite = s;
                slots[i].enabled = (s != null);
            }
            else
            {
                slots[i].enabled = false;
            }
        }

        // Force layout so positions are correct before we animate the container
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        ApplyHighlight(queue.Count);

        // First time: just show, no scroll
        if (!hasInitialized)
        {
            hasInitialized = true;
            PunchFirst(queue.Count);
            return;
        }

        // Scroll effect: offset container then tween back to base
        Vector2 basePos = container.anchoredPosition;
        float dir = scrollFromRight ? 1f : -1f;

        container.anchoredPosition = basePos + new Vector2(scrollPixels * dir, 0f);
        container.DOAnchorPos(basePos, scrollDuration)
                 .SetEase(scrollEase)
                 .SetUpdate(true);

        PunchFirst(queue.Count);
    }

    private void EnsureSlots(int needed)
    {
        while (slots.Count < needed)
        {
            Image img = Instantiate(slotPrefab, container);
            img.enabled = false;
            img.rectTransform.localScale = Vector3.one;
            slots.Add(img);
        }
    }

    private void ApplyHighlight(int count)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var img = slots[i];
            if (!img) continue;

            if (i >= count)
            {
                img.enabled = false;
                continue;
            }

            float scale = (i == 0) ? firstScale : otherScale;
            float alpha = (i == 0)
                ? firstAlpha
                : Mathf.Clamp01(firstAlpha - alphaFalloff * i);

            img.rectTransform.localScale = Vector3.one * scale;

            Color c = img.color;
            c.a = alpha;
            img.color = c;

            img.enabled = true;
        }
    }

    private void PunchFirst(int count)
    {
        if (count <= 0) return;
        var img = slots[0];
        if (!img || !img.enabled) return;

        var rt = img.rectTransform;
        rt.DOKill(true);

        rt.localScale = Vector3.one * firstScale;

        rt.DOPunchScale(
            Vector3.one * punchAmount,
            punchDuration,
            1,
            punchElasticity
        ).SetUpdate(true);
    }
}