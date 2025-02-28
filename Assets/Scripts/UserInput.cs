using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UserInput : MonoBehaviour
{

    public static UserInput instance {  get; private set; }


    #region Actions States
    public Vector2 movementInput { get; private set; }

    public bool menuOpenClosePressed { get; private set; }

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

    private InputAction move_Action;
    private InputAction menuOpenClose_Action;
    private InputAction ability1_Action;
    private InputAction ability2_Action;
    private InputAction ability3_Action;
    private InputAction ability4_Action;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        setInputActions();
    }

    private void Update()
    {
        UpdateInputs();
    }


    private void setInputActions()
    {
        //move_Action = PlayerInput.actions["Move"];
        //menuOpenClose_Action = PlayerInput.actions["MenuOpenClose"];
        ability1_Action = PlayerInput.actions["Ability1"];
        ability2_Action = PlayerInput.actions["Ability2"];
        ability3_Action = PlayerInput.actions["Ability3"];
        ability4_Action = PlayerInput.actions["Ability4"];
    }
    private void UpdateInputs()
    {
        //movementInput = move_Action.ReadValue<Vector2>();

        //menuOpenClosePressed = menuOpenClose_Action.WasPressedThisFrame();


        ability1Pressed =   ability1_Action.WasPressedThisFrame();
        ability1Holded =    ability1_Action.IsPressed();
        ability1Released =  ability1_Action.WasReleasedThisFrame();
                
        ability2Pressed =   ability2_Action.WasPressedThisFrame();
        ability2Holded =    ability2_Action.IsPressed();
        ability2Released =  ability2_Action.WasReleasedThisFrame();

        ability3Pressed =   ability3_Action.WasPressedThisFrame();
        ability3Holded =    ability3_Action.IsPressed();
        ability3Released =  ability3_Action.WasReleasedThisFrame();
        
        ability4Pressed =   ability4_Action.WasPressedThisFrame();
        ability4Holded =    ability4_Action.IsPressed();
        ability4Released =  ability4_Action.WasReleasedThisFrame();
        

    }
}
