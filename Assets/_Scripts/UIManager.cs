using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Countdown")]
    // Assign the three UI Image GameObjects in the Inspector (one per number)
    [SerializeField] private GameObject countdown3;   // Image showing sprite "3"
    [SerializeField] private GameObject countdown2;   // Image showing sprite "2"
    [SerializeField] private GameObject countdown1;   // Image showing sprite "1"

    [SerializeField] private GameObject fadeInBlack;
    [SerializeField] private GameObject fadeOutBlack;
    [SerializeField] private GameObject teacherImage;
    [SerializeField] private GameObject continueButton;

    [Header("Audio")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private float menuMusicFadeOutTime = 2f;

    // 0 = Main Menu, 1 = Restart, -1 = None
    private int confirmType = -1;

    private Coroutine countdownCoroutine;

    private void Awake()
    {
        if (coreManagersChannel != null)
            coreManagersChannel.SetUIManager(this);
    }

    private void Start()
    {
        if (gameOverPopup != null) gameOverPopup.SetActive(false);
        HideAllCountdownSprites();
        HidePausePopup();

        if (teacherImage != null) teacherImage.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);

        if (fadeInBlack != null) fadeInBlack.SetActive(false);
        if (fadeOutBlack != null) fadeOutBlack.SetActive(false);
    }

    private void HideAllCountdownSprites()
    {
        if (countdown3 != null) countdown3.SetActive(false);
        if (countdown2 != null) countdown2.SetActive(false);
        if (countdown1 != null) countdown1.SetActive(false);
    }

    // Called by Resume button
    public void StartResumeCountdown()
    {
        if (pausePopup != null) pausePopup.SetActive(false);

        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    // Cancels an in-progress countdown and re-opens pause menu (game stays paused)
    public void CancelCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        coreManagersChannel.isInputLocked = false;
        HideAllCountdownSprites();
        ShowPausePopup();
    }

    public bool IsCountingDown => countdownCoroutine != null;

    private IEnumerator CountdownRoutine()
    {
        coreManagersChannel.isInputLocked = true;
        SetCursorGameplay();

        // --- Show "3" ---
        HideAllCountdownSprites();
        if (countdown3 != null) countdown3.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);

        // --- Show "2" ---
        HideAllCountdownSprites();
        if (countdown2 != null) countdown2.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);

        // --- Show "1" ---
        HideAllCountdownSprites();
        if (countdown1 != null) countdown1.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);

        // --- Done ---
        HideAllCountdownSprites();
        countdownCoroutine = null;

        coreManagersChannel.isInputLocked = false;
        Time.timeScale = 1f;
        SetCursorGameplay();
    }

    private void ClearSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // Cursor helpers (called by GameManager)
    public void SetCursorUI()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void SetCursorGameplay()
    {
#if UNITY_EDITOR
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
#else
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
#endif
    }

    public void ShowPausePopup()
    {
        confirmType = -1;
        coreManagersChannel.isInputLocked = true;
        ClearSelection();

        if (pausePopup != null) pausePopup.SetActive(true);
        if (pauseButtonsContainer != null) pauseButtonsContainer.SetActive(true);
        if (areYouSurePopup != null) areYouSurePopup.SetActive(false);

        if (pauseText != null) pauseText.text = "Paused... better be gud";
    }

    public void HidePausePopup()
    {
        confirmType = -1;
        coreManagersChannel.isInputLocked = false;

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
        ClearSelection();

        if (pauseButtonsContainer != null) pauseButtonsContainer.SetActive(false);
        if (areYouSurePopup != null) areYouSurePopup.SetActive(true);

        if (pauseText != null) pauseText.text = "You sure?";
    }

    public void ShowConfirmRestart()
    {
        confirmType = 1;
        ClearSelection();

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
        ClearSelection();

        if (areYouSurePopup != null) areYouSurePopup.SetActive(false);
        if (pauseButtonsContainer != null) pauseButtonsContainer.SetActive(true);

        if (pauseText != null) pauseText.text = "Paused... better be gud";
    }

    public void StartIntroSequence()
    {
        if (teacherImage != null) teacherImage.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);

        if (fadeInBlack != null) fadeInBlack.SetActive(false);
        if (fadeOutBlack != null) fadeOutBlack.SetActive(false);

        StartCoroutine(StartIntroCoroutine());
    }

    private IEnumerator StartIntroCoroutine()
    {
        // Fade OUT (to black)
        if (fadeOutBlack != null) fadeOutBlack.SetActive(true);
        yield return new WaitForSecondsRealtime(2);

        // Now that screen is black, show the teacher UI
        if (teacherImage != null) teacherImage.SetActive(true);
        if (continueButton != null) continueButton.SetActive(true);

        // Fade IN (back to visible) so player can see teacher/continue
        if (fadeInBlack != null) fadeInBlack.SetActive(true);
        if (fadeOutBlack != null) fadeOutBlack.SetActive(false);

        // Optional: disable fade-in object after it finishes so it doesn't block clicks
        yield return new WaitForSecondsRealtime(2);
        if (fadeInBlack != null) fadeInBlack.SetActive(false);
    }

    public void FadeOutThenStartGame()
    {
        StartCoroutine(FadeOutThenStartGameCoroutine());
    }

    private IEnumerator FadeOutThenStartGameCoroutine()
    {
        // Fade OUT (to black)
        if (fadeInBlack != null) fadeInBlack.SetActive(false);
        if (fadeOutBlack != null) fadeOutBlack.SetActive(true);

        // Fade audio while we fade to black
        float fadeTime = menuMusicFadeOutTime;
        float t = 0f;

        float startVolume = 0f;
        if (menuMusicSource != null)
        {
            startVolume = menuMusicSource.volume;
        }

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;

            if (menuMusicSource != null)
            {
                float normalized = Mathf.Clamp01(t / fadeTime);
                menuMusicSource.volume = Mathf.Lerp(startVolume, 0f, normalized);
            }

            yield return null;
        }

        if (menuMusicSource != null)
        {
            menuMusicSource.volume = 0f;
            // Optional: stop it entirely (uncomment if you want)
            // menuMusicSource.Stop();
        }

        // Start the game after the fade is done
        coreManagersChannel?.gameManager?.StartGameAfterFade();
    }

    public void StartGameAfterFade()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(1); // scene 1
    }
}
