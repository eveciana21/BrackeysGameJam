using System.Collections;
using UnityEngine;

public class EnemyJumper : MonoBehaviour
{
    [Header("Internal Resources")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D enemyCollider;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpInterval = 2f; // seconds between jumps
    [SerializeField] private float horizontalJumpMultiplier = 1.2f; // horizontal speed during jump

    [Header("Detection")]
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float groundCheckOffset = 1f;
    [SerializeField] private float groundedRayLength = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    private bool facingRight;
    private bool isJumping;
    private bool isGrounded;

    private Coroutine jumpRoutine;

    private void Awake()
    {
        facingRight = transform.localScale.x >= 0f;
    }

    private void OnEnable()
    {
        jumpRoutine = StartCoroutine(JumpLoop());
    }

    private void OnDisable()
    {
        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
        }
    }

    private void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        if (isGrounded)
        {
            isJumping = false;
        }

        Move();

        if (isGrounded)
        {
            CheckForFlip();
        }
    }

    private void Move()
    {
        float dir = facingRight ? 1f : -1f;
        float currentSpeed = isJumping ? moveSpeed * horizontalJumpMultiplier : moveSpeed;
        rb.linearVelocity = new Vector2(dir * currentSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        isJumping = true;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private bool IsSafeToJump()
    {
        // Check if there's ground ahead after the jump would land
        float dir = facingRight ? 1f : -1f;

        // Project forward to where enemy would land (rough estimate)
        float jumpDistance = moveSpeed * horizontalJumpMultiplier * 0.8f; // Approximate landing distance
        Vector2 landingCheckPosition = new Vector2(transform.position.x + (jumpDistance * dir), transform.position.y);

        // Check if there's ground at landing position
        bool groundAtLanding = Physics2D.Raycast(landingCheckPosition, Vector2.down, groundCheckDistance, groundLayer);

        // Check if there's a wall immediately ahead
        Vector2 wallCheckDirection = facingRight ? Vector2.right : Vector2.left;
        bool wallAhead = Physics2D.Raycast(enemyCollider.bounds.center, wallCheckDirection, wallCheckDistance, wallLayer);
        return groundAtLanding && !wallAhead;
    }

    private IEnumerator JumpLoop()
    {
        // small random start offset so multiple enemies don't jump in sync
        yield return new WaitForSeconds(Random.Range(0f, 1f));

        while (true)
        {
            yield return new WaitForSeconds(jumpInterval);

            if (isGrounded && IsSafeToJump())
            {
                yield return new WaitForFixedUpdate();
                Jump();
            }
        }
    }

    private void CheckForFlip()
    {
        float dir = facingRight ? 1f : -1f;
        Vector2 groundCheckPosition = new Vector2(transform.position.x + (groundCheckOffset * dir), transform.position.y);

        bool groundAhead = Physics2D.Raycast(groundCheckPosition, Vector2.down, groundCheckDistance, groundLayer);

        Vector2 wallCheckDirection = facingRight ? Vector2.right : Vector2.left;
        bool wallAhead = Physics2D.Raycast(enemyCollider.bounds.center, wallCheckDirection, wallCheckDistance, wallLayer);

        if (!groundAhead || wallAhead)
        {
            Flip();
        }
    }

    private bool CheckGrounded()
    {
        Vector2 origin = new Vector2(enemyCollider.bounds.center.x, enemyCollider.bounds.min.y);
        float distance = groundedRayLength;

        return Physics2D.Raycast(origin, Vector2.down, distance, groundLayer);
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }
}