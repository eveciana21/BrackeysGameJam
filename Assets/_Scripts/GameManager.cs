using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [SerializeField] private GameObject controls;

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

    private void Start()
    {
        // Hide cursor during gameplay by default; main menu scenes show it
        Scene current = SceneManager.GetActiveScene();
        if (current.buildIndex == 0)
            coreManagersChannel?.uiManager?.SetCursorUI();
        else
            coreManagersChannel?.uiManager?.SetCursorGameplay();
    }

    private void OnDisable()
    {
        uiInput.UI.Disable();
        uiInput.UI.Cancel.performed -= EscapeButtonPressed;
    }

    public void OnClick_StartGame()
    {
        Time.timeScale = 1f;
        coreManagersChannel?.uiManager?.StartIntroSequence();
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
        coreManagersChannel?.uiManager?.SetCursorUI();
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
        onQuitGameScreen = false;
        coreManagersChannel?.uiManager?.StartResumeCountdown();
        // Cursor hides after countdown completes (inside CountdownRoutine)
    }

    public void OnClick_OpenControls()
    {
        controls.SetActive(true);
    }

    public void OnClick_CloseControls()
    {
        controls.SetActive(false);
    }

    private void EscapeButtonPressed(InputAction.CallbackContext context)
    {
        Scene current = SceneManager.GetActiveScene();

        // Don't pause on Main Menu
        if (current.buildIndex == 0)
            return;

        var ui = coreManagersChannel?.uiManager;

        if (ui != null && ui.IsInCredits)
            return;

        // If a countdown is running, cancel it and re-pause
        if (ui != null && ui.IsCountingDown)
        {
            ui.CancelCountdown();
            ui.SetCursorUI();
            onQuitGameScreen = true;
            Time.timeScale = 0f;
            return;
        }

        if (onQuitGameScreen)
        {
            // Pause menu is open � start the countdown resume instead
            OnClick_Continue();
            return;
        }

        ui?.ShowPausePopup();
        ui?.SetCursorUI();
        onQuitGameScreen = true;
        Time.timeScale = 0f;
    }

    public void ContinueButton()
    {
        Time.timeScale = 1f;
        coreManagersChannel?.uiManager?.FadeOutThenStartGame();
    }

    public void StartGameAfterFade()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneIndex); // scene 1
    }

    public void OnClick_QuitGame()
    {
        Application.Quit();
    }
}
