using System.Collections;
using UnityEngine;

public class PlatformDisable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D platformCollider; // Used By Effector collider (NOT trigger)
    [SerializeField] private Collider2D standTrigger;     // Trigger collider (IS trigger)
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Timing")]
    [SerializeField] private float standSeconds = 3f;
    [SerializeField] private float disabledSeconds = 3f;

    [Header("Flash")]
    [SerializeField] private int flashTimes = 5;
    [SerializeField] private float flashInterval = 0.08f;

    [Header("Top Check")]
    [SerializeField] private float topTolerance = 0.05f;

    [SerializeField] private GameObject dragon;

    private Collider2D playerCollider;
    private float standTimer;
    private bool sequenceRunning;
    private bool hasRoared = false;

    private void Awake()
    {
        if (platformCollider == null)
        {
            platformCollider = GetComponent<Collider2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (sequenceRunning) return;

        if (playerCollider == null)
        {
            standTimer = 0f;
            return;
        }

        if (IsPlayerOnTop())
        {
            standTimer += Time.deltaTime;

            if (standTimer >= standSeconds)
            {
                StartCoroutine(DropAndRestoreRoutine());
            }
        }
        else
        {
            standTimer = 0f;
        }
    }

    private bool IsPlayerOnTop()
    {
        if (playerCollider == null) return false;
        if (platformCollider == null) return false;

        float playerFeetY = playerCollider.bounds.min.y;
        float platformTopY = platformCollider.bounds.max.y;

        return playerFeetY >= platformTopY - topTolerance;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerCollider = other;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (other == playerCollider)
        {
            playerCollider = null;
            standTimer = 0f;
        }
    }

    private IEnumerator DropAndRestoreRoutine()
    {
        Debug.Log("Trigger roar");
        dragon.GetComponent<EnemyDragon>().Roar();

        yield return new WaitUntil(() => hasRoared == true);
        hasRoared = false;

        Debug.Log("Platform disappear");

        sequenceRunning = true;
        standTimer = 0f;

        // Flash before disappearing
        yield return StartCoroutine(FlashRoutine());

        // Disable BOTH collider + sprite so it's gone and you fall through
        if (platformCollider != null) platformCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        yield return new WaitForSeconds(disabledSeconds);

        // Turn sprite back on so we can flash it "coming in"
        if (spriteRenderer != null) spriteRenderer.enabled = true;

        // Flash again before becoming solid
        yield return StartCoroutine(FlashRoutine());

        // Re-enable collider (solid again)
        if (platformCollider != null) platformCollider.enabled = true;

        // Reset
        sequenceRunning = false;
        playerCollider = null;
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        for (int i = 0; i < flashTimes; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(flashInterval);

            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(flashInterval);
        }
    }

    public void Roar()
    {
        hasRoared = true;
    }
}
