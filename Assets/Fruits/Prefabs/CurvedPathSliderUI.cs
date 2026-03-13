using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MoreMountains.Tools;

public class CurvedAudioSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public enum AudioSettingType
    {
        Music,
        Sfx
    }

    [Header("Type")]
    [SerializeField] private AudioSettingType settingType = AudioSettingType.Music;

    [Header("References")]
    [SerializeField] private RectTransform hitArea;
    [SerializeField] private RectTransform handle;
    [SerializeField] private RectTransform pathRoot;

    [Header("Initial Value")]
    [SerializeField, Range(0f, 1f)] private float fallbackValue = 0.8f;

    [Header("Runtime")]
    [SerializeField, Range(0f, 1f)] private float currentValue = 0.8f;

    private readonly List<RectTransform> _pathPoints = new();
    private Camera _uiCamera;

    public float Value => currentValue;
    public event Action<float> OnValueChanged;

    private void Awake()
    {
        CacheCanvasCamera();
        CachePathPoints();

        float startValue = GetInitialValueFromSoundManager();
        SetValue(startValue, true);
    }

    private void OnEnable()
    {
        StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;

        if (hitArea != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(hitArea);
        }

        if (pathRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(pathRoot);
        }

        if (handle != null && handle.parent is RectTransform parent)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        CacheCanvasCamera();
        CachePathPoints();
        UpdateHandlePosition();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CachePathPoints();
            UpdateHandlePosition();
        }
    }
#endif

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateFromPointer(eventData);
    }

    public void SetValue(float newValue, bool silent = false)
    {
        currentValue = Mathf.Clamp01(newValue);
        UpdateHandlePosition();
        ApplyVolume(currentValue);

        if (!silent)
        {
            OnValueChanged?.Invoke(currentValue);
        }
    }

    public void RefreshVisual()
    {
        CacheCanvasCamera();
        CachePathPoints();
        UpdateHandlePosition();
    }

    private void CacheCanvasCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        _uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;
    }

    private void CachePathPoints()
    {
        _pathPoints.Clear();

        if (pathRoot == null)
            return;

        for (int i = 0; i < pathRoot.childCount; i++)
        {
            if (pathRoot.GetChild(i) is RectTransform rt)
            {
                _pathPoints.Add(rt);
            }
        }
    }

    private void UpdateFromPointer(PointerEventData eventData)
    {
        if (hitArea == null || _pathPoints.Count < 2)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                hitArea,
                eventData.position,
                _uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float bestGlobalT = 0f;
        float bestDistSq = float.MaxValue;

        int segmentCount = _pathPoints.Count - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 a = GetPointInHitAreaSpace(_pathPoints[i]);
            Vector2 b = GetPointInHitAreaSpace(_pathPoints[i + 1]);

            Vector2 closest = ClosestPointOnSegment(localPoint, a, b, out float segmentT);
            float distSq = (localPoint - closest).sqrMagnitude;

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;

                float segmentStartT = i / (float)segmentCount;
                float segmentSize = 1f / segmentCount;
                bestGlobalT = segmentStartT + segmentT * segmentSize;
            }
        }

        SetValue(bestGlobalT);
    }

    private void UpdateHandlePosition()
    {
        if (handle == null || _pathPoints.Count < 2)
            return;

        int segmentCount = _pathPoints.Count - 1;
        float scaled = currentValue * segmentCount;
        int index = Mathf.FloorToInt(scaled);

        if (index >= segmentCount)
        {
            handle.anchoredPosition = GetPointInHandleParentSpace(_pathPoints[_pathPoints.Count - 1]);
            return;
        }

        float localT = scaled - index;

        Vector2 a = GetPointInHandleParentSpace(_pathPoints[index]);
        Vector2 b = GetPointInHandleParentSpace(_pathPoints[index + 1]);

        handle.anchoredPosition = Vector2.Lerp(a, b, localT);
    }

    private Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        Vector2 ab = b - a;
        float abLengthSq = ab.sqrMagnitude;

        if (abLengthSq <= Mathf.Epsilon)
        {
            t = 0f;
            return a;
        }

        t = Vector2.Dot(p - a, ab) / abLengthSq;
        t = Mathf.Clamp01(t);

        return a + ab * t;
    }

    private Vector2 GetPointInHitAreaSpace(RectTransform point)
    {
        Vector3 worldPos = point.TransformPoint(point.rect.center);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hitArea,
            screenPos,
            _uiCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private Vector2 GetPointInHandleParentSpace(RectTransform point)
    {
        if (!(handle.parent is RectTransform parent))
            return Vector2.zero;

        Vector3 worldPos = point.TransformPoint(point.rect.center);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            screenPos,
            _uiCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private float GetInitialValueFromSoundManager()
    {
        if (MMSoundManager.Instance == null)
            return fallbackValue;

        switch (settingType)
        {
            case AudioSettingType.Music:
                return Mathf.Clamp01(
                    MMSoundManager.Instance.GetTrackVolume(
                        MMSoundManager.MMSoundManagerTracks.Music,
                        false));

            case AudioSettingType.Sfx:
                return Mathf.Clamp01(
                    MMSoundManager.Instance.GetTrackVolume(
                        MMSoundManager.MMSoundManagerTracks.Sfx,
                        false));

            default:
                return fallbackValue;
        }
    }

    private void ApplyVolume(float value)
    {
        if (MMSoundManager.Instance == null)
            return;

        switch (settingType)
        {
            case AudioSettingType.Music:
                MMSoundManager.Instance.SetVolumeMusic(value);
                break;

            case AudioSettingType.Sfx:
                MMSoundManager.Instance.SetVolumeSfx(value);
                break;
        }
    }

    [ContextMenu("Refresh Path Points")]
    private void RefreshPathPoints()
    {
        CachePathPoints();
        UpdateHandlePosition();
    }
}