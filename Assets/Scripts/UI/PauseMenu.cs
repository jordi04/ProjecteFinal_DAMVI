using Cinemachine;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{

    [SerializeField] GameObject pauseMenu_Canvas;

    private UserInput userInput;
    void Start()
    {
        userInput = UserInput.instance;
    }

    void Update()
    {
        if(userInput.pauseMenuPressed)
        {  
            pauseMenu_Canvas.SetActive(!pauseMenu_Canvas.activeSelf);
            if (pauseMenu_Canvas.activeSelf)
            {
                Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }       
        }
    }
}
