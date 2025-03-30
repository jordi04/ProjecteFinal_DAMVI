using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Button playButton;
    [SerializeField] Button exitButton;
    [SerializeField] CanvasGroup canvas;
    [SerializeField] PlayableDirector playableDirector;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playButton.onClick.AddListener(Play);
        exitButton.onClick.AddListener(ExitGame);
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveListener(Play);
        exitButton.onClick.RemoveListener(ExitGame);
    }

    void Play()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        this.gameObject.SetActive(false);
        playableDirector.Play();
    }

    void ExitGame()
    {
        Application.Quit();
    }
}
