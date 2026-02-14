using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [Header("Internal Resources")]
    [SerializeField] private GameObject gameOverPopup;
    [SerializeField] private GameObject pausePopup;

    private void Awake()
    {
        if (coreManagersChannel != null)
            coreManagersChannel.SetUIManager(this);
    }

    private void Start()
    {
        HideGameOverScreen();
    }

    public void ShowPausePopup()
    {
        if (pausePopup != null)
            pausePopup.SetActive(true);
    }

    public void HidePausePopup()
    {
        if (pausePopup != null)
            pausePopup.SetActive(false);
    }

    public void ShowGameOverPopup()
    {
        if (gameOverPopup != null)
            gameOverPopup.SetActive(true);
    }

    private void HideGameOverScreen()
    {
        if (gameOverPopup != null)
            gameOverPopup.SetActive(false);
    }
}
