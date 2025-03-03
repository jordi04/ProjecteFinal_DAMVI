using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UserInput : MonoBehaviour
{

    public static UserInput instance {  get; private set; }

    public enum ActionMap
    {
        InGame,
        InMenu
    }

    #region Actions States
    public Vector2 movementInput { get; private set; }
    public bool pauseMenuPressed { get; private set; }

    public bool interactPressed { get; private set; }
    //If needed: public bool interactHolded { get; private set; }
    //If needed: public bool interactReleased { get; private set; }


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


    [SerializeField] private PlayerInput PlayerInput;
    
    InputActionMap inGameMap;
    InputActionMap inMenuMap;


    //InGame Actions
    private InputAction move_Action;
    private InputAction pauseMenu_Action;
    private InputAction interact_Action;
    private InputAction ability1_Action;
    private InputAction ability2_Action;
    private InputAction ability3_Action;
    private InputAction ability4_Action;

    //InMenu Actions
    private InputAction unpauseMenu_Action;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        FindPlayerInput();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPlayerInput();
    }

    private void FindPlayerInput()
    {
        if (PlayerInput == null)
        {
            PlayerInput = FindObjectOfType<PlayerInput>();
            if (PlayerInput == null)
            {
                Debug.LogWarning("PlayerInput not found in the scene.");
                return;
            }
        }

        inGameMap = PlayerInput.actions.FindActionMap("InGame", true);
        inMenuMap = PlayerInput.actions.FindActionMap("InMenu", true);

        setInputActions();
        switchActionMap(ActionMap.InGame);
    }


    private void Update()
    {
        UpdateInputs();
    }



    private void setInputActions()
    {
        //InGame Actions
        //move_Action = inGameMap.FindAction("Move");
        pauseMenu_Action = inGameMap.FindAction("PauseMenu");
        interact_Action = inGameMap.FindAction("Interact");
        ability1_Action = inGameMap.FindAction("Ability1");
        ability2_Action = inGameMap.FindAction("Ability2");
        ability3_Action = inGameMap.FindAction("Ability3");
        ability4_Action = inGameMap.FindAction("Ability4");

        //InMenu Actions
        unpauseMenu_Action = inMenuMap.FindAction("PauseMenu");
    }
    private void UpdateInputs()
    {
        if(PlayerInput == null)
        {
            return;
        }

        if (PlayerInput.currentActionMap.name == "InGame")
        {
            //movementInput = move_Action.ReadValue<Vector2>();
            pauseMenuPressed = pauseMenu_Action.WasPressedThisFrame();
            interactPressed = interact_Action.WasPressedThisFrame();

            ability1Pressed = ability1_Action.WasPressedThisFrame();
            ability1Holded = ability1_Action.IsPressed();
            ability1Released = ability1_Action.WasReleasedThisFrame();

            ability2Pressed = ability2_Action.WasPressedThisFrame();
            ability2Holded = ability2_Action.IsPressed();
            ability2Released = ability2_Action.WasReleasedThisFrame();

            ability3Pressed = ability3_Action.WasPressedThisFrame();
            ability3Holded = ability3_Action.IsPressed();
            ability3Released = ability3_Action.WasReleasedThisFrame();

            ability4Pressed = ability4_Action.WasPressedThisFrame();
            ability4Holded = ability4_Action.IsPressed();
            ability4Released = ability4_Action.WasReleasedThisFrame();
        }
        else if (PlayerInput.currentActionMap.name == "InMenu")
        {
            pauseMenuPressed = unpauseMenu_Action.WasPressedThisFrame();
            if(pauseMenuPressed)
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
        if (PlayerInput.currentActionMap.name != mapName)
        {
            PlayerInput.SwitchCurrentActionMap(mapName);
            ResetInputs(); // Llamar a ResetInputs() al cambiar de mapa
        }
    }

    // Nueva función para resetear inputs
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
