using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundDampen = 0.6f;
    [SerializeField] private float lifeTime = 3f;

    private bool canDamage = true;
    private bool hasHitGround;
    private Collider2D myCol;

    private void Awake()
    {
        myCol = GetComponent<Collider2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int otherMask = 1 << collision.gameObject.layer;

        // Ground hit -> harmless + slows down a bit
        if (!hasHitGround && (otherMask & groundLayer) != 0)
        {
            hasHitGround = true;
            canDamage = false;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity *= groundDampen;
                rb.angularVelocity *= groundDampen;
            }

            return;
        }

        // Enemy hit BEFORE ground hit -> damage and destroy
        if (!canDamage) return;
        if ((otherMask & enemyLayer) == 0) return;

        EnemyBasic enemy = collision.gameObject.GetComponentInParent<EnemyBasic>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
        }

        Destroy(gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // After ground hit, make enemies pass through by ignoring their colliders
        if (!hasHitGround) return;

        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;

        Collider2D enemyCol = collision.collider;
        if (enemyCol != null && myCol != null)
        {
            Physics2D.IgnoreCollision(myCol, enemyCol, true);
        }
    }
}
