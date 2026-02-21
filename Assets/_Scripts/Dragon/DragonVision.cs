using UnityEngine;

public class DragonVision : MonoBehaviour
{
    private EnemyDragon dragon;

    private float debounceThreshold = 1.2f;
    private float lastFire = 0.0f;

    private void Start()
    {
        dragon = transform.parent.GetComponent<EnemyDragon>();
    }

    private void FixedUpdate()
    {
        lastFire += Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (lastFire >= debounceThreshold)
        {
            Player player = collider.GetComponent<Player>();

            if (player != null)
            {
                StartCoroutine(dragon.Attack());

                lastFire = 0.0f;
            }
        }
    }
}