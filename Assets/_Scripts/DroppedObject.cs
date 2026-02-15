using UnityEngine;

public class DroppedObject : MonoBehaviour
{
    [Header("Components")]
    [SerializeReference] private Rigidbody2D rb;
    [SerializeReference] private Collider2D objectCollider;

    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Damage")]
    [SerializeField] private int damageAmount = 1;

    private bool hasLanded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            hasLanded = true;

            // Stop moving after landing
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Static;
            }

            if (objectCollider != null)
            {
                objectCollider.enabled = false;
            }
        }

        // Only damage player if object hasn't landed yet
        if (!hasLanded && ((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            // TODO: Apply damage to player

            Destroy(gameObject);
        }
    }
}
