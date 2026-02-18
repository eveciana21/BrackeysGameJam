using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class LevelZoneManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [Header("Level Config")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("Camera")]
    [SerializeField] private CinemachineBrain brain;

    // Locked (per-level) camera
    [SerializeField] private CinemachineCamera levelCamera;

    // Travel/Peek (global) camera
    [SerializeField] private CinemachineCamera travelCamera;
    [SerializeField] private CinemachineConfiner2D travelConfiner;

    [SerializeField] private float enterBlendTime = 0.45f;
    [SerializeField] private float travelBlendTime = 0.35f;

    [Header("Camera Bounds")]
    [SerializeField] private Collider2D lockedCameraBounds;
    [SerializeField] private Collider2D peekCameraBounds;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointsRoot;

    [Header("Level Barriers")]
    [SerializeField] private GameObject entryBarrier;
    [SerializeField] private GameObject exitBarrier;

    private Transform[] cachedSpawnPoints;

    private int enemiesKilled;
    private bool isActive;
    private bool isComplete;

    private void Awake()
    {
        CacheSpawnPoints();

        if (brain == null && Camera.main != null)
        {
            brain = Camera.main.GetComponent<CinemachineBrain>();
        }

        // Default barrier states
        if (entryBarrier != null) entryBarrier.SetActive(false);
        if (exitBarrier != null) exitBarrier.SetActive(true);

        // Camera defaults
        if (travelCamera != null) travelCamera.Priority = 10;  // baseline
        if (levelCamera != null) levelCamera.Priority = 0;     // off until entered

        // Keep travel camera confined to locked bounds until the level is completed
        SetTravelBounds(lockedCameraBounds);
    }

    private void CacheSpawnPoints()
    {
        if (spawnPointsRoot == null)
        {
            cachedSpawnPoints = new Transform[0];
            return;
        }

        int childCount = spawnPointsRoot.childCount;
        cachedSpawnPoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            cachedSpawnPoints[i] = spawnPointsRoot.GetChild(i);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive || isComplete) return;
        if (!other.CompareTag("Player")) return;

        ActivateLevel();
    }

    private void ActivateLevel()
    {
        isActive = true;

        // Turn off previous level's locked camera
        if (coreManagersChannel != null && coreManagersChannel.levelZoneManager != null)
        {
            LevelZoneManager previous = coreManagersChannel.levelZoneManager;
            if (previous != null && previous != this)
            {
                previous.ForceDeactivateLockedCamera();
            }
        }

        if (coreManagersChannel != null)
        {
            coreManagersChannel.SetLevelZoneManager(this);
        }

        // Lock gates when entering
        if (entryBarrier != null) entryBarrier.SetActive(true);
        if (exitBarrier != null) exitBarrier.SetActive(true);

        // While level is active: travel camera should NOT be able to drift
        SetTravelBounds(lockedCameraBounds);

        // Blend into locked camera
        if (brain != null)
        {
            brain.DefaultBlend = new CinemachineBlendDefinition( CinemachineBlendDefinition.Styles.EaseInOut, enterBlendTime);
        }

        // Ensure locked camera wins
        if (travelCamera != null) travelCamera.Priority = 10;
        if (levelCamera != null) levelCamera.Priority = 20;

        // Music
        if (coreManagersChannel != null && coreManagersChannel.audioManager != null && levelConfig != null)
        {
            coreManagersChannel.audioManager.PlayMusic(levelConfig.levelMusic);
        }

        // Projectile sprite
        if (levelConfig != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Player player = playerObj.GetComponent<Player>();
                if (player != null)
                {
                    player.SetProjectileSprite(levelConfig.projectileSprite);
                }
            }
        }

        // Spawning
        if (coreManagersChannel != null && coreManagersChannel.spawnManager != null && levelConfig != null)
        {
            coreManagersChannel.spawnManager.StartSpawning(levelConfig.spawnRate,levelConfig.enemiesToKill, levelConfig.enemyPrefabs,cachedSpawnPoints, this );
        }
    }

    public void RegisterEnemyDeath()
    {
        if (!isActive || isComplete) return;

        enemiesKilled++;

        if (levelConfig != null && enemiesKilled >= levelConfig.enemiesToKill)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        isComplete = true;

        if (coreManagersChannel != null && coreManagersChannel.spawnManager != null)
        {
            coreManagersChannel.spawnManager.StopSpawning();
        }

        if (exitBarrier != null)
        {
            exitBarrier.SetActive(false);
        }

        // Smooth blend to travel camera
        if (brain != null)
        {
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, travelBlendTime);
        }

        // Keep travel bounds LOCKED at first so it doesn't immediately reveal more area
        SetTravelBounds(lockedCameraBounds);

        // Snap travel camera transform to current live camera position to avoid jump
        if (travelCamera != null && Camera.main != null)
        {
            travelCamera.transform.position = Camera.main.transform.position;
            travelCamera.transform.rotation = Camera.main.transform.rotation;
        }

        // Switch cameras
        ForceDeactivateLockedCamera();
        if (travelCamera != null) travelCamera.Priority = 20;

        // After a short moment, allow the small peek range
        StartCoroutine(EnablePeekBoundsAfterDelay());

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private IEnumerator EnablePeekBoundsAfterDelay()
    {
        // Wait until the blend has mostly started (prevents "pop" reveal)
        yield return new WaitForSeconds(travelBlendTime * 0.5f);

        SetTravelBounds(peekCameraBounds);
    }

    public void ForceDeactivateLockedCamera()
    {
        if (levelCamera != null)
        {
            levelCamera.Priority = 0;
        }
    }

    private void SetTravelBounds(Collider2D bounds)
    {
        if (travelConfiner == null) return;
        if (bounds == null) return;

        travelConfiner.BoundingShape2D = bounds;
        travelConfiner.InvalidateBoundingShapeCache();
    }
}
