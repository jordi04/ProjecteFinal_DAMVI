using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Fireball Ability", menuName = "Player Ability/Fireball")]
public class PlayerAbility_FireBall : PlayerAbility
{
    [SerializeField] private uint damage = 10;
    [SerializeField] private uint manaCost = 10;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 10f;

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
        //Dont instantiate Use a pool

        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = spawnPoint.forward * fireballSpeed;
        }
    }
}
