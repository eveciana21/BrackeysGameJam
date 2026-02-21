using System.Collections;
using UnityEngine;

public class EnemyLeg : EnemyBaseClass
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D enemyCollider;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float enragedSpeedMultiplier = 1.5f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpInterval = 2f;
    [SerializeField] private float horizontalJumpMultiplier = 1.2f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Damage Flash")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private float damageFlashTime = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.red;

    [Header("Player Hit (stomp + touch)")]
    [SerializeField] private int stompDamage = 1;
    [SerializeField] private float stompKnockbackX = 14f;
    [SerializeField] private float stompKnockbackY = 14f;
    [SerializeField] private float stompIgnoreCollisionTime = 0.15f;

    [SerializeField] private int touchDamage = 1;
    [SerializeField] private float touchKnockbackX = 10f;
    [SerializeField] private float touchKnockbackY = 8f;
    [SerializeField] private float touchCooldown = 0.35f;

    [Header("Screen Shake")]
    [SerializeField] private bool shakeOnStomp = true;
    [SerializeField] private float stompShakeAmplitude = 1.5f;
    [SerializeField] private float stompShakeDuration = 0.12f;

    [Header("Detection")]
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float groundCheckOffset = 1f;

    [Header("Fall Feel")]
    [SerializeField] private float extraFallGravity = 2f;

    [Header("Bounds (X only)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float edgeFlipCooldown = 0.25f;
    [SerializeField] private float boundsPadding = 0.1f;

    [Header("Flip Cooldown")]
    [SerializeField] private float flipCooldown = 0.2f;

    [Header("Death: Final Stomp + Fall Through")]
    [SerializeField] private float deathStompImpulse = 14f;
    [SerializeField] private float deathFallExtraGravity = 4f;
    [SerializeField] private float fallThroughDelay = 0.08f;
    [SerializeField] private float destroyAfterDeathSeconds = 1.25f;

    private Collider2D patrolBounds; // auto-found by PatrolBounds tag

    [Space(10)]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask playerLayer;

    private bool facingRight;
    private bool isGrounded;
    private bool wasGrounded;

    private float nextFlipTime;
    private float nextEdgeFlipTime;
    private float nextTouchTime;
    private float nextStompShakeTime;

    private int currentHealth;
    private bool isDying;

    private Coroutine jumpRoutine;
    private Coroutine flashRoutine;

    private float baseMoveSpeed;
    private Vector2 lastKnownVelocity; // cached each FixedUpdate to restore if a projectile hits

    private void Awake()
    {
        facingRight = transform.localScale.x >= 0f;
        currentHealth = maxHealth;
        baseMoveSpeed = moveSpeed;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (enemyCollider == null) enemyCollider = GetComponent<Collider2D>();

        if (patrolBounds == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("PatrolBounds");
            if (obj != null)
            {
                patrolBounds = obj.GetComponent<Collider2D>();
            }
        }
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
        if (isDying) return;

        isGrounded = CheckGrounded();

        bool landedThisFrame = !wasGrounded && isGrounded;
        wasGrounded = isGrounded;

        if (landedThisFrame)
        {
            TryStompShake();
        }

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (HandleBoundsXOnly()) return;

            CheckForFlip();
            return;
        }

        MoveInAir();
        ApplyExtraFallGravity();

        // Cache velocity so projectile hits can restore it without disrupting the jump
        lastKnownVelocity = rb.linearVelocity;
    }

    private IEnumerator JumpCoroutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, 1f));

        while (true)
        {
            bool enraged = currentHealth <= (maxHealth / 2f);
            float interval = enraged ? jumpInterval / enragedSpeedMultiplier : jumpInterval;

            yield return new WaitForSeconds(interval);

            if (isDying) continue;

            if (isGrounded && IsSafeToJump())
            {
                yield return new WaitForFixedUpdate();
                Jump();
            }
        }
    }

    private void Jump()
    {
        float dir = facingRight ? 1f : -1f;
        float airSpeed = baseMoveSpeed * horizontalJumpMultiplier;

        rb.linearVelocity = new Vector2(dir * airSpeed, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void MoveInAir()
    {
        float dir = facingRight ? 1f : -1f;
        float airSpeed = baseMoveSpeed * horizontalJumpMultiplier;

        rb.linearVelocity = new Vector2(dir * airSpeed, rb.linearVelocity.y);
    }

    private void ApplyExtraFallGravity()
    {
        if (extraFallGravity <= 1f) return;

        rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (extraFallGravity - 1f) * Time.fixedDeltaTime;
    }

    private bool HandleBoundsXOnly()
    {
        if (!useBounds) return false;
        if (enemyCollider == null) return false;
        if (patrolBounds == null) return false;
        if (Time.time < nextEdgeFlipTime) return false;

        Bounds my = enemyCollider.bounds;
        Bounds bounds = patrolBounds.bounds;

        bool atRight = my.max.x >= bounds.max.x - boundsPadding;
        bool atLeft = my.min.x <= bounds.min.x + boundsPadding;

        bool groundAhead = HasGroundAhead();

        bool shouldTurn =
            (facingRight && atRight) ||
            (!facingRight && atLeft) ||
            !groundAhead;

        if (!shouldTurn) return false;

        Flip();
        nextEdgeFlipTime = Time.time + edgeFlipCooldown;

        Jump();
        return true;
    }

    private bool HasGroundAhead()
    {
        if (enemyCollider == null) return true;

        Bounds b = enemyCollider.bounds;
        float dir = facingRight ? 1f : -1f;

        float footX = facingRight ? b.max.x : b.min.x;
        Vector2 origin = new Vector2(footX + (0.1f * dir), b.min.y + 0.05f);

        return Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }

    private bool IsSafeToJump()
    {
        float dir = facingRight ? 1f : -1f;
        float jumpDistance = baseMoveSpeed * horizontalJumpMultiplier * 0.8f;
        Bounds b = enemyCollider.bounds;

        float rayStartY = b.max.y + 1f;
        float rayLength = groundCheckDistance + (rayStartY - b.min.y);

        Vector2 landingCheckPosition = new Vector2(transform.position.x + (jumpDistance * dir), rayStartY);
        bool groundAtLanding = Physics2D.Raycast(landingCheckPosition, Vector2.down, rayLength, groundLayer);

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

        Vector2 groundCheckPosition = new Vector2(
            b.center.x + (groundCheckOffset * dir),
            b.min.y + 0.05f
        );

        bool groundAhead = Physics2D.Raycast(groundCheckPosition, Vector2.down, groundCheckDistance, groundLayer);

        Vector2 wallCheckDirection = facingRight ? Vector2.right : Vector2.left;
        bool wallAhead = Physics2D.Raycast(b.center, wallCheckDirection, wallCheckDistance, wallLayer);

        if (!groundAhead || wallAhead)
        {
            Flip();
        }
    }

    private bool CheckGrounded()
    {
        if (enemyCollider == null) return false;

        Bounds b = enemyCollider.bounds;

        float extra = 0.05f;
        Vector2 origin = new Vector2(b.center.x, b.center.y);
        Vector2 size = new Vector2(b.size.x * 0.9f, b.size.y);

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
        if (isDying) return;
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        Player player = collision.gameObject.GetComponent<Player>();
        if (player == null) return;

        if (IsStompOnPlayer(collision))
        {
            float dirX = GetKnockbackDirX(player.transform);

            player.DamagePlayer(stompDamage);
            player.ApplyKnockback(new Vector2(dirX * stompKnockbackX, stompKnockbackY));

            StartCoroutine(TemporarilyIgnorePlayerCollision(player));
            TryStompShake();
            return;
        }

        ApplyTouchHit(player);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDying) return;
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        Player player = collision.gameObject.GetComponent<Player>();
        if (player == null) return;

        if (IsStompOnPlayer(collision)) return;

        ApplyTouchHit(player);
    }

    private bool IsStompOnPlayer(Collision2D collision)
    {
        if (rb.linearVelocity.y >= 0f) return false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.normal.y > 0.5f) return true;
        }

        return false;
    }

    private void ApplyTouchHit(Player player)
    {
        if (Time.time < nextTouchTime) return;

        nextTouchTime = Time.time + touchCooldown;

        float dirX = GetKnockbackDirX(player.transform);

        player.DamagePlayer(touchDamage);
        player.ApplyKnockback(new Vector2(dirX * touchKnockbackX, touchKnockbackY));
    }

    private float GetKnockbackDirX(Transform playerTransform)
    {
        return playerTransform.position.x >= transform.position.x ? 1f : -1f;
    }

    private IEnumerator TemporarilyIgnorePlayerCollision(Player player)
    {
        if (enemyCollider == null) yield break;

        Collider2D playerCol = player?.GetComponent<Collider2D>();
        if (playerCol == null) yield break;

        Physics2D.IgnoreCollision(enemyCollider, playerCol, true);
        yield return new WaitForSeconds(stompIgnoreCollisionTime);

        if (enemyCollider != null && playerCol != null)
        {
            Physics2D.IgnoreCollision(enemyCollider, playerCol, false);
        }

        // Restart jump routine if it was killed
        if (jumpRoutine == null && !isDying)
        {
            jumpRoutine = StartCoroutine(JumpCoroutine());
        }
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

    public void ApplyDamage(int amount)
    {
        if (isDying) return;
        TakeDamage(amount);
    }

    // Called by Projectile to undo any physics impulse the collision applied this frame
    public void RestoreVelocity()
    {
        if (rb != null && !isGrounded)
        {
            rb.linearVelocity = lastKnownVelocity;
        }
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
        if (isDying) return;
        isDying = true;

        NotifyDeath();

        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
        }

        StartCoroutine(DeathStompThenFallThrough());
    }

    private IEnumerator DeathStompThenFallThrough()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.AddForce(Vector2.down * deathStompImpulse, ForceMode2D.Impulse);

            if (deathFallExtraGravity > 1f)
            {
                rb.gravityScale *= deathFallExtraGravity;
            }
        }

        TryStompShake();

        yield return new WaitForSeconds(fallThroughDelay);

        if (enemyCollider != null)
        {
            enemyCollider.isTrigger = true;
        }

        Destroy(gameObject, destroyAfterDeathSeconds);
    }
}