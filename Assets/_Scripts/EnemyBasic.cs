using System.Collections;
using UnityEngine;

public class EnemyBasic : EnemyBaseClass
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private GameObject turkeyLeg;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Damage")]
    [SerializeField] private float damageFlashTime = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private AudioClip damageSfx;
    [SerializeField] private AudioClip deathSFX;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Player Bounce")]
    [SerializeField] private float bounceForce = 10f;

    [Header("Stomp Difficulty")]
    [SerializeField] private float stompKnockbackX = 3f;          // sideways push on stomp
    [SerializeField] private float moveSpeedIncreaseOnStomp = 0.5f; // enemy gets faster per stomp
    [SerializeField] private float maxMoveSpeed = 5f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;     // radius of the attack zone
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackOffsetX = 0.8f;   // how far in front the zone is
    [SerializeField] private float attackDuration = 0.5f;

    [Header("Flip Tuning")]
    [SerializeField] private float flipCooldown = 0.15f;   // prevents flip spam

    [Header("Detection")]
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float wallCheckDistance = 2f;
    [SerializeField] private float groundCheckOffset = 1f;
    [Space(10)]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask playerLayer;

    private bool facingRight;
    private bool isAttacking;
    private float lastAttackTime;
    private float nextFlipTime;
    private int currentHealth;
    private float lastStompTime;
    private float stompCooldown = 0.1f;

    private Transform playerTransform;
    private Coroutine flashRoutine;


    private void Awake()
    {
        facingRight = transform.localScale.x >= 0f;

        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        FindPlayerIfNeeded();
        if (playerTransform != null && !isAttacking)
        {
            // If player is close enough to care, face them
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange + 0.5f) // small buffer so it feels responsive
            {
                FacePlayerIfBehind();
            }

            // After facing them, attack only if player is in front zone + cooldown ready
            if (IsPlayerInFrontAttackZone() && Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }

        if (!isAttacking)
        {
            Move();
            CheckForFlip();
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (playerTransform != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Move()
    {
        float direction = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        animator.SetBool("IsWalking", true);
    }

    private void FacePlayerIfBehind()
    {
        if (Time.time < nextFlipTime) return;

        float toPlayerX = playerTransform.position.x - transform.position.x;

        // If player is left of enemy, we want facingRight = false
        // If player is right of enemy, we want facingRight = true
        bool shouldFaceRight = toPlayerX > 0f;

        if (shouldFaceRight != facingRight)
        {
            Flip();
            nextFlipTime = Time.time + flipCooldown;
        }
    }

    private bool IsPlayerInFrontAttackZone()
    {
        if (playerTransform == null) return false;

        float dir = facingRight ? 1f : -1f;

        // Circle centered in front of enemy
        Vector2 center = new Vector2(transform.position.x + (attackOffsetX * dir), transform.position.y);

        Collider2D hit = Physics2D.OverlapCircle(center, attackRange, playerLayer);
        return hit != null;
    }

    private void CheckForFlip()
    {
        // don't do edge/wall flipping while player is near (prevents weird �fight the player� flips)
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange + 0.5f) return;
        }

        float directionMultiplier = facingRight ? 1f : -1f;
        Vector2 groundCheckPosition = new Vector2(transform.position.x + (groundCheckOffset * directionMultiplier), transform.position.y);

        bool groundAhead = Physics2D.Raycast(groundCheckPosition, Vector2.down, groundCheckDistance, groundLayer);

        Vector2 wallCheckDirection = facingRight ? Vector2.right : Vector2.left;
        bool wallAhead = Physics2D.Raycast(transform.position, wallCheckDirection, wallCheckDistance, wallLayer);

        if (!groundAhead || wallAhead)
        {
            if (Time.time >= nextFlipTime)
            {
                Flip();
                nextFlipTime = Time.time + flipCooldown;
            }
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        animator.SetBool("IsWalking", false);
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        CancelInvoke(nameof(EndAttack));
        Invoke(nameof(EndAttack), attackDuration);
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    public void Die()
    {
        NotifyDeath();

        CancelInvoke(nameof(EndAttack));
        isAttacking = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false; 
        }

        animator.SetBool("IsWalking", false);
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Death");

        if (turkeyLeg != null)
        {
            turkeyLeg.SetActive(false);
        }

        enabled = false;

        Destroy(gameObject, 1f);
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;
        if (currentHealth <= 0) return; // Don't process stomps if already dead
        if (Time.time < lastStompTime + stompCooldown) return; // Prevent rapid double-stomps

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f && playerRb.linearVelocity.y < 0f)
            {
                lastStompTime = Time.time;

                float knockDir = (playerRb.position.x >= rb.position.x) ? 1f : -1f;

                playerRb.linearVelocity = new Vector2(knockDir * stompKnockbackX, 0f);
                playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                TakeDamage(1);

                moveSpeed = Mathf.Min(moveSpeed + moveSpeedIncreaseOnStomp, maxMoveSpeed);

                break;
            }
        }
    }
}