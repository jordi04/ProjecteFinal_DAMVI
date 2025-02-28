using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
    Fireball,
    Shield,
    Swap
}

public class PlayerController : MonoBehaviour
{
    [Header("Abilities")]
    [SerializeField] private PlayerAbility fireBall;
    [SerializeField] private PlayerAbility shield;
    [SerializeField] private PlayerAbility swap;

    

    //Change this to a configuration option and the confirmation key to a inputSystem action
    [Header("Ability Confirmation")]
    [SerializeField] private bool requireConfirmation = false;
    [SerializeField] private KeyCode confirmationKey = KeyCode.Space;

    private Dictionary<AbilityType, PlayerAbility> abilityMap;
    
    //Singleton References
    private UserInput userInput;

    private void Start()
    {
        userInput = UserInput.instance;
        if (userInput == null)
        {
            userInput = FindObjectOfType<UserInput>();
            Debug.LogWarning("Had to find UserInput through FindObjectOfType");
        }

        InitializeAbilityMap();
    }

    private void InitializeAbilityMap()
    {
        abilityMap = new Dictionary<AbilityType, PlayerAbility>
        {
            { AbilityType.Fireball, fireBall },
            { AbilityType.Shield, shield },
            { AbilityType.Swap, swap }
        };
    }

    private void Update()
    {
        HandleAbilityInput();
    }

    private void HandleAbilityInput()
    {
        if (userInput == null)
        {
            Debug.LogError("UserInput instance is null");
            return;
        }
        HandleAbilityState(AbilityType.Fireball, userInput.ability1Pressed, userInput.ability1Holded, userInput.ability1Released);
        HandleAbilityState(AbilityType.Shield, userInput.ability2Pressed, userInput.ability2Holded, userInput.ability2Released);
        HandleAbilityState(AbilityType.Swap, userInput.ability3Pressed, userInput.ability3Holded, userInput.ability3Released);
    }

    private void HandleAbilityState(AbilityType abilityType, bool isPressed, bool isHeld, bool isReleased)
    {
        if (abilityMap.TryGetValue(abilityType, out PlayerAbility ability))
        {
            if (isPressed){
                if (!requireConfirmation || (requireConfirmation && Input.GetKeyDown(confirmationKey)))//Does not work
                    ability.Use(AbilityUseType.Pressed);
            }
            else if (isHeld)
                ability.Use(AbilityUseType.Held);
            else if (isReleased)
                ability.Use(AbilityUseType.Released);
        }
    }
}
