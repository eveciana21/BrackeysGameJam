using UnityEngine;

[CreateAssetMenu(fileName = "CoreManagersChannelSO", menuName = "Scriptable Objects/CoreManagersChannelSO")]
public class CoreManagersChannelSO : ScriptableObject
{
    [HideInInspector] public ScoreManager scoreManager;
    [HideInInspector] public PoolManager poolManager;
    [HideInInspector] public UIManager uiManager;
    [HideInInspector] public GameManager gameManager;
    [HideInInspector] public SpawnManager spawnManager;
    [HideInInspector] public AudioManager audioManager;

    public void SetUIManager(UIManager instance)
    {
        uiManager = instance;
    }

    public void SetScoreManager(ScoreManager instance)
    {
        scoreManager = instance;
    }

    public void SetPoolManager(PoolManager instance)
    {
        poolManager = instance;
    }

    public void SetGameManager(GameManager instance)
    {
        gameManager = instance;
    }

    public void SetSpawnManager(SpawnManager instance)
    {
        spawnManager = instance;
    }

    public void SetAudioManager(AudioManager instance)
    {
        audioManager = instance;
    }
}
