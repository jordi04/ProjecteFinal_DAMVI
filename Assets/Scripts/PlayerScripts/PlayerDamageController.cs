using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDamageController : MonoBehaviour, IDamageable
{
    public void TakeDamage(float amount)
    {
        ManaSystem.instance.TakeDamage(amount);
    }

    public float GetCurrentHealth()
    {
        return ManaSystem.instance.GetCurrentHealth();
    }

    public float GetMaxHealth()
    {
        return ManaSystem.instance.GetMaxHealth();
    }

    public bool IsDead()
    {
        return ManaSystem.instance.IsDead();
    }
}
