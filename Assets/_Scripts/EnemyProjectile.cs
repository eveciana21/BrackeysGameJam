using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask playerLayer;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 3f;

    [Header("Rotation")]
    [SerializeField] private float rotationOffsetDegrees = 0f;
    // If your sprite points UP by default, set this to -90.
    // If it points RIGHT by default, leave at 0.

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction, float speed)
    {
        if (rb == null) return;

        Vector2 normalizedDir = direction.normalized;
        rb.linearVelocity = normalizedDir * speed;

        RotateToDirection(normalizedDir);
    }

    private void RotateToDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffsetDegrees);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            player = other.GetComponentInParent<Player>();
        }

        if (player != null)
        {
            player.DamagePlayer(damage);
        }

        Destroy(gameObject);
    }
}
