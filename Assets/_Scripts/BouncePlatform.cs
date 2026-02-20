using UnityEngine;

public class BouncePlatform : MonoBehaviour
{
    [SerializeField] private float bounceVelocity = 14f; // increase for higher bounce
    [SerializeField] private LayerMask playerLayer;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        Rigidbody2D rb = collision.rigidbody;
        if (rb == null) return;

        // Only bounce if the player is coming down onto it
        if (rb.linearVelocity.y >= 0f) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceVelocity);
    }
}
