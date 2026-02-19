using Unity.Cinemachine;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [SerializeField] private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        Instance = this;

        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }
    }

    public void Shake(float amplitude = 1f)
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(amplitude);
    }
}
