using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public GameObject player { get; private set; }
    public Camera mainCamera { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPlayerAndCamera();
    }


    private void FindPlayerAndCamera()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        mainCamera = Camera.main;

        if (player == null)
        {
            Debug.LogWarning("Player not found in the current scene.");
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found in the current scene.");
        }
    }

    public void ResetGame()
    {
        // Implement reset logic here
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
