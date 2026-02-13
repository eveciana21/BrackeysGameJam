using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    private void Awake()
    {
        if (coreManagersChannel != null)
        {
            coreManagersChannel.SetSpawnManager(this);
        }
    }
}
