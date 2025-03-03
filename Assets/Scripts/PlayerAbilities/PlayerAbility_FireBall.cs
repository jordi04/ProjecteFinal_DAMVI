using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;

[CreateAssetMenu(fileName = "Fireball Ability", menuName = "Player Ability/Fireball")]
public class PlayerAbility_FireBall : PlayerAbility
{
    [SerializeField] private uint damage = 10;
    [SerializeField] private uint manaCost = 10;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 15f;
    [SerializeField] private float fireballLifetime = 3f;
    [SerializeField] private float momentumFactor = 0.5f; // How much player momentum affects the fireball (0-1)

    // Static reference to the most recently thrown fireball
    public static GameObject mostRecentFireball = null;

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
        GameObject player = GameManager.instance.player;

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

        // Set this as the most recent fireball
        mostRecentFireball = fireball;

        // Add or get a FireballLifetime component
        FireballLifetime lifetime = fireball.GetComponent<FireballLifetime>() ?? fireball.AddComponent<FireballLifetime>();
        lifetime.SetLifetime(fireballLifetime);

        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Get player's velocity
            CharacterController playerController = player.GetComponent<CharacterController>();
            Vector3 playerVelocity = Vector3.zero;

            if (playerController != null)
            {
                playerVelocity = playerController.velocity;
            }
            else
            {
                FirstPersonController fpsController = player.GetComponent<FirstPersonController>();
                if (fpsController != null)
                {
                    playerVelocity = player.transform.forward * fpsController.MoveSpeed;
                }
            }

            // Clear any existing forces
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Apply player's momentum to the fireball's initial velocity
            Vector3 forwardForce = spawnPoint.forward * fireballSpeed;
            Vector3 momentumForce = playerVelocity * momentumFactor;

            // Combine forces and ensure we have a substantial initial velocity
            rb.velocity = (forwardForce + momentumForce).normalized * fireballSpeed;

            // Make sure useGravity is disabled
            rb.useGravity = false;

            // Add an initial impulse force to ensure momentum
            rb.AddForce(rb.velocity.normalized * fireballSpeed, ForceMode.Impulse);
        }
    }

    public uint GetDamage()
    {
        return damage;
    }
}