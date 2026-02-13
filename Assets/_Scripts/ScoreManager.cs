using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    private void Awake()
    {
        if (coreManagersChannel != null)
        {
            coreManagersChannel.SetScoreManager(this);
        }
    }
}
