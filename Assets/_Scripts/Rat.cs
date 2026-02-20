using System.Collections;
using UnityEngine;

public class Rat : EnemyBaseClass
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private int minBaseSpeed = 1;
    [SerializeField] private int maxBaseSpeed = 3;
    [SerializeField] private float stompSpeedMultiplier = 1.5f;
    [SerializeField] private float maxPossibleSpeed = 6f;
    private float moveSpeed;

    [Header("Checks")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.15f;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 0.25f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private float damageFlashTime = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.red;

    [Header("Stomp")]
    [SerializeField] private float bounceForce = 10f;
    [SerializeField] private float stompCooldown = 0.1f;

    [Header("Player Damage")]
    [SerializeField] private int touchDamage = 1;
    [SerializeField] private Vector2 touchKnockback = new Vector2(4f, 6f);

    [Header("Layers")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask playerLayer;

    private bool facingRight = true;
    private int currentHealth;
    private float lastStompTime;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        facingRight = transform.localScale.x >= 0f;
        currentHealth = maxHealth;
        moveSpeed = Random.Range(minBaseSpeed, maxBaseSpeed + 1);
        moveSpeed = Mathf.Min(moveSpeed, maxPossibleSpeed);
    }

    private void FixedUpdate()
    {
        Move();

        bool hitWall = Physics2D.Raycast(wallCheck.position, FacingDir(), wallCheckDistance, wallLayer);
        bool hasGroundAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        if (hitWall || !hasGroundAhead)
        {
            Flip();
        }
    }

    private void Move()
    {
        if (rb != null)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x = FacingDir().x * moveSpeed;
            rb.linearVelocity = velocity;
        }
        else
        {
            Vector3 delta = new Vector3(FacingDir().x * moveSpeed * Time.fixedDeltaTime, 0f, 0f);
            transform.position += delta;
        }
    }

    private Vector2 FacingDir()
    {
        return facingRight ? Vector2.right : Vector2.left;
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;
        if (currentHealth <= 0) return;

        if (Time.time < lastStompTime + stompCooldown) return;

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        Player player = collision.gameObject.GetComponent<Player>();
        if (playerRb == null || player == null) return;

        bool stomped = false;

        // Stomp check: player hitting rat from above
        int contactCount = collision.contactCount;
        int i = 0;

        while (i < contactCount)
        {
            ContactPoint2D contact = collision.GetContact(i);

            if (contact.normal.y < -0.5f && playerRb.linearVelocity.y < 0f)
            {
                stomped = true;
                break;
            }

            i++;
        }

        if (stomped)
        {
            lastStompTime = Time.time;

            // Bounce player upward
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
            playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

            TakeDamage(1);

            // Speed up 2x on stomp
            moveSpeed = Mathf.Min(moveSpeed * stompSpeedMultiplier, maxPossibleSpeed);
        }
        else
        {
            // Not a stomp => damage player
            player.DamagePlayer(touchDamage);

            // Optional knockback away from rat
            float dir = (playerRb.position.x >= rb.position.x) ? 1f : -1f;
            Vector2 knock = new Vector2(touchKnockback.x * dir, touchKnockback.y);
            player.ApplyKnockback(knock);
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

    public void ApplyDamage(int amount)
    {
        TakeDamage(amount);
    }

    public void Die()
    {
        animator.SetTrigger("Death");

        NotifyDeath();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        enabled = false;
        Destroy(gameObject, 1f);
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
}
