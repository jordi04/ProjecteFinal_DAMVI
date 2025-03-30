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
        this.gameObject.SetActive(false);
        playableDirector.Play();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
