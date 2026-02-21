using System.Collections;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    private Transform[] spawnPoints;

    private float spawnRate;
    private int maxToSpawn;
    private int spawnedCount;
    private bool spawning;

    private GameObject[] currentEnemyPrefabs;
    private LevelZoneManager levelZone;

    private void Awake()
    {
        if (coreManagersChannel != null)
        {
            coreManagersChannel.SetSpawnManager(this);
        }
    }

    public void StartSpawning(float rate, int totalToSpawn, GameObject[] levelEnemies, Transform[] levelSpawnPoints, LevelZoneManager zone)
    {
        spawnRate = rate;
        maxToSpawn = totalToSpawn;
        currentEnemyPrefabs = levelEnemies;
        spawnPoints = levelSpawnPoints;
        levelZone = zone;

        spawnedCount = 0;
        spawning = true;

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (spawning && (spawnedCount < maxToSpawn || maxToSpawn < 0))
        {
            SpawnEnemy();
            spawnedCount++;

            yield return new WaitForSeconds(spawnRate);
        }
    }

    private void SpawnEnemy()
    {
        if (currentEnemyPrefabs == null || currentEnemyPrefabs.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        int spawnPointIndex = Random.Range(0, spawnPoints.Length);
        int enemyIndex = Random.Range(0, currentEnemyPrefabs.Length);

        GameObject enemy = Instantiate(currentEnemyPrefabs[enemyIndex], spawnPoints[spawnPointIndex].position, Quaternion.identity);

        EnemyBaseClass enemyBase = enemy.GetComponent<EnemyBaseClass>();
        if (enemyBase != null)
        {
            enemyBase.Initialize(levelZone);
        }
    }

    public void StopSpawning()
    {
        spawning = false;
        StopAllCoroutines();
    }
}
