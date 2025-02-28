using UnityEngine;

public enum AbilityUseType
{
    Pressed,
    Held,
    Released
}

public abstract class PlayerAbility : ScriptableObject
{
    public abstract void Use(AbilityUseType useType);
}
