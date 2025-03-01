using UnityEngine;
using UnityEngine.InputSystem;

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
    private InputAction ability1_Action;
    private InputAction ability2_Action;
    private InputAction ability3_Action;
    private InputAction ability4_Action;

    //InMenu Actions
    private InputAction unpauseMenu_Action;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
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
        ability1_Action = inGameMap.FindAction("Ability1");
        ability2_Action = inGameMap.FindAction("Ability2");
        ability3_Action = inGameMap.FindAction("Ability3");
        ability4_Action = inGameMap.FindAction("Ability4");

        //InMenu Actions
        unpauseMenu_Action = inMenuMap.FindAction("PauseMenu");
    }
    private void UpdateInputs()
    {
        if (PlayerInput.currentActionMap.name == "InGame")
        {
            //movementInput = move_Action.ReadValue<Vector2>();
            pauseMenuPressed = pauseMenu_Action.WasPressedThisFrame();

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

    public void switchActionMap(ActionMap map)
    {
        string mapName = map.ToString();
        if (PlayerInput.currentActionMap.name != mapName)
        {
            Debug.Log("Switching to " + mapName);
            PlayerInput.SwitchCurrentActionMap(mapName);
        }

    }
}
