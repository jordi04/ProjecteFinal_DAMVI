using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using Cinemachine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Button playButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button exitButton;

    [SerializeField] CanvasGroup mainMenuCanvas;
    [SerializeField] CanvasGroup hudCanvas;
    [SerializeField] GameObject optionsMenuCanvas;

    [SerializeField] PlayableDirector playableDirector;
    [SerializeField] float fadeDuration = 1f;
    [SerializeField] CinemachineVirtualCamera mainMenuCamera;
    private bool firstTime = true;

    private void Start()
    {
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
        // Bloquea los botones durante la transición
        mainMenuCanvas.interactable = false;
        mainMenuCanvas.blocksRaycasts = false;

        // Inicia la cinemática y el fade out simultáneamente
        if (firstTime)
            playableDirector.Play();

        float startAlpha = mainMenuCanvas.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            mainMenuCanvas.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
            hudCanvas.alpha = Mathf.Lerp(0, 1f, elapsedTime / fadeDuration);
            yield return null;
        }

        // Finaliza la transición
        gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        mainMenuCamera.Priority = 1;
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
        mainMenuCanvas.gameObject.SetActive(false);
        optionsMenuCanvas.SetActive(true);

    }

    public void EndTimeline()
    {
        PauseMenu.otherMenuOpen = false;
        UserInput.instance.switchActionMap(UserInput.ActionMap.InGame);
    }
}
