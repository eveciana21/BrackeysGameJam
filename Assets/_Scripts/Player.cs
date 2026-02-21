using System;
using System.Collections;
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
    [SerializeField] private int initHealth = 5;
    [SerializeField] private float invincibilityTime = 1.0f;

    [Header("Jump Properties")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Knockback")]
    [SerializeField] private float knockbackLockTime = 0.18f;

    [Header("Damage Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float redFlashInterval = 0.08f;
    [SerializeField] private float flickerInterval = 0.15f;
    [SerializeField] private Color flashColor = Color.red;

    private Color originalColor;
    private Coroutine flashRoutine;

    private float knockbackTimeRemaining;
    private Vector2 knockbackVelocity;

    [Header("Throw")]
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwSpeed = 10f;
    [SerializeField] private float throwUpwardBoost = 2.5f;
    [SerializeField] private float throwCooldown = 0.35f;

    [Header("Projectile Selection")]
    [SerializeField] private GameObject projectilePrefab;

    private GameObject[] currentProjectilePrefabs;
    private float nextThrowTime;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundCheckOffset = 0.1f;

    [Header("UI Components")]
    [SerializeField] private GameObject healthIndicatorUI;

    private Vector2 moveInput;
    private bool isJumpHeld;
    private bool isGrounded;
    private bool isKnockbackInvincible = false;
    private float beltVelocityX;

    private float bounceGraceTime = 0f;

    private Sprite currentProjectileSprite;

    private HealthIndicator healthIndicator;

    private int currentHealth;
    private bool isInvincible = false;
    private float timeSinceMadeInvincible = 0.0f;

    private bool isDead;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Start()
    {
        currentHealth = initHealth;

        healthIndicator = healthIndicatorUI.GetComponent<HealthIndicator>();
        healthIndicator.SetHealth(currentHealth);

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void FixedUpdate()
    {
        CheckIfInvincible();
        CheckIfGrounded();

        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
        }

        if (isDead)
        {
            UpdateAnimator();
            return;
        }

        if (knockbackTimeRemaining > 0f)
        {
            knockbackTimeRemaining -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(knockbackVelocity.x, rb.linearVelocity.y);
        }
        else
        {
            PlayerMovement();
        }

        ApplyGravityMultiplier();
        UpdateAnimator();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (isDead) return;

        moveInput = context.ReadValue<Vector2>();
        FlipPlayer();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isDead) return;

        if (context.performed && isGrounded)
        {
            isJumpHeld = true;
            PerformJump();

            if (animator != null)
            {
                animator.ResetTrigger("Jump");
                animator.SetTrigger("Jump");
            }
        }
        else if (context.canceled)
        {
            isJumpHeld = false;
        }
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (isDead) return;
        if (!context.performed) return;
        if (Time.time < nextThrowTime) return;

        nextThrowTime = Time.time + throwCooldown;
        ThrowProjectile();
    }

    private void ThrowProjectile()
    {
        if (throwPoint == null) return;
        if (currentProjectilePrefabs == null) return;
        if (currentProjectilePrefabs.Length == 0) return;

        int index = UnityEngine.Random.Range(0, currentProjectilePrefabs.Length);
        GameObject prefabToUse = currentProjectilePrefabs[index];
        if (prefabToUse == null) return;

        GameObject projObj = Instantiate(prefabToUse, throwPoint.position, Quaternion.identity);

        float facingDir = transform.localScale.x >= 0f ? 1f : -1f;
        Vector2 launchVelocity = new Vector2(facingDir * throwSpeed, throwUpwardBoost);

        ProjectileSpin spin = projObj.GetComponent<ProjectileSpin>();
        if (spin != null)
        {
            // Convert your velocity into an impulse-like force
            spin.Throw(launchVelocity);
        }
        else
        {
            Rigidbody2D projRb = projObj.GetComponent<Rigidbody2D>();
            if (projRb == null) return;
            projRb.linearVelocity = launchVelocity;
        }
    }

    public void SetProjectilePrefabs(GameObject[] prefabs)
    {
        currentProjectilePrefabs = prefabs;
    }

    public void SetProjectileSprite(Sprite newSprite)
    {
        currentProjectileSprite = newSprite;
    }

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void ApplyGravityMultiplier()
    {
        if (bounceGraceTime > 0f)
        {
            bounceGraceTime -= Time.fixedDeltaTime;
            return; // skip multiplier during grace period
        }

        float velocityY = rb.linearVelocity.y;
        float multiplier = 0f;

        if (velocityY < 0)
        {
            multiplier = fallMultiplier - 1f;
        }
        else if (velocityY > 0 && !isJumpHeld)
        {
            multiplier = lowJumpMultiplier - 1f;
        }

        rb.linearVelocity += Vector2.up * Physics2D.gravity.y * multiplier * Time.fixedDeltaTime;
    }

    public void ApplyBounceLaunch(float velocity)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocity);
        bounceGraceTime = 0.3f;

        if (animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.SetTrigger("Jump");
        }
    }

    private void CheckIfGrounded()
    {
        Vector2 checkPosition = new Vector2(transform.position.x, playerCollider.bounds.min.y + groundCheckOffset);
        isGrounded = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer);
    }

    private void PlayerMovement()
    {
        float targetVelX = Mathf.Clamp(moveInput.x, -1f, 1f) * playerSpeed;
        rb.linearVelocity = new Vector2(targetVelX + beltVelocityX, rb.linearVelocity.y);

        // Animate based on input only — belt movement doesn't trigger walk animation
        if (animator != null)
        {
            animator.SetBool("Walk", Mathf.Abs(moveInput.x) > 0.01f);
        }
    }

    private void FlipPlayer()
    {
        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (moveInput.x > 0f ? 1f : -1f);
            transform.localScale = scale;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        bool isInKnockback = knockbackTimeRemaining > 0f;
        bool shouldWalk = !isDead && !isInKnockback && isGrounded && Mathf.Abs(moveInput.x) > 0.01f;

        animator.SetBool("Walk", shouldWalk);
    }

    private void CheckIfInvincible()
    {
        if (!isInvincible) return;

        timeSinceMadeInvincible += Time.deltaTime;

        if (timeSinceMadeInvincible >= invincibilityTime)
        {
            isInvincible = false;
            isKnockbackInvincible = false; 
            timeSinceMadeInvincible = 0.0f;
        }
    }

    private void StartDamageFlash()
    {
        if (spriteRenderer == null) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(DamageFlashCoroutine());
    }

    private IEnumerator DamageFlashCoroutine()
    {
        if (spriteRenderer == null)
        {
            flashRoutine = null;
            yield break;
        }

        // Initial red flash
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(redFlashInterval);

        if (spriteRenderer == null) { flashRoutine = null; yield break; }
        spriteRenderer.color = originalColor;

        // Flicker by toggling renderer enabled
        float elapsed = redFlashInterval;

        while (elapsed < invincibilityTime)
        {
            if (spriteRenderer == null) { flashRoutine = null; yield break; }
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;

            if (spriteRenderer == null) { flashRoutine = null; yield break; }
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }

        flashRoutine = null;
    }

    public void DamagePlayer(int damageDealt = 1)
    {
        if (isInvincible) return;
        if (isDead) return;

        currentHealth = Math.Max(0, currentHealth - damageDealt);

        isInvincible = true;
        timeSinceMadeInvincible = 0.0f;
        StartDamageFlash(); // runs 2 alpha flickers

        healthIndicator.SetHealth(currentHealth);

        if (currentHealth == 0)
        {
            KillPlayer();
            return;
        }
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        if (isDead) return;
        if (isKnockbackInvincible) return;

        knockbackVelocity = knockback;
        knockbackTimeRemaining = knockbackLockTime;
        rb.linearVelocity = new Vector2(knockback.x, knockback.y);

        isKnockbackInvincible = true;
    }

    private void KillPlayer()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Player died");

        if (animator != null)
        {
            animator.SetBool("Walk", false);
            animator.ResetTrigger("Death");
            animator.SetTrigger("Death");
        }

        rb.linearVelocity = Vector2.zero;
    }

    // Called by MovingWalkway each FixedUpdate while player is on the belt
    public void SetBeltVelocity(float velocityX)
    {
        beltVelocityX = velocityX;
    }

    public void ClearBeltVelocity()
    {
        beltVelocityX = 0f;
    }

    public void LockMovement()
    {
        moveInput = Vector2.zero;
        var input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null) input.enabled = false;
    }

    public void UnlockMovement()
    {
        var input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null) input.enabled = true;
    }
}
