using UnityEngine;

/// <summary>
/// Ensures the object stays within the specified boundary colliders.
/// Serves as a safety net in case high velocities allow the Rigidbody
/// to tunnel through the walls.
/// </summary>
public class BoundaryGuard : MonoBehaviour
{
    [SerializeField] private Collider2D _left;
    [SerializeField] private Collider2D _right;
    [SerializeField] private Collider2D _top;
    [SerializeField] private Collider2D _bottom;
    [SerializeField] private float _pushBack = 0.1f;

    private void LateUpdate()
    {
        Vector3 pos = transform.position;

        if (_left != null)
        {
            float limit = _left.bounds.max.x;
            if (pos.x < limit)
            {
                pos.x = limit + _pushBack;
            }
        }

        if (_right != null)
        {
            float limit = _right.bounds.min.x;
            if (pos.x > limit)
            {
                pos.x = limit - _pushBack;
            }
        }

        if (_bottom != null)
        {
            float limit = _bottom.bounds.max.y;
            if (pos.y < limit)
            {
                pos.y = limit + _pushBack;
            }
        }

        if (_top != null)
        {
            float limit = _top.bounds.min.y;
            if (pos.y > limit)
            {
                pos.y = limit - _pushBack;
            }
        }

        transform.position = pos;
    }
}
