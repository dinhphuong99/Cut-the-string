// Assets/Scripts/AnimationSystem/Runtime/AnimationCoordinator.cs
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MonoBehaviour))]
public class AnimationCoordinator : MonoBehaviour
{
    [Header("Database")]
    public AnimationDatabase database;
    public string characterId = "Hero";
    public float defaultCrossfade = 0.08f;

    [Header("Adapter")]
    public MonoBehaviour adapterBehaviour; // assign a component that implements IAnimationAdapter

    private IAnimationAdapter _adapter;
    private IAnimationClipLoader _loader;
    private int _requestVersion = 0;
    private AnimationKey _currentKey;

    void Awake()
    {
        _adapter = adapterBehaviour as IAnimationAdapter;
        if (_adapter == null) Debug.LogError("adapterBehaviour must implement IAnimationAdapter.", this);
        if (database == null) Debug.LogError("AnimationCoordinator requires database assigned.", this);
        else database.RebuildCache();
        _loader = CreateLoader();
    }

    private IAnimationClipLoader CreateLoader()
    {
#if ENABLE_ADDRESSABLES
        return new AddressablesAnimationClipLoader();
#else
        return new DefaultAnimationClipLoader();
#endif
    }

    public void RequestIntent(AnimationIntent intent)
    {
        _ = RequestIntentAsync(intent);
    }

    public async Task RequestIntentAsync(AnimationIntent intent)
    {
        if (!intent.IsValid) return;
        if (_adapter == null || database == null) return;

        var key = new AnimationKey(characterId, intent.intent, intent.variant);
        if (!database.TryGetClipRef(key, out var clipRef))
        {
            Debug.LogWarning($"[{name}] Missing DB entry for key {key.ToId()}", this);
            return;
        }

        int myVersion = ++_requestVersion;

        var clip = await _loader.Load(clipRef);

        if (myVersion != _requestVersion) return; // newer request arrived, drop
        if (clip == null)
        {
            Debug.LogWarning($"[{name}] Clip load failed for key {key.ToId()} ({clipRef})", this);
            return;
        }

        float clipLen = clip.length;
        float target = intent.targetDuration > 0f ? intent.targetDuration : clipLen;
        float speed = (clipLen > 0.0001f) ? (clipLen / Mathf.Max(0.00001f, target)) : 1f;
        speed *= Mathf.Max(0.0001f, intent.speedMultiplier);

        _adapter.PlayClip(clip, speed, defaultCrossfade, upperBody: false);

        _currentKey = key;
    }

    public void CancelCurrent()
    {
        _requestVersion++; // bump to cancel pending loads usage
        _adapter?.HardStop();
    }

    private void OnDestroy()
    {
        // nothing to release here; loader can be disposed if needed
    }
}