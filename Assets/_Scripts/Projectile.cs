using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private bool randomizeSpriteOnSpawn = false;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] randomSprites;

    [Header("Damage")]
    [SerializeField] private int damage = 1;

    [Header("Lifetime")]
    [SerializeField] private bool useLifetime = true;
    [SerializeField] private float lifeTime = 3f;

    [Header("Ground Behavior (lifetime ON only)")]
    [SerializeField] private float groundDampen = 0.6f;

    [Header("Impact Animation (lifetime OFF only)")]
    [SerializeField] private Animator animator;

    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;

    private bool canDamage = true;
    private bool hasHitGround;
    private bool hasImpacted;

    private Collider2D myCol;
    private Rigidbody2D rb;

    private void Awake()
    {
        myCol = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        ApplyRandomSprite();

        // For impact-style projectiles, keep animator OFF until impact
        if (!useLifetime && animator != null)
        {
            animator.enabled = false;
        }
    }

    private void Start()
    {
        if (useLifetime)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasImpacted) return;

        int otherMask = 1 << collision.gameObject.layer;
        bool hitGround = (otherMask & groundLayer) != 0;
        bool hitEnemy = (otherMask & enemyLayer) != 0;

        // Lifetime OFF: impact on ground/enemy -> play impact animation -> destroy via Animation Event
        if (!useLifetime)
        {
            if (hitEnemy)
            {
                ApplyDamageIfPossible(collision);
                Impact();
            }
            else if (hitGround)
            {
                Impact();
            }

            return;
        }

        // Lifetime ON: keep your original behavior
        if (!hasHitGround && hitGround)
        {
            hasHitGround = true;
            canDamage = false;

            if (rb != null)
            {
                rb.linearVelocity *= groundDampen;
                rb.angularVelocity *= groundDampen;
            }

            return;
        }

        if (!canDamage) return;
        if (!hitEnemy) return;

        ApplyDamageIfPossible(collision);
        Destroy(gameObject);
    }

    private void ApplyRandomSprite()
    {
        if (!randomizeSpriteOnSpawn) return;
        if (spriteRenderer == null) return;
        if (randomSprites == null) return;
        if (randomSprites.Length == 0) return;

        int index = Random.Range(0, randomSprites.Length);
        spriteRenderer.sprite = randomSprites[index];
    }

    private void ApplyDamageIfPossible(Collision2D collision)
    {
        // If you hit a child collider, try parent first too
        Rat rat = collision.gameObject.GetComponentInParent<Rat>();
        if (rat != null)
        {
            rat.ApplyDamage(damage);
            return;
        }

        EnemyLeg leg = collision.gameObject.GetComponentInParent<EnemyLeg>();
        if (leg != null)
        {
            leg.ApplyDamage(damage);
            return;
        }

        EnemyBasic enemy = collision.gameObject.GetComponentInParent<EnemyBasic>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
        }
    }

    private void Impact()
    {
        hasImpacted = true;
        canDamage = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (myCol != null)
        {
            myCol.enabled = false;
        }

        if (animator != null)
        {
            animator.enabled = true; // animation starts immediately on enable (default state)
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Animation Event will call this at the end of the impact animation
    public void DestroySelf() 
    {
        Destroy(gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Keep your “pass through enemies after ground hit” behavior (lifetime ON only)
        if (!useLifetime) return;
        if (!hasHitGround) return;
        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;

        Collider2D enemyCol = collision.collider;
        if (enemyCol != null && myCol != null)
        {
            Physics2D.IgnoreCollision(myCol, enemyCol, true);
        }
    }
}
