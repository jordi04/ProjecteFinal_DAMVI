using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Fireball Ability", menuName = "Player Ability/Fireball")]
public class PlayerAbility_FireBall : PlayerAbility
{
    [SerializeField] private uint damage = 10;
    [SerializeField] private uint manaCost = 10;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 10f;
    [SerializeField] private float fireballLifetime = 3f; // How many seconds the fireball exists before disappearing

    public override void Use(AbilityUseType useType)
    {
        if (useType == AbilityUseType.Pressed)
        {
            if (ManaSystem.Instance.TryConsumeMana(manaCost))
                ThrowFireBall();
            else
                HandleInsufficientMana();
        }
    }

    private void HandleInsufficientMana()
    {
        Debug.Log("No hay suficiente maná para lanzar la bola de fuego.");
    }

    private void ThrowFireBall()
    {
        Transform spawnPoint = GameManager.Instance.mainCamera.transform;
        //Dont instantiate Use a pool
        GameObject fireball = Instantiate(fireballPrefab, spawnPoint.position, spawnPoint.rotation);

        // Add a lifetime component to the fireball
        FireballLifetime lifetime = fireball.AddComponent<FireballLifetime>();
        lifetime.SetLifetime(fireballLifetime);

        //Dont instantiate Use a pool
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = spawnPoint.forward * fireballSpeed;
        }
    }
}

// Simple component to handle the fireball's lifetime
public class FireballLifetime : MonoBehaviour
{
    private float _lifetime = 3f;
    private float _timer = 0f;

    public void SetLifetime(float lifetime)
    {
        _lifetime = lifetime;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= _lifetime)
        {
            // You might want to add some VFX here before destroying
            Destroy(gameObject);
        }
    }
}