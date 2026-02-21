using UnityEngine;

public class ConveyorGround : MonoBehaviour
{
    [Header("Belt Settings")]
    [SerializeField] private float beltSpeed = 3f;
    [SerializeField] private bool moveRight = true;

    private Collider2D solidCollider;
    private Collider2D triggerCollider;
    private Player currentPlayer;

    private void Awake()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (Collider2D col in cols)
        {
            if (col.isTrigger) triggerCollider = col;
            else solidCollider = col;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (solidCollider == null) return;

        float beltTop = solidCollider.bounds.max.y;
        float otherBottom = other.bounds.min.y;

        if (otherBottom > beltTop + 0.1f) return;
        if (otherBottom < beltTop - 0.3f) return;

        float dir = moveRight ? 1f : -1f;

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            currentPlayer = player;
            player.SetBeltVelocity(dir * beltSpeed);
            return;
        }

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        if (rb.bodyType == RigidbodyType2D.Static) return;

        float targetX = Mathf.MoveTowards(rb.linearVelocity.x, dir * beltSpeed, beltSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.ClearBeltVelocity();
            currentPlayer = null;
        }
    }

    public void SetDirection(bool right) => moveRight = right;
    public void SetSpeed(float speed) => beltSpeed = speed;
}
