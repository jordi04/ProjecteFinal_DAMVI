using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using Cinemachine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Transform playerInitialTransform;
    [SerializeField] Button playButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button exitButton;

    [SerializeField] CanvasGroup mainMenuCanvas;
    [SerializeField] CanvasGroup hudCanvas;
    [SerializeField] GameObject optionsMenuCanvas;

    [SerializeField] PlayableDirector playableDirector;
    [SerializeField] float fadeDuration = 1f;
    [SerializeField] CinemachineVirtualCamera mainMenuCamera;
    [SerializeField] private bool firstTime = true;
    private Transform playerTransform;

    private void Awake()
    {
        playerTransform = ManaSystem.instance.gameObject.transform;
        playerTransform.transform.position = playerInitialTransform.position;
        playerTransform.transform.rotation = playerInitialTransform.rotation;
    }

    private void Start()
    {
        playerTransform = ManaSystem.instance.gameObject.transform;
        playerTransform.transform.position = playerInitialTransform.position;
        playerTransform.transform.rotation = playerInitialTransform.rotation;
        UserInput.instance.switchActionMap(UserInput.ActionMap.InMenu);
    }

    private void OnEnable()
    {
        hudCanvas.alpha = 0;
        mainMenuCamera.Priority = 21;
        mainMenuCanvas.alpha = 1;
        PauseMenu.otherMenuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        mainMenuCanvas.interactable = true;
        mainMenuCanvas.blocksRaycasts = true;
        playButton.onClick.AddListener(Play);
        optionsButton.onClick.AddListener(OpenOptionsMenu);
        exitButton.onClick.AddListener(ExitGame);
        Time.timeScale = 0;
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveListener(Play);
        optionsButton.onClick.RemoveListener(OpenOptionsMenu);
        exitButton.onClick.RemoveListener(ExitGame);

    }

    private void Play()
    {
        StartCoroutine(FadeOutAndPlay());
        if (!firstTime)
            EndTimeline();
        firstTime = false;
    }

    private void ExitGame()
    {
        StartCoroutine(FadeOutAndExit());
    }

    private IEnumerator FadeOutAndPlay()
    {
        // Bloquea los botones durante la transicion
        mainMenuCanvas.interactable = false;
        mainMenuCanvas.blocksRaycasts = false;

        // Inicia la cinem�tica y el fade out simult�neamente
        if (firstTime)
            playableDirector.Play();

        float startAlpha = mainMenuCanvas.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            mainMenuCanvas.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
            hudCanvas.alpha = Mathf.Lerp(0, 1f, elapsedTime / fadeDuration);
            yield return null;
        }

        // Finaliza la transicion
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        mainMenuCamera.Priority = 1;
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    private IEnumerator FadeOutAndExit()
    {
        mainMenuCanvas.interactable = false;
        mainMenuCanvas.blocksRaycasts = false;

        float startAlpha = mainMenuCanvas.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            mainMenuCanvas.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        Application.Quit();
    }

    private void OpenOptionsMenu()
    {
        PauseMenu.previousMenu = PreviousMenu.MainMenu;
        mainMenuCanvas.gameObject.SetActive(false);
        optionsMenuCanvas.SetActive(true);
    }


    public void EndTimeline()
    {
        PauseMenu.otherMenuOpen = false;
        UserInput.instance.switchActionMap(UserInput.ActionMap.InGame);
    }
}