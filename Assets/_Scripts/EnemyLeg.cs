using System.Collections;
using UnityEngine;

public class EnemyLeg : EnemyBaseClass
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D enemyCollider;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Player Bounce")]
    [SerializeField] private float bounceForce = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpInterval = 2f; // seconds between jumps
    [SerializeField] private float horizontalJumpMultiplier = 1.2f; // horizontal speed during jump

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Damage")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private float damageFlashTime = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.red;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float contactDamageCooldown = 1f;

    [Header("Screen Shake")]
    [SerializeField] private bool shakeOnStomp = true;
    [SerializeField] private float stompShakeAmplitude = 1.5f;
    [SerializeField] private float stompShakeDuration = 0.12f;

    private float nextStompShakeTime;
    private float nextContactDamageTime;
    private int currentHealth;
    private Coroutine flashRoutine;

    private bool wasGrounded;

    [Header("Detection")]
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float groundCheckOffset = 1f;
    [SerializeField] private float groundedRayLength = 0.2f;

    [Header("Fall Feel")]
    [SerializeField] private float extraFallGravity = 2f;

    [Header("Screen Bounds (X only)")]
    [SerializeField] private bool useScreenBounds = true;
    [SerializeField] private float screenEdgePadding = 0.03f; // 0..0.5 (viewport space)
    [SerializeField] private float edgeFlipCooldown = 0.25f;

    [SerializeField] private float flipCooldown = 0.2f;
    private float nextFlipTime;

    [Space(10)]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask playerLayer;

    private bool facingRight;
    private bool isGrounded;

    private Coroutine jumpRoutine;

    private float nextEdgeFlipTime;
    private Camera mainCam;

    private void Awake()
    {
        facingRight = transform.localScale.x >= 0f;
        mainCam = Camera.main;
        currentHealth = maxHealth;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (enemyCollider == null)
        {
            enemyCollider = GetComponent<Collider2D>();
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (HandleScreenEdgeXOnly())
        {
            return;
        }

        CheckForFlip();
        return;
    }

    private void OnEnable()
    {
        jumpRoutine = StartCoroutine(JumpCoroutine());
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

        bool landedThisFrame = !wasGrounded && isGrounded;
        wasGrounded = isGrounded;

        if (landedThisFrame)
        {
            TryStompShake();
        }

        if (isGrounded)
        {
            // Leg is "planted" on ground: never slide horizontally
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // Left/right screen edge handling (X only). If we flipped, jump away immediately.
            if (HandleScreenEdgeXOnly())
            {
                return;
            }

            // Optional: also flip if a wall/edge is immediately ahead (world collision)
            CheckForFlip();
            return;
        }

        // In air: stomp horizontally
        MoveInAir();

        // Extra gravity while in air (heavier stomp feel)
        ApplyExtraFallGravity();
    }

    private void TryStompShake()
    {
        if (!shakeOnStomp) return;

        ScreenShake shaker = ScreenShake.Instance;
        if (shaker == null) return;

        if (Time.time < nextStompShakeTime) return;
        nextStompShakeTime = Time.time + stompShakeDuration;

        shaker.Shake(stompShakeAmplitude);
    }

    private void MoveInAir()
    {
        float dir = facingRight ? 1f : -1f;
        float airSpeed = moveSpeed * horizontalJumpMultiplier;

        rb.linearVelocity = new Vector2(dir * airSpeed, rb.linearVelocity.y);
    }

    private void ApplyExtraFallGravity()
    {
        if (extraFallGravity <= 1f) return;

        rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (extraFallGravity - 1f) * Time.fixedDeltaTime;
    }

    private void Jump()
    {
        float dir = facingRight ? 1f : -1f;
        float airSpeed = moveSpeed * horizontalJumpMultiplier;

        // Start stomp direction immediately
        rb.linearVelocity = new Vector2(dir * airSpeed, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private IEnumerator JumpCoroutine()
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

    private bool HandleScreenEdgeXOnly()
    {
        if (!useScreenBounds) return false;
        if (mainCam == null) return false;
        if (enemyCollider == null) return false;
        if (Time.time < nextEdgeFlipTime) return false;

        Bounds b = enemyCollider.bounds;

        // Screen edge check (X only) using the collider edges
        Vector3 leftVp = mainCam.WorldToViewportPoint(new Vector3(b.min.x, b.center.y, 0f));
        Vector3 rightVp = mainCam.WorldToViewportPoint(new Vector3(b.max.x, b.center.y, 0f));

        bool atRightScreenEdge = rightVp.x >= 1f - screenEdgePadding;
        bool atLeftScreenEdge = leftVp.x <= screenEdgePadding;

        // Ground-ahead safety check (prevents falling off stage)
        bool groundAhead = HasGroundAhead();

        bool shouldTurn =
            (facingRight && atRightScreenEdge) ||
            (!facingRight && atLeftScreenEdge) ||
            !groundAhead;

        if (!shouldTurn) return false;

        Flip();
        nextEdgeFlipTime = Time.time + edgeFlipCooldown;

        // Jump immediately so we leave the edge and don't "stutter"
        Jump();
        return true;
    }

    private bool HasGroundAhead()
    {
        if (enemyCollider == null) return true;

        Bounds b = enemyCollider.bounds;

        float dir = facingRight ? 1f : -1f;

        // "Front foot" position (a little in front of the collider edge)
        float footX = facingRight ? b.max.x : b.min.x;
        Vector2 origin = new Vector2(footX + (0.1f * dir), b.min.y + 0.05f);

        float distanceDown = groundCheckDistance;

        // Debug.DrawRay(origin, Vector2.down * distanceDown, Color.cyan); // optional

        return Physics2D.Raycast(origin, Vector2.down, distanceDown, groundLayer);
    }

    private bool IsSafeToJump()
    {
        float dir = facingRight ? 1f : -1f;

        // Rough landing distance estimate
        float jumpDistance = moveSpeed * horizontalJumpMultiplier * 0.8f;

        Bounds b = enemyCollider.bounds;

        // Ray starts well above so it doesn't clip into any surface
        float rayStartY = b.max.y + 1f;
        float rayLength = groundCheckDistance + (rayStartY - b.min.y);

        // Check ground at the landing spot
        Vector2 landingCheckPosition = new Vector2(transform.position.x + (jumpDistance * dir), rayStartY);
        bool groundAtLanding = Physics2D.Raycast(landingCheckPosition, Vector2.down, rayLength, groundLayer);

        // Also check that there's still ground right at the front edge (catches edge cases)
        Vector2 frontEdgePosition = new Vector2(facingRight ? b.max.x : b.min.x, rayStartY);
        bool groundAtFrontEdge = Physics2D.Raycast(frontEdgePosition, Vector2.down, rayLength, groundLayer);

        Vector2 wallCheckDirection = facingRight ? Vector2.right : Vector2.left;
        bool wallAhead = Physics2D.Raycast(b.center, wallCheckDirection, wallCheckDistance, wallLayer);

        return groundAtLanding && groundAtFrontEdge && !wallAhead;
    }

    private void CheckForFlip()
    {
        if (enemyCollider == null) return;

        Bounds b = enemyCollider.bounds;

        float dir = facingRight ? 1f : -1f;

        // Put the ground-ahead ray at the FRONT FOOT area:
        // - X: a bit in front of the collider
        // - Y: right near the bottom of the collider (the "foot")
        Vector2 groundCheckPosition = new Vector2(
            b.center.x + (groundCheckOffset * dir),
            b.min.y + 0.05f
        );

        bool groundAhead = Physics2D.Raycast(groundCheckPosition, Vector2.down, groundCheckDistance, groundLayer);

        // Wall check from the collider center (good baseline)
        Vector2 wallCheckDirection = facingRight ? Vector2.right : Vector2.left;
        bool wallAhead = Physics2D.Raycast(b.center, wallCheckDirection, wallCheckDistance, wallLayer);

        // Optional: visualize the rays while debugging
        // Debug.DrawRay(groundCheckPosition, Vector2.down * groundCheckDistance, groundAhead ? Color.green : Color.red);
        // Debug.DrawRay(b.center, wallCheckDirection * wallCheckDistance, wallAhead ? Color.green : Color.red);

        if (!groundAhead || wallAhead)
        {
            Flip();
        }
    }

    private bool CheckGrounded()
    {
        if (enemyCollider == null) return false;

        Bounds b = enemyCollider.bounds;

        float extra = 0.05f; // small "reach" below the collider
        Vector2 origin = new Vector2(b.center.x, b.center.y);
        Vector2 size = new Vector2(b.size.x * 0.9f, b.size.y); // slightly thinner to avoid side hits

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, extra, groundLayer);
        return hit.collider != null;
    }

    private void Flip()
    {
        if (Time.time < nextFlipTime) return;
        nextFlipTime = Time.time + flipCooldown;

        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f && playerRb.linearVelocity.y < 0f)
            {
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
                playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
                break;
            }
        }
    }

    public void ApplyDamage(int amount)
    {
        TakeDamage(amount);
    }

    private void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        FlashDamage();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void FlashDamage()
    {
        if (bodyRenderer == null) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashDamageRoutine());
    }

    private IEnumerator FlashDamageRoutine()
    {
        Color original = bodyRenderer.color;
        bodyRenderer.color = damageFlashColor;

        yield return new WaitForSeconds(damageFlashTime);

        bodyRenderer.color = original;
        flashRoutine = null;
    }

    private void Die()
    {
        NotifyDeath();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
        }

        Destroy(gameObject, 0.1f);
    }
}