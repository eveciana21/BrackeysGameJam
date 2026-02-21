using System.Collections;
using UnityEngine;

public class ConveyorObjectSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] prefabsToSpawn;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRate = 2f;
    [SerializeField] private int maxAliveAtOnce = 0;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Spawn Area")]
    [SerializeField] private float spawnWidth = 5f;
    [SerializeField] private float spawnOffsetY = 0f;

    [Header("Cleanup")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float destroyDelayOffGround = 3f;

    [Header("Layers")]
    [SerializeField] private LayerMask wallLayer;

    private Coroutine spawnRoutine;
    private int aliveCount = 0;

    private void Start()
    {
        if (spawnOnStart)
            StartSpawning();
    }

    public void StartSpawning()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnRate);

            if (maxAliveAtOnce > 0 && aliveCount >= maxAliveAtOnce)
                continue;

            SpawnObject();
        }
    }

    private void SpawnObject()
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0) return;

        int index = Random.Range(0, prefabsToSpawn.Length);
        if (prefabsToSpawn[index] == null) return;

        float randomX = transform.position.x + Random.Range(-spawnWidth * 0.5f, spawnWidth * 0.5f);
        Vector3 spawnPos = new Vector3(randomX, transform.position.y + spawnOffsetY, 0f);

        GameObject obj = Instantiate(prefabsToSpawn[index], spawnPos, Quaternion.identity);

        aliveCount++;
        ConveyorObjectTracker tracker = obj.AddComponent<ConveyorObjectTracker>();
        tracker.Initialize(this, groundLayer, GetLayerFromMask(wallLayer));
    }

    public void OnTrackedObjectDestroyed()
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }

    // LayerMask to layer index conversion
    private int GetLayerFromMask(LayerMask mask)
    {
        int value = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((value & (1 << i)) != 0)
                return i;
        }
        return 0;
    }
}
