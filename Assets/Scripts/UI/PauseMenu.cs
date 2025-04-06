using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu_Canvas;
    [SerializeField] GameObject mainMenuCanvas;
    [SerializeField] GameObject optionsMenu_Canvas;
    [SerializeField] CanvasGroup hudCanvas;
    private UserInput userInput;
    private bool isPaused = false;
    public static bool otherMenuOpen = true;

    [SerializeField] Button exitButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button resumeButton;

    private void OnEnable()
    {
        exitButton.onClick.AddListener(ReturnToMainMenu);
        optionsButton.onClick.AddListener(OpenOptionsMenu);
        resumeButton.onClick.AddListener(TogglePauseMenu);
        hudCanvas.alpha = 0f;
    }

    private void OnDisable()
    {
        exitButton.onClick.RemoveListener(ReturnToMainMenu);
        optionsButton.onClick.RemoveListener(OpenOptionsMenu);
        resumeButton.onClick.RemoveListener(TogglePauseMenu);
        hudCanvas.alpha = 1f;
    }

    void Start()
    {
        userInput = UserInput.instance;
    }

    void Update()
    {
        if (userInput.pauseMenuPressed)
        {
            if (optionsMenu_Canvas.activeSelf)
            {
                pauseMenu_Canvas.SetActive(true);
                optionsMenu_Canvas.SetActive(false);
            }
            else if (!otherMenuOpen)
            {
                TogglePauseMenu();
            }
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

    private void OpenOptionsMenu()
    {
        pauseMenu_Canvas.SetActive(false);
        optionsMenu_Canvas.SetActive(true);

    }

    private void ReturnToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1;
        mainMenuCanvas.SetActive(true);
        pauseMenu_Canvas.SetActive(false);
    }
}
