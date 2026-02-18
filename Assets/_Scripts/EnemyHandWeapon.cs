using UnityEngine;

public class EnemyHandWeapon : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask playerLayer;

    private bool hasHitThisSwing;

    private void OnEnable()
    {
        // Each time the collider gets enabled by the animation,
        // allow one hit again.
        hasHitThisSwing = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitThisSwing) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        Debug.Log("Attack");
        /*        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
                if (health == null) return;

                 health.TakeDamage(damage);
        */

        hasHitThisSwing = true;
    }
}
