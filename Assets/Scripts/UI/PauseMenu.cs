using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum PreviousMenu
{
    None,
    MainMenu,
    PauseMenu
}


public class PauseMenu : MonoBehaviour
{
    public static PreviousMenu previousMenu = PreviousMenu.None;


    [SerializeField] GameObject pauseMenuCanvas;
    [SerializeField] GameObject mainMenuCanvas;
    [SerializeField] GameObject optionsMenuCanvas;
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
            if (optionsMenuCanvas.activeSelf)
            {
                // Cerrar opciones y volver al menú anterior
                optionsMenuCanvas.SetActive(false);

                if (previousMenu == PreviousMenu.MainMenu)
                {
                    mainMenuCanvas.SetActive(true);
                }
                else if (previousMenu == PreviousMenu.PauseMenu)
                {
                    pauseMenuCanvas.SetActive(true);
                }

                previousMenu = PreviousMenu.None;
            }
            else if (!otherMenuOpen)
            {
                // Alternar pausa solo si no hay otro menú abierto
                TogglePauseMenu();
            }
        }
    }



    private void TogglePauseMenu()
    {
        isPaused = !isPaused;
        pauseMenuCanvas.SetActive(isPaused);

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
        previousMenu = PreviousMenu.PauseMenu;
        pauseMenuCanvas.SetActive(false);
        optionsMenuCanvas.SetActive(true);
    }


    private void ReturnToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1;
        mainMenuCanvas.SetActive(true);
        pauseMenuCanvas.SetActive(false);
    }
}
