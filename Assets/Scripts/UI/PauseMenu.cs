using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu_Canvas;
    [SerializeField] GameObject mainMenuCanvas;
    [SerializeField] CanvasGroup hudCanvas;
    private UserInput userInput;
    private bool isPaused = false;
    public static bool otherMenuOpen = true;

    [SerializeField] Button exitButton;
    [SerializeField] Button resumeButton;

    private void OnEnable()
    {
        exitButton.onClick.AddListener(ReturnToMainMenu);
        resumeButton.onClick.AddListener(TogglePauseMenu);
        hudCanvas.alpha = 0f;
    }

    private void OnDisable()
    {
        exitButton.onClick.RemoveListener(ReturnToMainMenu);
        resumeButton.onClick.RemoveListener(TogglePauseMenu);
        hudCanvas.alpha = 1f;
    }

    void Start()
    {
        userInput = UserInput.instance;
    }

    void Update()
    {
        if (userInput.pauseMenuPressed && !otherMenuOpen)
        {
            TogglePauseMenu();
        }
    }

    private void TogglePauseMenu()
    {
        isPaused = !isPaused;
        pauseMenu_Canvas.SetActive(isPaused);

        if (isPaused)
        {
            userInput.switchActionMap(UserInput.ActionMap.InMenu);
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            userInput.switchActionMap(UserInput.ActionMap.InGame);
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ReturnToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1;
        mainMenuCanvas.SetActive(true);
        pauseMenu_Canvas.SetActive(false);
    }
}
