using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

public class EnemyDragon : EnemyBaseClass
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Fly Mechanics")]
    [SerializeField] private float waitDuration = 2.0f;
    [SerializeField] private float distThreshold = 0.1f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Player Bounce")]
    [SerializeField] private float bounceForce = 10f;

    [Header("Stomp Difficulty")]
    [SerializeField] private float stompKnockbackX = 3f;          // sideways push on stomp
    [SerializeField] private float moveSpeedIncreaseOnStomp = 0.5f; // enemy gets faster per stomp
    [SerializeField] private float maxMoveSpeed = 5f;

    [Header("Damage")]
    [SerializeField] private float damageFlashTime = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private AudioClip damageSfx;
    [SerializeField] private AudioClip deathSFX;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;

    private SplineAnimate splineAnimate;
    private Spline spline;

    private Animator animator;
    private SpriteRenderer bodyRenderer;

    private bool waitingAtTop = false;
    private bool hasAttacked = false;

    private float currentHealth;
    private float lastStompTime;
    private float stompCooldown = 0.1f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        splineAnimate = GetComponent<SplineAnimate>();
        animator = transform.Find("DragonSprite").GetComponent<Animator>();
        bodyRenderer = transform.Find("DragonSprite").GetComponent<SpriteRenderer>();

        if (splineAnimate != null)
        {
            spline = splineAnimate.Container.Spline;
            splineAnimate.Play();
        }
    }

    private void FixedUpdate()
    {
        Vector3 lastPosition = (Vector3)spline.Last().Position + splineAnimate.Container.transform.position;
        float dist = Vector3.Distance(lastPosition, transform.position);

        if (dist <= distThreshold && !waitingAtTop)
        {
            waitingAtTop = true;
            SplineUtility.ReverseFlow(spline);
            StartCoroutine(WaitAtTop());
        }
    }

    private IEnumerator WaitAtTop()
    {
        hasAttacked = false;

        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;

        splineAnimate.Pause();

        yield return new WaitForSeconds(waitDuration);

        splineAnimate.Restart(true);
        waitingAtTop = false;
    }

    public IEnumerator Attack()
    {
        if (!hasAttacked && !waitingAtTop)
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

        animator.SetTrigger("death");

        enabled = false;

        Destroy(gameObject, 1f);
    }

    public void ApplyDamage(int amount)
    {
        TakeDamage(amount);
    }
}