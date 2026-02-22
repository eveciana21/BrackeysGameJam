using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

public class EnemyDragon : EnemyBaseClass
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Fly Mechanics")]
    [SerializeField] private float waitDuration = 2.0f;
    [SerializeField] private float distThreshold = 0.1f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int hitsBeforeStun = 3;
    [SerializeField] private float stunLength = 2.0f;

    [Header("Player Bounce")]
    [SerializeField] private float bounceForce = 10f;

    [Header("Stomp Difficulty")]
    [SerializeField] private float stompKnockbackX = 3f;            // sideways push on stomp
    [SerializeField] private float moveSpeedIncreaseOnStomp = 0.5f; // enemy gets faster per stomp
    [SerializeField] private float maxMoveSpeed = 5f;

    [Header("Entry")]
    [SerializeField] private float entrySpeed = 3f; // units per second toward spline start

    [Header("Damage")]
    [SerializeField] private float damageFlashTime = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private AudioClip damageSfx;
    [SerializeField] private AudioClip roarSfx;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Roar")]
    [SerializeField] private float roarLength = 2.0f;
    [SerializeField] private GameObject dragonPlatform;

    private SplineAnimate splineAnimate;
    private Spline spline;
    private SplineContainer injectedContainer;

    private Animator animator;
    private SpriteRenderer bodyRenderer;

    private bool waitingAtTop = false;
    private bool hasAttacked = false;

    private float currentHealth;
    private float lastStompTime;
    private float stompCooldown = 0.1f;

    private bool isStunned = false;
    private int stunCounter = 0;
    private bool isFlyingBack = false;

    private bool isRoaring = false;

    private Coroutine flashRoutine;
    private Coroutine waitAtTopCoroutine;
    private Coroutine attackCoroutine;
    private Coroutine stunCoroutine;
    private Coroutine roarCoroutine;

    //  Flash safety
    private Color cachedOriginalColor;
    private bool hasCachedColor;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void OnDisable()
    {
        // if the component gets disabled while red, force reset
        ResetFlashColor();
    }

    // Call this immediately after Instantiate, before Start() fires
    public void SetSplineContainer(SplineContainer container)
    {
        injectedContainer = container;
    }

    private void Start()
    {
        splineAnimate = GetComponent<SplineAnimate>();
        animator = transform.Find("DragonSprite").GetComponent<Animator>();
        bodyRenderer = transform.Find("DragonSprite").GetComponent<SpriteRenderer>();

        CacheOriginalColor();

        spline = splineAnimate.Container.Spline;

        if (coreManagersChannel != null)
            coreManagersChannel.audioManager.PlaySFX(roarSfx);
    }

    private void CacheOriginalColor()
    {
        if (bodyRenderer == null) return;
        if (hasCachedColor) return;

        cachedOriginalColor = bodyRenderer.color;
        hasCachedColor = true;
    }

    private void ResetFlashColor()
    {
        if (bodyRenderer == null) return;

        CacheOriginalColor();
        bodyRenderer.color = cachedOriginalColor;
    }

    private void FixedUpdate()
    {
        Vector3 lastPosition = (Vector3)spline.Last().Position + splineAnimate.Container.transform.position;
        float dist = Vector3.Distance(lastPosition, transform.position);

        if (isFlyingBack)
        {
            float speed = 3.0f;
            Vector3 pathBack = lastPosition - transform.position;

            transform.position += Vector3.Normalize(pathBack) * Math.Min(speed * Time.deltaTime, dist);
        }

        if (dist <= distThreshold && !waitingAtTop)
        {
            waitingAtTop = true;
            isFlyingBack = false;
            hasAttacked = false;

            Vector3 newScale = transform.localScale;
            newScale.x *= -1;
            transform.localScale = newScale;

            if (isRoaring)
            {
                roarCoroutine = StartCoroutine(RoarCoroutine());
            }
            else
            {
                waitAtTopCoroutine = StartCoroutine(WaitAtTop());
            }
        }
    }

    public void Init()
    {
        splineAnimate.Play();
    }

    private IEnumerator WaitAtTop()
    {
        SplineUtility.ReverseFlow(spline);

        splineAnimate.Pause();

        yield return new WaitForSeconds(waitDuration);

        splineAnimate.Restart(true);
        waitingAtTop = false;
    }

    public void Attack()
    {
        attackCoroutine = StartCoroutine(AttackCoroutine());
    }

    private IEnumerator AttackCoroutine()
    {
        if (!hasAttacked && !waitingAtTop && !isStunned)
        {
            splineAnimate.Pause();
            animator.SetTrigger("attack");

            yield return new WaitForSeconds(1.0f);

            splineAnimate.Play();

            hasAttacked = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (((1 << collider.gameObject.layer) & playerLayer) == 0) return;
        if (currentHealth <= 0) return; // Don't process stomps if already dead
        if (Time.time < lastStompTime + stompCooldown) return; // Prevent rapid double-stomps

        Rigidbody2D playerRb = collider.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        if (
            playerRb.linearVelocity.y < 0f &&
            collider.transform.position.y + collider.bounds.min.y >= bodyRenderer.bounds.max.y + bodyRenderer.transform.position.y - 0.1f
        )
        {
            lastStompTime = Time.time;

            float knockDir = (playerRb.position.x >= rb.position.x) ? 1f : -1f;

            playerRb.linearVelocity = new Vector2(knockDir * stompKnockbackX, 0f);
            playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

            TakeDamage(1);
        }
    }

    private void TakeDamage(int amount)
    {
        if (currentHealth <= 0 || isStunned || isRoaring) return;

        currentHealth -= amount;

        FlashDamage();
        coreManagersChannel.audioManager.PlaySFX(damageSfx);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        stunCounter++;

        if (stunCounter >= hitsBeforeStun && !isStunned)
        {
            stunCoroutine = StartCoroutine(Stun());
        }
    }

    private void FlashDamage()
    {
        if (bodyRenderer == null) return;

        CacheOriginalColor();

        // restart flash each hit (prevents “stuck red” due to overlapping timing)
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        flashRoutine = StartCoroutine(FlashDamageRoutine());
    }

    private IEnumerator FlashDamageRoutine()
    {
        if (bodyRenderer == null) yield break;

        bodyRenderer.color = damageFlashColor;

        yield return new WaitForSeconds(damageFlashTime);

        ResetFlashColor();
        flashRoutine = null;
    }

    public void Die()
    {
        // ensure we never die while stuck red
        ResetFlashColor();

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (waitAtTopCoroutine != null) StopCoroutine(waitAtTopCoroutine);
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        if (roarCoroutine != null) StopCoroutine(roarCoroutine);

        NotifyDeath(true);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        animator.SetTrigger("death");

        enabled = false;

        Destroy(gameObject, 1f);
    }

    public void ApplyDamage(int amount)
    {
        TakeDamage(amount);
    }

    private IEnumerator Stun()
    {
        isStunned = true;
        waitingAtTop = false;

        if (waitAtTopCoroutine != null) StopCoroutine(waitAtTopCoroutine);
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        if (roarCoroutine != null) StopCoroutine(roarCoroutine);

        splineAnimate.Pause();
        animator.SetTrigger("stun");

        rb.bodyType = RigidbodyType2D.Dynamic;

        yield return new WaitForSeconds(stunLength);

        animator.SetTrigger("idle");

        rb.bodyType = RigidbodyType2D.Kinematic;

        isFlyingBack = true;

        yield return new WaitUntil(() => !isFlyingBack);

        isStunned = false;
        stunCounter = 0;
    }

    public void Roar()
    {
        isRoaring = true;

        if (coreManagersChannel != null)
            coreManagersChannel.audioManager.PlaySFX(roarSfx);
    }

    private IEnumerator RoarCoroutine()
    {
        isStunned = false;
        animator.SetTrigger("roar");

        yield return new WaitForSeconds(roarLength / 2.0f);

        dragonPlatform.GetComponentInChildren<PlatformDisable>().Roar();

        yield return new WaitForSeconds(roarLength / 2.0f);

        animator.SetTrigger("idle");

        isRoaring = false;

        waitAtTopCoroutine = StartCoroutine(WaitAtTop());
    }
}