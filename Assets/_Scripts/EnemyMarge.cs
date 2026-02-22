using System.Collections;
using UnityEngine;

public class EnemyMarge : EnemyBaseClass
{
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;
    [SerializeField] private Animator animator;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Timing")]
    [SerializeField] private float minFireInterval = 1.0f;
    [SerializeField] private float maxFireInterval = 2.0f;
    [SerializeField] private float startDelay = 0.25f;

    [Header("Projectile Settings")]
    [SerializeField] private float projectileSpeed = 8f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Damage Flash")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private float flashInterval = 0.08f;
    [SerializeField] private float invincibilityTime = 0.15f;

    [Header("Head Hit Sprite")]
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private Sprite headHitSprite;
    [SerializeField] private float headHitSpriteDuration = 0.35f;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float contactKnockbackX = 8f;
    [SerializeField] private float contactKnockbackY = 6f;
    [SerializeField] private float contactCooldown = 0.2f;

    [SerializeField] private AudioClip damageSfx;
    [SerializeField] private AudioClip shootSfx;

    private float nextContactTime;

    private Sprite originalHeadSprite;
    private Coroutine headSpriteRoutine;

    private Color originalBodyColor;
    private Color originalHeadColor;

    private int currentHealth;
    private bool isInvincible;
    private bool hasAppliedStartDelay;
    private Color originalColor;
    private Coroutine flashRoutine;

    private Coroutine shootRoutine;

    private void Awake()
    {
        if (firePoint == null)
        {
            firePoint = transform;
        }

        currentHealth = maxHealth;

        if (bodyRenderer != null)
        {
            originalBodyColor = bodyRenderer.color;
        }

        if (headRenderer != null)
        {
            originalHeadColor = headRenderer.color;
            originalHeadSprite = headRenderer.sprite;
        }
    }

    private void OnEnable()
    {
        shootRoutine = StartCoroutine(ShootLoop());
    }

    private void OnDisable()
    {
        if (shootRoutine != null)
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
        }
    }

    private IEnumerator ShootLoop()
    {
        if (!hasAppliedStartDelay && startDelay > 0f)
        {
            hasAppliedStartDelay = true;
            yield return new WaitForSeconds(startDelay);
        }

        while (true)
        {
            if (projectilePrefab != null)
            {
                Shoot();
            }

            float min = Mathf.Min(minFireInterval, maxFireInterval);
            float max = Mathf.Max(minFireInterval, maxFireInterval);
            float waitTime = Random.Range(min, max);

            yield return new WaitForSeconds(waitTime);
        }
    }

    private void Shoot()
    {
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            // Use the arm’s direction (pick the sign that matches your ShootPoint)
            Vector2 direction = -firePoint.right;
            projectile.Launch(direction, projectileSpeed);
            coreManagersChannel.audioManager.PlaySFX(shootSfx);
        }
    }

    // Call this when hit by player projectile
    public void ApplyDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        StartCoroutine(InvincibilityTimer());
        StartDamageFlash();

        StartHeadHitSprite();

        if (coreManagersChannel != null)
            coreManagersChannel.audioManager.PlaySFX(damageSfx);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void StartHeadHitSprite()
    {
        if (headRenderer == null) return;
        if (headHitSprite == null) return;

        if (headSpriteRoutine != null)
        {
            StopCoroutine(headSpriteRoutine);
        }

        headSpriteRoutine = StartCoroutine(HeadHitSpriteCoroutine());
    }

    private IEnumerator HeadHitSpriteCoroutine()
    {
        headRenderer.sprite = headHitSprite;

        yield return new WaitForSeconds(headHitSpriteDuration);

        // Restore original
        if (headRenderer != null)
        {
            headRenderer.sprite = originalHeadSprite;
        }

        headSpriteRoutine = null;
    }

    private IEnumerator InvincibilityTimer()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }

    private void StartDamageFlash()
    {
        if (bodyRenderer == null && headRenderer == null) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(DamageFlashCoroutine());
    }

    private IEnumerator DamageFlashCoroutine()
    {
        Color red = Color.red;

        if (bodyRenderer != null) bodyRenderer.color = red;
        if (headRenderer != null) headRenderer.color = red;

        yield return new WaitForSeconds(flashInterval);

        if (bodyRenderer != null) bodyRenderer.color = originalBodyColor;
        if (headRenderer != null) headRenderer.color = originalHeadColor;

        flashRoutine = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayerOnContact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayerOnContact(other);
    }

    private void TryDamagePlayerOnContact(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < nextContactTime) return;

        Player player = other.GetComponent<Player>();
        if (player == null) return;

        // Knock player away from Marge (based on relative position)
        float dirX = (player.transform.position.x >= transform.position.x) ? 1f : -1f;
        Vector2 knockback = new Vector2(dirX * contactKnockbackX, contactKnockbackY);

        player.DamagePlayer(contactDamage);
        player.ApplyKnockback(knockback);

        nextContactTime = Time.time + contactCooldown;
    }

    private void Die()
    {
        NotifyDeath();
        animator.SetTrigger("Death");
        Destroy(gameObject, 2f);
    }
}
