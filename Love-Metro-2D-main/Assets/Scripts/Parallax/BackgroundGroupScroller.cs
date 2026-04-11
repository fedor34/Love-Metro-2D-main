using UnityEngine;

public class BackgroundGroupScroller : MonoBehaviour
{
    private enum ResolutionMode
    {
        Unresolved,
        Group,
        Fallback
    }

    [Header("Target Group")]
    [SerializeField] private string _groupName = "Background";
    [SerializeField] private string[] _fallbackNodeNames = new[]
    {
        "6_\u0433\u043e\u0440\u043e\u0434_\u0444\u043e\u043d",
        "5_\u0433\u043e\u0440\u043e\u0434_\u0434\u0430\u043b\u044c\u043d\u0438\u0439",
        "4_\u0433\u043e\u0440\u043e\u0434_\u0441\u0440\u0435\u0434\u043d\u0438\u0439",
        "3_\u0433\u043e\u0440\u043e\u0434_\u0431\u043b\u0438\u0436\u043d\u0438\u0439",
        "2_\u0433\u043e\u0440\u043e\u0434_\u0434\u0435\u0440\u0435\u0432\u044c\u044f",
        "1_\u0433\u043e\u0440\u043e\u0434_\u0440\u0435\u043b\u044c\u0441\u044b"
    };

    [Header("Speed Scaling")]
    [SerializeField] private float _linearFactor = 30f;
    [SerializeField] private float _quadraticFactor = 2.0f;
    [SerializeField] private float _extraMultiplier = 20f;
    [SerializeField] private Vector2 _scrollDirection = Vector2.left;

    [Header("Dependencies")]
    [SerializeField] private TrainManager _train;
    [SerializeField] private float _resolveRetryInterval = 1.0f;

    private Transform _group;
    private Transform[] _fallbackNodes;
    private float _nextResolveTime;
    private ResolutionMode _lastLoggedMode = ResolutionMode.Unresolved;

    private void Awake()
    {
        ResolveTargets(force: true);
    }

    private void Update()
    {
        if (!ResolveTargets())
            return;

        Vector3 delta = BuildScrollDelta(
            Mathf.Abs(_train.GetCurrentSpeed()),
            _linearFactor,
            _quadraticFactor,
            _extraMultiplier,
            _scrollDirection,
            Time.deltaTime);

        if (delta.sqrMagnitude <= 0.000001f)
            return;

        if (_group != null)
        {
            _group.position += delta;
            return;
        }

        ApplyDeltaToFallbackNodes(delta);
    }

    private bool ResolveTargets(bool force = false)
    {
        if (!force && Time.time < _nextResolveTime)
            return _train != null && (_group != null || HasFallbackNodes());

        if (_train == null)
            _train = FindObjectOfType<TrainManager>();

        ResolveGroupOrFallbackNodes();
        _nextResolveTime = Time.time + Mathf.Max(0.1f, _resolveRetryInterval);
        return _train != null && (_group != null || HasFallbackNodes());
    }

    private void ResolveGroupOrFallbackNodes()
    {
        _group = ResolveGroup(_groupName);
        if (_group != null)
        {
            MarkAsDynamic(_group.gameObject);
            LogResolutionMode(ResolutionMode.Group);
            return;
        }

        _fallbackNodes = ResolveFallbackNodes(_fallbackNodeNames);
        if (HasFallbackNodes())
        {
            LogResolutionMode(ResolutionMode.Fallback);
        }
        else
        {
            _lastLoggedMode = ResolutionMode.Unresolved;
        }
    }

    private static Transform ResolveGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return null;

        GameObject groupObject = GameObject.Find(groupName);
        return groupObject != null ? groupObject.transform : null;
    }

    private static Transform[] ResolveFallbackNodes(string[] nodeNames)
    {
        if (nodeNames == null || nodeNames.Length == 0)
            return null;

        var resolved = new Transform[nodeNames.Length];
        for (int i = 0; i < nodeNames.Length; i++)
        {
            GameObject nodeObject = GameObject.Find(nodeNames[i]);
            if (nodeObject == null)
                continue;

            MarkAsDynamic(nodeObject);
            resolved[i] = nodeObject.transform;
        }

        return resolved;
    }

    private void ApplyDeltaToFallbackNodes(Vector3 delta)
    {
        if (_fallbackNodes == null)
            return;

        for (int i = 0; i < _fallbackNodes.Length; i++)
        {
            Transform fallbackNode = _fallbackNodes[i];
            if (fallbackNode != null)
                fallbackNode.position += delta;
        }
    }

    private bool HasFallbackNodes()
    {
        if (_fallbackNodes == null || _fallbackNodes.Length == 0)
            return false;

        for (int i = 0; i < _fallbackNodes.Length; i++)
        {
            if (_fallbackNodes[i] != null)
                return true;
        }

        return false;
    }

    private static void MarkAsDynamic(GameObject gameObject)
    {
        if (gameObject != null && gameObject.isStatic)
            gameObject.isStatic = false;
    }

    private static Vector3 BuildScrollDelta(
        float speed,
        float linearFactor,
        float quadraticFactor,
        float extraMultiplier,
        Vector2 scrollDirection,
        float deltaTime)
    {
        if (speed <= 0f || deltaTime <= 0f || scrollDirection.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        float boost = (Mathf.Max(0f, linearFactor) * speed + Mathf.Max(0f, quadraticFactor) * speed * speed) *
                      Mathf.Max(0f, extraMultiplier);
        return (Vector3)(scrollDirection.normalized * boost * deltaTime);
    }

    private void LogResolutionMode(ResolutionMode mode)
    {
        if (_lastLoggedMode == mode)
            return;

        _lastLoggedMode = mode;
        if (mode == ResolutionMode.Group)
        {
            Diagnostics.Log($"[BackgroundGroupScroller] Using group '{_groupName}'.");
            return;
        }

        if (mode == ResolutionMode.Fallback)
            Diagnostics.Log("[BackgroundGroupScroller] Group not found. Using fallback background nodes.");
    }
}
