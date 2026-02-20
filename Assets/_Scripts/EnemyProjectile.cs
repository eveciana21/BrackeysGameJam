using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 3f;

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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Optional: destroy on hit
        // if (other.CompareTag("Player")) { ... }
        Destroy(gameObject);
    }
}
