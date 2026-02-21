using UnityEngine;

public class EnemyHandWeapon : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask playerLayer;

    private bool hasHitThisSwing;

    private void OnEnable()
    {
        hasHitThisSwing = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitThisSwing) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            player = other.GetComponentInParent<Player>();
        }

        if (player == null) return;

        player.DamagePlayer(damage); 
        hasHitThisSwing = true;
    }
}
