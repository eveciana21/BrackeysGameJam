using UnityEngine;

public class ProjectileSpin : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    [Header("Spin")]
    [SerializeField] private float minSpin = -360f; // degrees/sec
    [SerializeField] private float maxSpin = 360f;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    public void Throw(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);

        float spin = Random.Range(minSpin, maxSpin);
        rb.angularVelocity = spin; // degrees per second
    }
}
