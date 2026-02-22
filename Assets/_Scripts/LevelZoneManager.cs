using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

public class LevelZoneManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [Header("Level Config")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("Camera")]
    [SerializeField] private CinemachineBrain brain;

    // Locked (per-level) camera (fixed view for this drawing)
    [SerializeField] private CinemachineCamera levelCamera;

    // Travel/Peek (global) camera (follows between drawings)
    [SerializeField] private CinemachineCamera travelCamera;
    [SerializeField] private CinemachineConfiner2D travelConfiner;

    [SerializeField] private float enterBlendTime = 0.45f;
    [SerializeField] private float travelBlendTime = 0.35f;

    [Header("Cutscene")]
    [SerializeField] private CutsceneTriggerRatHole ratHoleTrigger;

    [Header("Conveyor")]
    [SerializeField] private ConveyorObjectSpawner conveyorSpawner;

    [Header("Camera Bounds")]
    [SerializeField] private Collider2D lockedCameraBounds;
    [SerializeField] private Collider2D peekCameraBounds;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointsRoot;

    [Header("Level Barriers")]
    [SerializeField] private GameObject entryBarrier;
    [SerializeField] private GameObject exitBarrier;

    [Header("Start Level")]
    [SerializeField] private bool startLevel; // ONLY true for level 1

    [Header("Spawning")]
    [SerializeField] private float firstSpawnDelay = 0.25f; // 0 = immediate after trigger

    [Header("Last Level Boss")]
    [SerializeField] private bool isLastLevel;
    [SerializeField] private GameObject dragon;
    [SerializeField] private GameObject[] stage1Platforms;
    [SerializeField] private GameObject[] stage2Platforms;

    [SerializeField] private AudioClip levelCompleteSfx;

    private Transform[] cachedSpawnPoints;

    [SerializeField] private float arrowShowDelay = 1f;
    private Coroutine arrowRoutine;
    [SerializeField] private GameObject arrow;

    private int enemiesKilled;
    private bool isActive;
    private bool isComplete;
    private bool hasStartedCombat;
    private bool bossPhase;
    private bool bossKilled;

    private bool hasBossSpawned = false;

    private void Awake()
    {
        CacheSpawnPoints();

        if (brain == null && Camera.main != null)
        {
            brain = Camera.main.GetComponent<CinemachineBrain>();
        }

        if (arrow != null && arrow.activeSelf)
        {
            arrow.SetActive(false);
        }
        // Default barrier states
        if (entryBarrier != null) entryBarrier.SetActive(false);
        if (exitBarrier != null) exitBarrier.SetActive(true);

        // Camera defaults
        if (travelCamera != null) travelCamera.Priority = 10; // baseline
        if (levelCamera != null) levelCamera.Priority = 0;    // off until active

        // Default: keep travel confined to locked bounds
        SetTravelBounds(lockedCameraBounds);
    }

    private void Start()
    {
        if (startLevel)
        {
            ApplyStartViewOnly();
            ApplyProjectileLoadoutOnly();

            // Auto-play music for the first level without needing the entry trigger
            if (coreManagersChannel != null && coreManagersChannel.audioManager != null && levelConfig != null)
            {
                coreManagersChannel.audioManager.PlayMusic(levelConfig.levelMusic);
            }
        }
    }

    private void ApplyStartViewOnly()
    {
        SetTravelBounds(lockedCameraBounds);

        if (travelCamera != null) travelCamera.Priority = 10;
        if (levelCamera != null) levelCamera.Priority = 20;

        // Snap output camera so there is no "move from somewhere else" on first frame
        if (Camera.main != null && levelCamera != null)
        {
            Transform mainCam = Camera.main.transform;
            mainCam.position = new Vector3(levelCamera.transform.position.x, levelCamera.transform.position.y, mainCam.position.z);
            mainCam.rotation = levelCamera.transform.rotation;
        }
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
        if (isComplete) return;
        if (!other.CompareTag("Player")) return;

        // First time entering this trigger = lock level + start combat
        if (!isActive)
        {
            ActivateLevel();
        }
    }

    private void ActivateLevel()
    {
        isActive = true;

        // Lower previous level camera if needed
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

        if (conveyorSpawner != null)
            conveyorSpawner.StartSpawning();

        // Lock gates on entry
        if (entryBarrier != null) entryBarrier.SetActive(true);
        if (exitBarrier != null) exitBarrier.SetActive(true);

        // Keep travel camera confined to locked bounds while level is active
        SetTravelBounds(lockedCameraBounds);

        // Blend into locked camera
        if (brain != null)
        {
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, enterBlendTime);
        }

        // Priority switch (locked camera wins)
        if (travelCamera != null) travelCamera.Priority = 10;
        if (levelCamera != null) levelCamera.Priority = 20;

        // Music
        if (coreManagersChannel != null && coreManagersChannel.audioManager != null && levelConfig != null)
        {
            coreManagersChannel.audioManager.PlayMusic(levelConfig.levelMusic);
        }

        // Projectile prefabs (per level)
        if (levelConfig != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Player player = playerObj.GetComponent<Player>();
                if (player != null)
                {
                    player.SetProjectilePrefabs(levelConfig.projectilePrefabs);
                }
            }
        }

        // Start combat/spawning (only once)
        if (!hasStartedCombat)
        {
            hasStartedCombat = true;

            if (firstSpawnDelay > 0f)
            {
                StartCoroutine(StartSpawningAfterDelay());
            }
            else
            {
                StartSpawningNow();
            }
        }
    }

    private void ApplyProjectileLoadoutOnly()
    {
        if (levelConfig == null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        Player player = playerObj.GetComponent<Player>();
        if (player == null) return;

        player.SetProjectilePrefabs(levelConfig.projectilePrefabs);
    }

    private IEnumerator StartSpawningAfterDelay()
    {
        yield return new WaitForSeconds(firstSpawnDelay);
        StartSpawningNow();
    }

    private void StartSpawningNow()
    {
        if (coreManagersChannel == null) return;
        if (coreManagersChannel.spawnManager == null) return;
        if (levelConfig == null) return;

        coreManagersChannel.spawnManager.StartSpawning(levelConfig.spawnRate, levelConfig.enemiesToKill, levelConfig.enemyPrefabs, cachedSpawnPoints, this);
    }

    public void RegisterEnemyDeath(bool isBoss = false)
    {
        if (!isActive || isComplete) return;
        if (!hasStartedCombat) return;

        // Boss phase: only the dragon dying completes the level
        if (bossPhase && isBoss)
        {
            if (!bossKilled)
            {
                EnemyBasic[] enemies = FindObjectsByType<EnemyBasic>(FindObjectsSortMode.None);

                foreach (EnemyBasic enemy in enemies)
                {
                    enemy.Die(false);
                }

                bossKilled = true;
                CompleteLevel(skipPeek: true);
            }
            return;
        }

        enemiesKilled++;

        if (levelConfig != null && enemiesKilled >= levelConfig.enemiesToKill)
        {
            if (isLastLevel && !hasBossSpawned)
            {
                hasBossSpawned = true;
                StartCoroutine(SpawnBoss());
            }
            else if (!hasBossSpawned)
            {
                CompleteLevel(skipPeek: false);
            }
        }
    }

    private IEnumerator SpawnBoss()
    {
        if (coreManagersChannel != null && coreManagersChannel.spawnManager != null)
        {
            coreManagersChannel.spawnManager.StopSpawning();
        }

        bossPhase = true;

        // Wait for platforms to disappear
        foreach (GameObject platform in stage1Platforms)
        {
            platform.GetComponent<Animator>().SetTrigger("disappear");
        }

        yield return new WaitForSeconds(4.0f);

        // Wait for platforms to appear
        foreach (GameObject platform in stage2Platforms)
        {
            platform.SetActive(true);
        }

        yield return new WaitForSeconds(3.0f);

        if (dragon != null)
        {
            dragon.SetActive(true);

            coreManagersChannel.spawnManager.StartSpawning(levelConfig.spawnRate * 7.0f, -1, levelConfig.enemyPrefabs, cachedSpawnPoints, this);

            EnemyBaseClass enemyBase = dragon.GetComponent<EnemyBaseClass>();
            if (enemyBase != null)
            {
                enemyBase.Initialize(this);
            }
        }

        yield return "";
    }

    private void CompleteLevel(bool skipPeek = false)
    {
        isComplete = true;

        if (coreManagersChannel != null && levelCompleteSfx != null)
            coreManagersChannel.audioManager.PlaySFX(levelCompleteSfx);

        TryShowArrowWithDelay();

        // Enable the rathole cutscene trigger
        if (ratHoleTrigger != null)
            ratHoleTrigger.EnableTrigger();

        if (coreManagersChannel != null && coreManagersChannel.spawnManager != null)
        {
            coreManagersChannel.spawnManager.StopSpawning();
        }

        if (conveyorSpawner != null)
            conveyorSpawner.StopSpawning();

        // Open exit
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

        // Switch cameras (travel wins)
        ForceDeactivateLockedCamera();
        if (travelCamera != null) travelCamera.Priority = 20;

        // After a short moment, allow the small peek range (skipped on last level)
        if (!skipPeek)
        {
            StartCoroutine(EnablePeekBoundsAfterDelay());
        }

        // Prevent re-triggering this level trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private void TryShowArrowWithDelay()
    {
        // If this level has no arrow assigned, do nothing
        if (arrow == null) return;

        if (arrowRoutine != null)
        {
            StopCoroutine(arrowRoutine);
        }

        arrowRoutine = StartCoroutine(ShowArrowAfterDelay());
    }

    private IEnumerator ShowArrowAfterDelay()
    {
        yield return new WaitForSeconds(arrowShowDelay);

        if (arrow != null)
        {
            arrow.SetActive(true);
        }

        arrowRoutine = null;
    }

    private IEnumerator EnablePeekBoundsAfterDelay()
    {
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
