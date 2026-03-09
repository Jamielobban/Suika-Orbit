using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonPressTween : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Scale")]
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float tweenDuration = 0.08f;

    [Header("Click Bounce")]
    [SerializeField] private bool punchOnClick = true;
    [SerializeField] private float punchStrength = 0.08f;
    [SerializeField] private float punchDuration = 0.18f;

    private Button button;
    private Vector3 baseScale = Vector3.one;
    private Tween currentTween;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (target == null)
            target = transform as RectTransform;

        if (target != null)
            baseScale = target.localScale;
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);

        if (target != null)
            target.localScale = baseScale;
    }

    private void OnDisable()
    {
        currentTween?.Kill();

        if (button != null)
            button.onClick.RemoveListener(HandleClick);

        if (target != null)
            target.localScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        TweenTo(baseScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        TweenTo(baseScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (target == null) return;
        TweenTo(baseScale);
    }

    private void HandleClick()
    {
        if (!IsInteractable() || target == null) return;

        currentTween?.Kill();

        if (!punchOnClick)
        {
            TweenTo(baseScale);
            return;
        }

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(target.DOScale(baseScale * pressedScale, 0.04f).SetEase(Ease.OutQuad).SetUpdate(true));
        seq.Append(target.DOScale(baseScale, 0.06f).SetEase(Ease.OutQuad).SetUpdate(true));
        seq.Append(target.DOPunchScale(Vector3.one * punchStrength, punchDuration, 8, 0.8f).SetUpdate(true));

        currentTween = seq;
    }

    private void TweenTo(Vector3 scale)
    {
        if (target == null) return;

        currentTween?.Kill();
        currentTween = target
            .DOScale(scale, tweenDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private bool IsInteractable()
    {
        return button != null && button.IsInteractable();
    }
}