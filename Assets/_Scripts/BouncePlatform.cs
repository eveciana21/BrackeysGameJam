using UnityEngine;

public class BouncePlatform : MonoBehaviour
{
    [SerializeField] private float bounceVelocity = 14f;
    [SerializeField] private LayerMask playerLayer;

    [SerializeField] private Animator uncleAnimator;

    private void OnCollisionEnter2D(Collision2D collision) => TryBounce(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryBounce(collision);

    private void TryBounce(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        Rigidbody2D playerRb = collision.rigidbody;
        if (playerRb == null) return;

        //  Use relative velocity (impact speed), not rb velocity after resolution
        if (collision.relativeVelocity.y <= -0.1f) // player moving down into the platform
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player == null) return;

            player.ApplyBounceLaunch(bounceVelocity);

            if (uncleAnimator != null)
            {
                uncleAnimator.ResetTrigger("Bounce");
                uncleAnimator.SetTrigger("Bounce");
            }
        }
    }
    /*    private void TryBounce(Collision2D collision)
        {
            if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;
            if (collision.rigidbody == null) return;
            if (collision.rigidbody.linearVelocity.y >= 0f) return;

            Player player = collision.gameObject.GetComponent<Player>();
            if (player == null) return;

            player.ApplyBounceLaunch(bounceVelocity);

            if (uncleAnimator != null)
            {
                uncleAnimator.ResetTrigger("Bounce");
                uncleAnimator.SetTrigger("Bounce");
            }
        }*/
}
