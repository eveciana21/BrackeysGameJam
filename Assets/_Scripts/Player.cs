using System;
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

    [Header("Throw")]
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwSpeed = 10f;
    [SerializeField] private float throwUpwardBoost = 2.5f;
    [SerializeField] private float throwCooldown = 0.35f;

    [Header("Projectile Selection")]
    [SerializeField] private bool useWeightedRandom = false;
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

    private Sprite currentProjectileSprite;

    private HealthIndicator healthIndicator;

    private int currentHealth;
    private bool isInvincible = false;
    private float timeSinceMadeInvincible = 0.0f;

    private void Start()
    {
        currentHealth = initHealth;

        healthIndicator = healthIndicatorUI.GetComponent<HealthIndicator>();
        healthIndicator.SetHealth(currentHealth);
    }

    private void FixedUpdate()
    {
        ChickIfInvincible();
        CheckIfGrounded();
        PlayerMovement();
        ApplyGravityMultiplier();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        FlipPlayer();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            isJumpHeld = true;
            PerformJump();
        }
        else if (context.canceled)
        {
            isJumpHeld = false;
        }
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
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

        Rigidbody2D projRb = projObj.GetComponent<Rigidbody2D>();
        if (projRb == null) return;

        float facingDir = transform.localScale.x >= 0f ? 1f : -1f;
        Vector2 launchVelocity = new Vector2(facingDir * throwSpeed, throwUpwardBoost);

        projRb.linearVelocity = launchVelocity;
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

    private void CheckIfGrounded()
    {
        Vector2 checkPosition = new Vector2(transform.position.x, playerCollider.bounds.min.y + groundCheckOffset);
        isGrounded = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer);
    }

    private void PlayerMovement()
    {
        float targetVelX = Mathf.Clamp(moveInput.x, -1f, 1f) * playerSpeed;
        rb.linearVelocity = new Vector2(targetVelX, rb.linearVelocity.y);
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

    private void ChickIfInvincible()
    {
        if (isInvincible)
        {
            timeSinceMadeInvincible += Time.deltaTime;

            if (timeSinceMadeInvincible >= invincibilityTime)
            {
                isInvincible = false;
                timeSinceMadeInvincible = 0.0f;
            }
        }
    }

    public void DamagePlayer(int damageDealt = 1)
    {
        if (isInvincible) return;

        Debug.Log(damageDealt + " damage dealt");

        currentHealth = Math.Max(0, currentHealth - damageDealt);
        isInvincible = true;

        healthIndicator.SetHealth(currentHealth);

        if (currentHealth == 0)
        {
            killPlayer();
            return;
        }

        // Trigger damage animation here
    }

    private void killPlayer()
    {
        Debug.Log("Player died");
        // Trigger death animation/logic here

    }
}
