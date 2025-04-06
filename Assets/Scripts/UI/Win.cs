using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Win : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void playAgain()
    {
        SceneManager.LoadScene("MainGame");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
