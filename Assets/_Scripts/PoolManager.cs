using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    private void Awake()
    {
        if (coreManagersChannel != null)
        {
            coreManagersChannel.SetPoolManager(this);
        }
    }
}
