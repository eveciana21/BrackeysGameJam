using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    private InputActions uiInput;
    private bool onQuitGameScreen;

    private int mainMenuIndex = 0;
    private int mainSceneIndex = 1;

    private void Awake()
    {
        if (coreManagersChannel != null)
        {
            coreManagersChannel.SetGameManager(this);
        }

        uiInput = new InputActions();
    }

    private void OnEnable()
    {
        uiInput.UI.Enable();
        uiInput.UI.Cancel.performed += EscapeButtonPressed;

        coreManagersChannel?.uiManager?.HidePausePopup();
    }

    private void OnDisable()
    {
        uiInput.UI.Disable();
        uiInput.UI.Cancel.performed -= EscapeButtonPressed;
    }

    public void OnClick_StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneIndex);
    }

    // called by UIManager ConfirmYes()
    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // called by UIManager ConfirmYes()
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuIndex);
    }

    // Kept for button wiring if you still use them
    public void OnClick_RestartGame()
    {
        RestartCurrentScene();
    }

    public void OnClick_MainMenu()
    {
        GoToMainMenu();
    }

    public void OnClick_Continue()
    {
        coreManagersChannel?.uiManager?.HidePausePopup();
        onQuitGameScreen = false;
        Time.timeScale = 1f;
    }

    private void EscapeButtonPressed(InputAction.CallbackContext context)
    {
        Scene current = SceneManager.GetActiveScene();

        // Don't pause on Main Menu
        if (current.buildIndex == 0)
            return;

        if (onQuitGameScreen)
        {
            OnClick_Continue();
            return;
        }

        coreManagersChannel?.uiManager?.ShowPausePopup();
        onQuitGameScreen = true;
        Time.timeScale = 0f;
    }

    public void OnClick_QuitGame()
    {
        Application.Quit();
    }
}
