using System.Collections;
using UnityEngine;

public class EnemyMarge : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Timing")]
    [SerializeField] private float fireInterval = 1.5f;
    [SerializeField] private float startDelay = 0.25f;

    [Header("Projectile Settings")]
    [SerializeField] private float projectileSpeed = 8f;

    private Coroutine shootRoutine;

    private void Awake()
    {
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void OnEnable()
    {
        shootRoutine = StartCoroutine(ShootLoop());
    }

    private void OnDisable()
    {
        if (shootRoutine != null)
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
        }
    }

    private IEnumerator ShootLoop()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        while (true)
        {
            if (projectilePrefab != null)
            {
                ShootLeft();
            }

            yield return new WaitForSeconds(fireInterval);
        }
    }

    private void ShootLeft()
    {
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            projectile.Launch(Vector2.left, projectileSpeed);
        }
    }
}
