using System;
using UnityEngine;
using UnityEngine.Advertisements;

public class AdsManager : MonoBehaviour,
    IUnityAdsInitializationListener,
    IUnityAdsLoadListener,
    IUnityAdsShowListener
{
    public static AdsManager I { get; private set; }

    [Header("Unity Dashboard Game IDs")]
    [SerializeField] private string androidGameId = "6058549";
    [SerializeField] private string iosGameId = "6058548";

    [Header("Ad Unit IDs")]
    [SerializeField] private string rewardedAndroid = "Rewarded_Android";
    [SerializeField] private string rewardedIOS = "Rewarded_iOS";

    [Header("Settings")]
    [SerializeField] private bool testMode = true;

    [Header("Editor testing")]
    [Tooltip("If true, in the Unity Editor we instantly grant the reward instead of showing ads.")]
    [SerializeField] private bool editorInstantReward = true;

    private string rewardedId;
    private bool initialized;
    private bool rewardedLoaded;
    private Action onReward;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID
        string gameId = androidGameId;
        rewardedId = rewardedAndroid;
#elif UNITY_IOS
        string gameId = iosGameId;
        rewardedId = rewardedIOS;
#else
        // Editor: pick one so code paths work
        string gameId = androidGameId;
        rewardedId = rewardedAndroid;
#endif
        Advertisement.Initialize(gameId, testMode, this);
    }

    public bool IsRewardedReady()
    {
#if UNITY_EDITOR
        return true;
#else
        return initialized && rewardedLoaded;
#endif
    }

    public void ShowRewarded(Action rewardCallback)
    {
#if UNITY_EDITOR
        if (editorInstantReward)
        {
            rewardCallback?.Invoke();
            return;
        }
#endif

        if (!initialized)
        {
            Debug.LogWarning("Ads not initialized yet.");
            return;
        }

        if (!rewardedLoaded)
        {
            Debug.LogWarning($"Rewarded not loaded yet: {rewardedId}. Loading now...");
            LoadRewarded();
            return;
        }

        onReward = rewardCallback;
        rewardedLoaded = false; // we'll reload after show
        Advertisement.Show(rewardedId, this);
    }

    private void LoadRewarded()
    {
        if (!initialized) return;
        Advertisement.Load(rewardedId, this);
    }

    // -------- Initialization callbacks --------
    public void OnInitializationComplete()
    {
        initialized = true;
        LoadRewarded();
        Debug.Log("Unity Ads initialized.");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Ads init failed: {error} - {message}");
    }

    // -------- Load callbacks --------
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId == rewardedId)
        {
            rewardedLoaded = true;
            Debug.Log($"Rewarded loaded: {adUnitId}");
        }
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        if (adUnitId == rewardedId)
        {
            rewardedLoaded = false;
            Debug.LogError($"Rewarded failed to load: {error} - {message}");
        }
    }

    // -------- Show callbacks --------
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState state)
    {
        if (adUnitId != rewardedId) return;

        if (state == UnityAdsShowCompletionState.COMPLETED)
            onReward?.Invoke();

        onReward = null;

        // Preload the next one
        LoadRewarded();
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        if (adUnitId != rewardedId) return;

        Debug.LogError($"Show failed: {error} - {message}");
        onReward = null;

        // Try to load again
        LoadRewarded();
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }
}