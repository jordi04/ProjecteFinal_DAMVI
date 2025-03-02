using System;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Fireball Ability", menuName = "Player Ability/Fireball")]
public class PlayerAbility_FireBall : PlayerAbility
{
    [SerializeField] private uint damage = 10;
    [SerializeField] private uint manaCost = 10;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 10f;
    [SerializeField] private float fireballLifetime = 3f;

    [Header("UI")]
    [SerializeField] private Sprite normalIcon;
    [SerializeField] private Sprite cooldownIcon;

    private void OnEnable()
    {
        abilityType = AbilityType.Fireball;
    }

    public override void Use(AbilityUseType useType)
    {
        if (useType == AbilityUseType.Pressed && CanUse())
        {
            // Only check mana if the ability is not on cooldown
            if (ManaSystem.instance.HasManaAmount(manaCost))
            {
                // Consume mana first
                ManaSystem.instance.TryConsumeMana(manaCost);
                // Then throw fireball and start cooldown
                ThrowFireBall();
                // Start cooldown
                AbilityManager.instance.StartCooldown(this, cooldownTime, abilityType);
            }
            else
            {
                HandleInsufficientMana();
            }
        }
    }

    private void HandleInsufficientMana()
    {
        Debug.Log("No hay suficiente maná para lanzar la bola de fuego.");
    }

    private void ThrowFireBall()
    {
        Transform spawnPoint = GameManager.instance.mainCamera.transform;
        // Using Object Pooling instead of Instantiate
        GameObject fireball = ObjectPool.GetPooledObject(fireballPrefab);
        if (fireball == null)
        {
            fireball = Instantiate(fireballPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            fireball.transform.position = spawnPoint.position;
            fireball.transform.rotation = spawnPoint.rotation;
            fireball.SetActive(true);
        }
        // Add or get a FireballLifetime component
        FireballLifetime lifetime = fireball.GetComponent<FireballLifetime>() ?? fireball.AddComponent<FireballLifetime>();
        lifetime.SetLifetime(fireballLifetime);
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = spawnPoint.forward * fireballSpeed;
        }
    }
}
