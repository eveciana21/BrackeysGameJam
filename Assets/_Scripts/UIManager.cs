using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [Header("Internal Resources")]
    [SerializeField] private GameObject gameOverPopup;
    [SerializeField] private GameObject pausePopup;                // PauseMenu root
    [SerializeField] private GameObject pauseButtonsContainer;     // Buttons (Resume/Restart/MainMenu)
    [SerializeField] private GameObject areYouSurePopup;           // Buttons_AreYouSure (Yes/No)

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI pauseText;

    // 0 = Main Menu, 1 = Restart, -1 = None
    private int confirmType = -1;

    private void Awake()
    {
        if (coreManagersChannel != null)
            coreManagersChannel.SetUIManager(this);
    }

    private void Start()
    {
        if (gameOverPopup != null) gameOverPopup.SetActive(false);
        HidePausePopup();
    }

    public void ShowPausePopup()
    {
        confirmType = -1;

        if (pausePopup != null) pausePopup.SetActive(true);
        if (pauseButtonsContainer != null) pauseButtonsContainer.SetActive(true);
        if (areYouSurePopup != null) areYouSurePopup.SetActive(false);

        if (pauseText != null) pauseText.text = "Paused... better be gud";
    }

    public void HidePausePopup()
    {
        confirmType = -1;

        if (pausePopup != null) pausePopup.SetActive(false);

        // reset children for next time
        if (pauseButtonsContainer != null) pauseButtonsContainer.SetActive(true);
        if (areYouSurePopup != null) areYouSurePopup.SetActive(false);

        if (pauseText != null) pauseText.text = "Paused... better be gud";
    }

    public void ShowGameOverPopup()
    {
        if (gameOverPopup != null) gameOverPopup.SetActive(true);
    }

    public void HideGameOverPopup()
    {
        if (gameOverPopup != null) gameOverPopup.SetActive(false);
    }

    public void ShowConfirmMainMenu()
    {
        confirmType = 0;

        if (pauseButtonsContainer != null) pauseButtonsContainer.SetActive(false);
        if (areYouSurePopup != null) areYouSurePopup.SetActive(true);

        if (pauseText != null) pauseText.text = "You sure?";
    }

    public void ShowConfirmRestart()
    {
        confirmType = 1;

        if (pauseButtonsContainer != null) pauseButtonsContainer.SetActive(false);
        if (areYouSurePopup != null) areYouSurePopup.SetActive(true);

        if (pauseText != null) pauseText.text = "You sure?";
    }

    public void ConfirmYes()
    {
        if (confirmType == 0)
            coreManagersChannel?.gameManager?.GoToMainMenu();
        else if (confirmType == 1)
            coreManagersChannel?.gameManager?.RestartCurrentScene();

        confirmType = -1;
    }

    public void ConfirmNo()
    {
        confirmType = -1;

        if (areYouSurePopup != null) areYouSurePopup.SetActive(false);
        if (pauseButtonsContainer != null) pauseButtonsContainer.SetActive(true);

        if (pauseText != null) pauseText.text = "Paused... better be gud";
    }
}
