using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D playerCollider;

    [Header("Player Properties")]
    [SerializeField] private float playerSpeed = 8f;

    [Header("Jump Properties")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f; // if you didnt hold the jump key

    [Header("Throw")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwSpeed = 10f;
    [SerializeField] private float throwUpwardBoost = 2.5f;
    [SerializeField] private float throwCooldown = 0.35f;

    private float nextThrowTime;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f; // the radius that is considered ground
    [SerializeField] private float groundCheckOffset = 0.1f; // gives a more forgiving landing check

    private Vector2 moveInput;
    private bool isJumpHeld = false;
    private bool isGrounded = false;


    private void FixedUpdate()
    {
        CheckIfGrounded();
        PlayerMovement();
        ApplyGravityMultiplier();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        FlipPlayer();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            isJumpHeld = true;
            PerformJump();
        }
        else if (context.canceled)
        {
            isJumpHeld = false;
        }
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (Time.time < nextThrowTime) return;

        nextThrowTime = Time.time + throwCooldown;
        ThrowProjectile();
    }

    private void ThrowProjectile()
    {
        if (projectilePrefab == null || throwPoint == null) return;

        GameObject proj = Instantiate(projectilePrefab, throwPoint.position, Quaternion.identity);

        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        if (projRb == null) return;

        float facingDir = transform.localScale.x >= 0f ? 1f : -1f;
        Vector2 launchVelocity = new Vector2(facingDir * throwSpeed, throwUpwardBoost);

        projRb.linearVelocity = launchVelocity;
    }

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void ApplyGravityMultiplier()
    {
        float velocityY = rb.linearVelocity.y;
        float multiplier = 0f;

        if (velocityY < 0) // Falling
        {
            multiplier = fallMultiplier - 1;
        }
        else if (velocityY > 0 && !isJumpHeld) // Released jump early
        {
            multiplier = lowJumpMultiplier - 1;
        }

        rb.linearVelocity += Vector2.up * Physics2D.gravity.y * multiplier * Time.fixedDeltaTime;
    }

    private void CheckIfGrounded()
    {
        Vector2 checkPosition = new Vector2(transform.position.x, playerCollider.bounds.min.y + groundCheckOffset);
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer);

        if (isGrounded != wasGrounded && isGrounded)
        {
            // Just landed
            // Add animation state here for landing
        }
    }

    private void PlayerMovement()
    {
        float targetVelX = Mathf.Clamp(moveInput.x, -1f, 1f) * playerSpeed;
        rb.linearVelocity = new Vector2(targetVelX, rb.linearVelocity.y);
    }

    private void FlipPlayer()
    {
        if (Mathf.Abs(moveInput.x) > 0.01)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (moveInput.x > 0f ? 1f : -1f);
            transform.localScale = scale;
        }
    }
}
