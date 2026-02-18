using UnityEngine;

public class DroppedObject : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D objectCollider;

    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Damage")]
    [SerializeField] private int damageAmount = 1;

    private bool hasLanded;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (objectCollider == null)
        {
            objectCollider = GetComponent<Collider2D>();
        }

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Hit ground -> become "landed"
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            hasLanded = true;

            // Optional: Stop moving after landing
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Static;
            }

            // Optional: disable collider if you don't want it to block after landing
            if (objectCollider != null)
            {
                objectCollider.enabled = false;
            }

            return;
        }

        // Only damage player if object hasn't landed yet
        if (!hasLanded && ((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            // Player Damage
            Destroy(gameObject);
        }
    }
}
