using System.Net;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UserInput : MonoBehaviour
{
    public static UserInput instance { get; private set; }

    public enum ActionMap
    {
        InGame,
        InMenu
    }

    #region Actions States
    public Vector2 movementInput { get; private set; }
    public bool pauseMenuPressed { get; private set; }

    public bool interactPressed { get; private set; }

    public bool ability1Pressed { get; private set; }
    public bool ability1Holded { get; private set; }
    public bool ability1Released { get; private set; }

    public bool ability2Pressed { get; private set; }
    public bool ability2Holded { get; private set; }
    public bool ability2Released { get; private set; }

    public bool ability3Pressed { get; private set; }
    public bool ability3Holded { get; private set; }
    public bool ability3Released { get; private set; }

    public bool ability4Pressed { get; private set; }
    public bool ability4Holded { get; private set; }
    public bool ability4Released { get; private set; }
    #endregion

    [SerializeField]private PlayerInput PlayerInput;

    InputActionMap inGameMap;
    InputActionMap inMenuMap;

    // InGame Actions
    private InputAction move_Action;
    private InputAction pauseMenu_Action;
    private InputAction interact_Action;
    private InputAction ability1_Action;
    private InputAction ability2_Action;
    private InputAction ability3_Action;
    private InputAction ability4_Action;

    // InMenu Actions
    private InputAction unpauseMenu_Action;

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

        InitializePlayerInput();
        switchActionMap(ActionMap.InGame);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Called when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializePlayerInput();
        switchActionMap(ActionMap.InGame); // Ensure the correct action map is active
        ResetInputs(); // Reset inputs after loading a new scene
    }

    // Initialize or reassign the PlayerInput reference
    private void InitializePlayerInput()
    {
        if (PlayerInput == null)
        {
            PlayerInput = FindObjectOfType<PlayerInput>();
            if (PlayerInput == null)
            {
                Debug.LogWarning("PlayerInput component not found in the current scene.");
                return;
            }
        }

        inGameMap = PlayerInput.actions.FindActionMap("InGame", true);
        inMenuMap = PlayerInput.actions.FindActionMap("InMenu", true);

        setInputActions();
    }

    private void Update()
    {
        UpdateInputs();
    }

    private void setInputActions()
    {
        if (inGameMap == null || inMenuMap == null)
            return;

        // InGame Actions
        pauseMenu_Action = inGameMap.FindAction("PauseMenu");
        interact_Action = inGameMap.FindAction("Interact");
        ability1_Action = inGameMap.FindAction("Ability1");
        ability2_Action = inGameMap.FindAction("Ability2");
        ability3_Action = inGameMap.FindAction("Ability3");
        ability4_Action = inGameMap.FindAction("Ability4");

        // InMenu Actions
        unpauseMenu_Action = inMenuMap.FindAction("PauseMenu");
    }

    private void UpdateInputs()
    {
        if (PlayerInput == null)
            return;

        if (PlayerInput.currentActionMap.name == "InGame")
        {
            pauseMenuPressed = pauseMenu_Action?.WasPressedThisFrame() ?? false;
            interactPressed = interact_Action?.WasPressedThisFrame() ?? false;

            ability1Pressed = ability1_Action?.WasPressedThisFrame() ?? false;
            ability1Holded = ability1_Action?.IsPressed() ?? false;
            ability1Released = ability1_Action?.WasReleasedThisFrame() ?? false;

            ability2Pressed = ability2_Action?.WasPressedThisFrame() ?? false;
            ability2Holded = ability2_Action?.IsPressed() ?? false;
            ability2Released = ability2_Action?.WasReleasedThisFrame() ?? false;

            ability3Pressed = ability3_Action?.WasPressedThisFrame() ?? false;
            ability3Holded = ability3_Action?.IsPressed() ?? false;
            ability3Released = ability3_Action?.WasReleasedThisFrame() ?? false;

            ability4Pressed = ability4_Action?.WasPressedThisFrame() ?? false;
            ability4Holded = ability4_Action?.IsPressed() ?? false;
            ability4Released = ability4_Action?.WasReleasedThisFrame() ?? false;
        }
        else if (PlayerInput.currentActionMap.name == "InMenu")
        {
            pauseMenuPressed = unpauseMenu_Action?.WasPressedThisFrame() ?? false;
            if (pauseMenuPressed)
            {
                Debug.Log("Unpausing");
            }
        }
    }

    public ActionMap CurrentActionMap
    {
        get
        {
            if (PlayerInput.currentActionMap.name == "InGame")
                return ActionMap.InGame;
            else if (PlayerInput.currentActionMap.name == "InMenu")
                return ActionMap.InMenu;
            else
                return ActionMap.InGame; // Default to InGame if unknown
        }
    }

    public bool IsInGameMode => CurrentActionMap == ActionMap.InGame;

    public bool IsInMenuMode => CurrentActionMap == ActionMap.InMenu;

    public void switchActionMap(ActionMap map)
    {
        string mapName = map.ToString();
        if (PlayerInput != null && PlayerInput.currentActionMap.name != mapName)
        {
            PlayerInput.SwitchCurrentActionMap(mapName);
            ResetInputs(); // Reset inputs when switching action maps
        }
    }

    private void ResetInputs()
    {
        movementInput = Vector2.zero;
        pauseMenuPressed = false;
        interactPressed = false;

        ability1Pressed = false;
        ability1Holded = false;
        ability1Released = false;

        ability2Pressed = false;
        ability2Holded = false;
        ability2Released = false;

        ability3Pressed = false;
        ability3Holded = false;
        ability3Released = false;

        ability4Pressed = false;
        ability4Holded = false;
        ability4Released = false;
    }
}
