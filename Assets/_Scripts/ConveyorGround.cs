using UnityEngine;

public class ConveyorGround : MonoBehaviour
{
    [Header("Belt Settings")]
    [SerializeField] private float beltSpeed = 3f;
    [SerializeField] private bool moveRight = true;

    [Header("Visual Scrolling")]
    // Assign two child GameObjects with SpriteRenderers — they leapfrog each other
    // to create a seamless infinite treadmill loop. Make each one the full belt width.
    [SerializeField] private Transform beltVisualA;
    [SerializeField] private Transform beltVisualB;
    [SerializeField] private float visualScrollSpeed = 3f;

    private Collider2D solidCollider;
    private Collider2D triggerCollider;

    private float beltWidth;
    private Player currentPlayer;

    private void Awake()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (Collider2D col in cols)
        {
            if (col.isTrigger) triggerCollider = col;
            else solidCollider = col;
        }

        if (solidCollider != null)
            beltWidth = solidCollider.bounds.size.x;

        // Position B directly to the right (or left) of A to start
        if (beltVisualA != null && beltVisualB != null)
        {
            Vector3 bPos = beltVisualA.localPosition;
            bPos.x += moveRight ? -beltWidth : beltWidth;
            beltVisualB.localPosition = bPos;
        }
    }

    private void Update()
    {
        ScrollVisuals();
    }

    private void ScrollVisuals()
    {
        if (beltVisualA == null || beltVisualB == null) return;

        float dir = moveRight ? 1f : -1f;
        float delta = dir * visualScrollSpeed * Time.deltaTime;

        beltVisualA.localPosition += new Vector3(delta, 0f, 0f);
        beltVisualB.localPosition += new Vector3(delta, 0f, 0f);

        // When a tile scrolls fully off one end, jump it to the other end (leapfrog)
        if (moveRight)
        {
            if (beltVisualA.localPosition.x > beltWidth * 0.5f)
                beltVisualA.localPosition -= new Vector3(beltWidth * 2f, 0f, 0f);
            if (beltVisualB.localPosition.x > beltWidth * 0.5f)
                beltVisualB.localPosition -= new Vector3(beltWidth * 2f, 0f, 0f);
        }
        else
        {
            if (beltVisualA.localPosition.x < -beltWidth * 0.5f)
                beltVisualA.localPosition += new Vector3(beltWidth * 2f, 0f, 0f);
            if (beltVisualB.localPosition.x < -beltWidth * 0.5f)
                beltVisualB.localPosition += new Vector3(beltWidth * 2f, 0f, 0f);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (solidCollider == null) return;

        float beltTop = solidCollider.bounds.max.y;
        float otherBottom = other.bounds.min.y;

        // Only affect objects resting on the top surface
        if (otherBottom > beltTop + 0.1f) return;
        if (otherBottom < beltTop - 0.3f) return;

        float dir = moveRight ? 1f : -1f;

        // Player gets special treatment so belt velocity survives PlayerMovement()
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            currentPlayer = player;
            player.SetBeltVelocity(dir * beltSpeed);
            return;
        }

        // All other Rigidbody2D objects (enemies, props) get directly nudged
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        if (rb.bodyType == RigidbodyType2D.Static) return;

        // Clamp to beltSpeed so velocity doesn't accumulate indefinitely
        float targetX = Mathf.MoveTowards(rb.linearVelocity.x, dir * beltSpeed, beltSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Clear belt velocity when player steps off
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.ClearBeltVelocity();
            currentPlayer = null;
        }
    }

    public void SetDirection(bool right)
    {
        moveRight = right;
    }

    public void SetSpeed(float speed)
    {
        beltSpeed = speed;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = moveRight ? Color.cyan : Color.yellow;
        Vector3 center = col.bounds.center;
        Vector3 arrowEnd = center + (moveRight ? Vector3.right : Vector3.left) * 1.5f;
        Gizmos.DrawLine(center, arrowEnd);
        Gizmos.DrawSphere(arrowEnd, 0.15f);
    }
#endif
}
