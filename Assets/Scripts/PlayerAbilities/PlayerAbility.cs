using UnityEngine;

public enum AbilityUseType
{
    Pressed,
    Held,
    Released
}

public abstract class PlayerAbility : ScriptableObject
{
    [SerializeField] protected float cooldownTime = 1.5f;
    [SerializeField] protected AbilityType abilityType;

    public abstract void Use(AbilityUseType useType);

    public virtual bool CanUse()
    {
        // Check if ability is on cooldown
        return !AbilityManager.instance.IsAbilityOnCooldown(this);
    }

    protected virtual void StartCooldown()
    {
        AbilityManager.instance.StartCooldown(this, cooldownTime, abilityType);
    }
}