using UnityEngine;

public class BackgroundGroupScroller : MonoBehaviour
{
    [Header("Target group")] 
    [SerializeField] private string _groupName = "Background";
    [SerializeField] private string[] _fallbackNodeNames = new[] { "6_город_фон", "5_город_дальний", "4_город_средний", "3_город_ближний", "2_город_деревья", "1_город_рельсы" };

    [Header("Speed scaling")] 
    [SerializeField] private float _linearFactor = 30f;   // сильное линейное усиление
    [SerializeField] private float _quadraticFactor = 2.0f; // s^2 вклад
    [SerializeField] private float _extraMultiplier = 20f;  // общий множитель

    [SerializeField] private Vector2 _scrollDirection = Vector2.left;

    private Transform _group;
    private TrainManager _train;
    private Transform[] _fallbackNodes;

    private void Awake()
    {
        _train = FindObjectOfType<TrainManager>();
        var go = GameObject.Find(_groupName);
        if (go != null)
        {
            _group = go.transform;
            if (_group.gameObject.isStatic) _group.gameObject.isStatic = false;
            Debug.Log("[BackgroundGroupScroller] Using group '" + _groupName + "'.");
        }
        else
        {
            // Fallback to individual nodes
            _fallbackNodes = new Transform[_fallbackNodeNames.Length];
            for (int i = 0; i < _fallbackNodeNames.Length; i++)
            {
                var f = GameObject.Find(_fallbackNodeNames[i]);
                if (f != null)
                {
                    _fallbackNodes[i] = f.transform;
                    if (f.isStatic) f.isStatic = false;
                }
            }
            Debug.Log("[BackgroundGroupScroller] Background group not found. Fallback nodes attached.");
        }
    }

    private void Update()
    {
        if (_train == null) return;
        float s = Mathf.Abs(_train.GetCurrentSpeed());
        float boost = (_linearFactor * s + _quadraticFactor * s * s) * _extraMultiplier;
        Vector3 delta = (Vector3)(_scrollDirection.normalized * boost * Time.deltaTime);

        if (_group != null)
        {
            _group.position += delta;
        }
        else if (_fallbackNodes != null)
        {
            foreach (var t in _fallbackNodes)
            {
                if (t == null) continue;
                t.position += delta;
            }
        }
    }
}