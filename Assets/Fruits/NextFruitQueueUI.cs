using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class NextFruitQueueUI : MonoBehaviour
{
    [SerializeField] private FruitDatabase database;

    [Header("UI")]
    [SerializeField] private RectTransform container; // has Layout Group
    [SerializeField] private Image slotPrefab;

    [Header("Highlight")]
    [SerializeField] private float firstScale = 1.18f;
    [SerializeField] private float otherScale = 1.0f;
    [SerializeField, Range(0f, 1f)] private float firstAlpha = 1.0f;
    [SerializeField, Range(0f, 1f)] private float alphaFalloff = 0.15f;

    [Header("Animation")]
    [SerializeField] private float slidePixels = 28f;
    [SerializeField] private float slideDuration = 0.15f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    private readonly List<Image> slots = new();

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

        // Kill tweens so spam updates don’t stack
        foreach (var img in slots)
        {
            if (!img) continue;
            img.rectTransform.DOKill();
            img.DOKill();
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

        ApplyHighlight(queue.Count);

        if (queue.Count > 0 && slots[0].enabled)
        {
            var rt = slots[0].rectTransform;

            // reset scale so punch is consistent
            rt.localScale = Vector3.one * firstScale;

            rt.DOPunchScale(
                Vector3.one * 0.12f, // amount (tune 0.08–0.15)
                0.18f,               // duration
                1,                   // vibrato (1 = clean pop)
                0.5f                 // elasticity (lower = snappier)
            ).SetUpdate(true);
        }

        AnimateScroll(queue.Count);
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

            float scale = (i == 0) ? firstScale : otherScale;
            float alpha = (i == 0)
                ? firstAlpha
                : Mathf.Clamp01(firstAlpha - alphaFalloff * i);

            img.rectTransform.localScale = Vector3.one * scale;

            Color c = img.color;
            c.a = alpha;
            img.color = c;

            if (i >= count) img.enabled = false;
        }
    }

    private void AnimateScroll(int count)
    {
        // Force layout so anchored positions are correct
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        for (int i = 0; i < count; i++)
        {
            var rt = slots[i].rectTransform;

            Vector2 target = rt.anchoredPosition;
            Vector2 start = target - new Vector2(0f, slidePixels);

            rt.anchoredPosition = start;
            rt.DOAnchorPos(target, slideDuration)
              .SetEase(slideEase)
              .SetUpdate(true); // UI should animate even if timescale = 0
        }
    }
}
