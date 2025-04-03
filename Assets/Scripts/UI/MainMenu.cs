using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using Cinemachine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Button playButton;
    [SerializeField] Button exitButton;
    [SerializeField] CanvasGroup canvas;
    [SerializeField] PlayableDirector playableDirector;
    [SerializeField] float fadeDuration = 1f;
    [SerializeField] CinemachineVirtualCamera mainMenuCamera;

    private void Start()
    {
        UserInput.instance.switchActionMap(UserInput.ActionMap.InMenu);
    }

    private void OnEnable()
    {
        PauseMenu.otherMenuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playButton.onClick.AddListener(Play);
        exitButton.onClick.AddListener(ExitGame);
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveListener(Play);
        exitButton.onClick.RemoveListener(ExitGame);
        PauseMenu.otherMenuOpen = false;
    }

    public void Play()
    {
        StartCoroutine(FadeOutAndPlay());
    }

    public void ExitGame()
    {
        StartCoroutine(FadeOutAndExit());
    }

    private IEnumerator FadeOutAndPlay()
    {
        // Bloquea los botones durante la transición
        canvas.interactable = false;
        canvas.blocksRaycasts = false;

        // Inicia la cinemática y el fade out simultáneamente
        playableDirector.Play();

        float startAlpha = canvas.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvas.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        // Finaliza la transición
        this.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        mainMenuCamera.Priority = 1;
    }

    private IEnumerator FadeOutAndExit()
    {
        canvas.interactable = false;
        canvas.blocksRaycasts = false;

        float startAlpha = canvas.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvas.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        Application.Quit();
    }
}
