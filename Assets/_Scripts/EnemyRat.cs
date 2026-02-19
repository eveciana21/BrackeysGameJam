using System.Collections;
using UnityEngine;

public class EnemyRat : EnemyBaseClass
{
    [SerializeField] private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Small enemy checks")]
    [SerializeField] private float forwardGroundOffset = 0.22f;
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private float groundCheckYOffset = -0.10f;

    [SerializeField] private float wallCheckDistance = 0.15f;
    [SerializeField] private float wallCheckYOffset = -0.02f;

    private bool movingRight = true;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void FixedUpdate()
    {
        CheckFlip();
        Move();
    }

    private void Move()
    {
        float dir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    private void CheckFlip()
    {
        float dir = movingRight ? 1f : -1f;
        Vector2 basePos = rb.position;

        // ground ahead
        Vector2 groundOrigin = new Vector2(
            basePos.x + (forwardGroundOffset * dir),
            basePos.y + groundCheckYOffset
        );

        bool groundAhead = Physics2D.Raycast(groundOrigin, Vector2.down, groundCheckDistance, groundLayer);

        // wall ahead
        Vector2 wallOrigin = new Vector2(
            basePos.x,
            basePos.y + wallCheckYOffset
        );

        Vector2 wallDir = movingRight ? Vector2.right : Vector2.left;

        bool wallAhead = Physics2D.Raycast(wallOrigin, wallDir, wallCheckDistance, wallLayer);

        if (!groundAhead || wallAhead)
        {
            Flip();
        }
    }

    private void Flip()
    {
        movingRight = !movingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    /*    [Header("Components")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer bodyRenderer;

        [Header("Health")]
        [SerializeField] private int maxHealth = 3;

        [Header("Damage")]
        [SerializeField] private float damageFlashTime = 0.08f;
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private AudioClip damageSfx;
        [SerializeField] private AudioClip deathSFX;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;

        [Header("Flip Tuning")]
        [SerializeField] private float flipCooldown = 0.15f;

        [Header("Detection (small enemy)")]
        [SerializeField] private float groundCheckDistance = 0.25f;
        [SerializeField] private float wallCheckDistance = 0.15f;
        [SerializeField] private float forwardGroundOffset = 0.25f;
        [SerializeField] private Vector2 groundRayLocalOffset = new Vector2(0f, -0.10f);
        [SerializeField] private Vector2 wallRayLocalOffset = new Vector2(0.15f, -0.02f);

        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask wallLayer;

        private bool facingRight;
        private float nextFlipTime;
        private int currentHealth;
        private Coroutine flashRoutine;

        private void Awake()
        {
            facingRight = transform.localScale.x >= 0f;
            currentHealth = maxHealth;
        }

        private void FixedUpdate()
        {
            Move();
            CheckForFlip();
        }

        private void Move()
        {
            float dir = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        }

        private void CheckForFlip()
        {
            if (Time.time < nextFlipTime) return;

            float dir = facingRight ? 1f : -1f;
            Vector2 basePos = transform.position;

            // Ground ahead (two short downward rays)
            Vector2 groundOriginA = basePos + new Vector2(forwardGroundOffset * dir, 0f) + groundRayLocalOffset;
            Vector2 groundOriginB = basePos + new Vector2((forwardGroundOffset + 0.10f) * dir, 0f) + groundRayLocalOffset;

            bool groundAheadA = Physics2D.Raycast(groundOriginA, Vector2.down, groundCheckDistance, groundLayer);
            bool groundAheadB = Physics2D.Raycast(groundOriginB, Vector2.down, groundCheckDistance, groundLayer);
            bool groundAhead = groundAheadA || groundAheadB;

            // Wall ahead (short forward ray, slightly above feet)
            Vector2 wallOrigin = basePos + new Vector2(wallRayLocalOffset.x * dir, wallRayLocalOffset.y);
            Vector2 wallDir = facingRight ? Vector2.right : Vector2.left;

            bool wallAhead = Physics2D.Raycast(wallOrigin, wallDir, wallCheckDistance, wallLayer);

            if (!groundAhead || wallAhead)
            {
                Flip();
                nextFlipTime = Time.time + flipCooldown;
            }
        }

        private void Flip()
        {
            facingRight = !facingRight;

            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
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

        public void Die()
        {
            NotifyDeath();

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }

            enabled = false;
            Destroy(gameObject, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            float dir = facingRight ? 1f : -1f;
            Vector2 basePos = transform.position;

            Vector2 groundOriginA = basePos + new Vector2(forwardGroundOffset * dir, 0f) + groundRayLocalOffset;
            Vector2 groundOriginB = basePos + new Vector2((forwardGroundOffset + 0.10f) * dir, 0f) + groundRayLocalOffset;

            Vector2 wallOrigin = basePos + new Vector2(wallRayLocalOffset.x * dir, wallRayLocalOffset.y);
            Vector2 wallDir = facingRight ? Vector2.right : Vector2.left;

            Gizmos.DrawLine(groundOriginA, groundOriginA + Vector2.down * groundCheckDistance);
            Gizmos.DrawLine(groundOriginB, groundOriginB + Vector2.down * groundCheckDistance);
            Gizmos.DrawLine(wallOrigin, wallOrigin + wallDir * wallCheckDistance);
        }*/
}
