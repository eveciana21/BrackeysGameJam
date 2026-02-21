using UnityEngine;

public class DragonAttack : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collider)
    {
        Player player = collider.GetComponent<Player>();

        if (player != null) player.DamagePlayer(2);
    }
}