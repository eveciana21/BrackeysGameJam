using UnityEngine;

public class EnemyFly : EnemyBaseClass
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D enemyCollider;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private float frequency = 1f;
    [SerializeField] private float wallCheckDistance = 0.5f;

    [Header("Fly Zone (level bounds)")]
    [SerializeField] private Collider2D flyZoneBounds;
    [SerializeField] private float padding = 0.2f;

    [Header("Player Bounce")]
    [SerializeField] private float bounceForce = 10f;
    [SerializeField] private float bounceThreshold = 0.2f;

    [Header("Layers")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask playerLayer;

    private bool facingRight;
    private float baseY;
    private float time;
    private float phaseOffset;

    private void Awake()
    {
        baseY = transform.position.y;
        phaseOffset = Random.Range(0f, 100f);
        facingRight = transform.localScale.x >= 0f;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Movement()
    {
        if (rb == null || flyZoneBounds == null) return;

        time += Time.fixedDeltaTime;
        float dir = facingRight ? 1f : -1f;

        float newX = rb.position.x + (dir * moveSpeed * Time.fixedDeltaTime);

        float wave = Mathf.Sin((time + phaseOffset) * frequency * Mathf.PI * 2f) * amplitude;
        float newY = baseY + wave;

        Vector2 nextPos = new Vector2(newX, newY);

        // Check for walls before moving
        Vector2 wallCheckDirection = new Vector2(dir, 0f);
        bool hitWall = Physics2D.Raycast(transform.position, wallCheckDirection, wallCheckDistance, wallLayer);

        // Clamp inside fly zone bounds
        Bounds b = flyZoneBounds.bounds;
        float minX = b.min.x + padding;
        float maxX = b.max.x - padding;
        float minY = b.min.y + padding;
        float maxY = b.max.y - padding;

        // Flip if hit wall OR reached bounds edge
        if (hitWall || nextPos.x <= minX)
        {
            nextPos.x = Mathf.Max(nextPos.x, minX);
            facingRight = true;
            Flip();
        }
        else if (nextPos.x >= maxX)
        {
            nextPos.x = maxX;
            facingRight = false;
            Flip();
        }

        nextPos.y = Mathf.Clamp(nextPos.y, minY, maxY);

        rb.MovePosition(nextPos);
    }

    private void Flip()
    {
        Vector3 scale = transform.localScale;
        float absX = Mathf.Abs(scale.x);
        scale.x = facingRight ? absX : -absX;
        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb == null || playerRb.linearVelocity.y >= 0f) return;

        float playerY = collision.transform.position.y;
        float enemyTopThreshold = transform.position.y + bounceThreshold;

        if (playerY > enemyTopThreshold)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
            playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
        }
    }
}
