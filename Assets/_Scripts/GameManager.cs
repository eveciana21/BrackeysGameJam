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
            coreManagersChannel?.SetGameManager(this);
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
        Time.timeScale = 1;
        SceneManager.LoadScene(mainSceneIndex);
    }

    public void OnClick_RestartGame()
    {
        Time.timeScale = 1;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void OnClick_MainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(mainMenuIndex);
    }

    public void OnClick_Continue()
    {
        coreManagersChannel?.uiManager?.HidePausePopup();

        onQuitGameScreen = false;

        Time.timeScale = 1;
    }

    private void EscapeButtonPressed(InputAction.CallbackContext context)
    {
        Scene current = SceneManager.GetActiveScene();

        // Don't pause on Main Menu
        if (current.buildIndex == 0)
        {
            return;
        }

        if (onQuitGameScreen)
        {
            OnClick_Continue();
            return;
        }

        coreManagersChannel?.uiManager?.ShowPausePopup();
        onQuitGameScreen = true;
        Time.timeScale = 0;
    }

    public void OnClick_QuitGame()
    {
        Application.Quit();
    }
}
