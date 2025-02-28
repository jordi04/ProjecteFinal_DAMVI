
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "Shield Ability", menuName = "Player Ability/Shield")]
public class PlayerAbility_Shield : PlayerAbility
{
    public override void Use(AbilityUseType useType)
    {
        Debug.Log("Shield ability used");
    }
}   