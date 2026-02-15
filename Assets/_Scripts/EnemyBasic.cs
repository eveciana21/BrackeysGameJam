using UnityEngine;

public class EnemyBasic : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Player Bounce")]
    [SerializeField] private float bounceForce = 10f;

    [Header("Detection")]
    [SerializeField] private float groundCheckDistance = 1f; // how far downwards the ground check is
    [SerializeField] private float wallCheckDistance = 2f; // distance check from the walls
    [SerializeField] private float groundCheckOffset = 1f; // How far forward to check for ground
    [Space(10)]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask playerLayer;

    private bool facingRight;

    private void Awake()
    {
        facingRight = transform.localScale.x >= 0f;
    }

    private void FixedUpdate()
    {
        Move();
        CheckForFlip();
    }

    private void Move()
    {
        float direction = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    private void CheckForFlip()
    {
        // Calculate check positions based on facing direction
        float directionMultiplier = facingRight ? 1f : -1f;
        Vector2 groundCheckPosition = new Vector2(transform.position.x + (groundCheckOffset * directionMultiplier), transform.position.y);

        // Check if there's ground ahead
        bool groundAhead = Physics2D.Raycast(groundCheckPosition, Vector2.down, groundCheckDistance, groundLayer);

        // Check if there's a wall ahead
        Vector2 wallCheckDirection = facingRight ? Vector2.right : Vector2.left;
        bool wallAhead = Physics2D.Raycast(transform.position, wallCheckDirection, wallCheckDistance, wallLayer);

        // Flip if no ground ahead or if hit a wall
        if (!groundAhead || wallAhead)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if it's the player
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        // Check if player hit from above
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // If contact normal is pointing upward, player hit from above
            if (contact.normal.y < -0.5f && playerRb.linearVelocity.y < 0f)
            {
                // Bounce the player
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
                playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                // Call damage method here?
                // TakeDamage();

                break;
            }
        }
    }
}