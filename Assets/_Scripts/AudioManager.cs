using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    private void Awake()
    {
        if (coreManagersChannel != null)
        {
            coreManagersChannel.SetAudioManager(this);
        }
    }
}
