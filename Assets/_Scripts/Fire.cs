using UnityEngine;

public class Fire : MonoBehaviour
{
    private bool isPlayerInFire = false;

    private void Update()
    {
        if (isPlayerInFire)
        {
            GameObject.Find("Player")?.GetComponent<Player>()?.DamagePlayer(1);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        Player player = collider.gameObject.GetComponent<Player>();

        if (player == null) return;

        isPlayerInFire = true;
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        Player player = collider.gameObject.GetComponent<Player>();

        if (player == null) return;

        isPlayerInFire = false;
    }
}