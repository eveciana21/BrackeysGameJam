using UnityEngine;

public class EnemyBasic : MonoBehaviour
{
    [Header("Internal Resources")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Detection")]
    [SerializeField] private float groundCheckDistance = 1f; // how far downwards the ground check is
    [SerializeField] private float wallCheckDistance = 2f; // distance check from the walls
    [SerializeField] private float groundCheckOffset = 1f; // How far forward to check for ground
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

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
}