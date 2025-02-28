using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Shield Ability", menuName = "Player Ability/Shield")]
public class PlayerAbility_Shield : PlayerAbility
{
    [SerializeField] private float shieldDuration = 3f;
    [SerializeField] private float manaCost = 15f;
    [SerializeField] private float damageReduction = 0.5f; // 50% damage reduction
    [SerializeField] private GameObject shieldPrefab; // Reference to your shield prefab

    private float shieldActiveTime = 0f;
    private bool isShieldActive = false;
    private GameObject shieldInstance = null;

    public override void Use(AbilityUseType useType)
    {
        switch (useType)
        {
            case AbilityUseType.Pressed:
                if (ManaSystem.Instance.TryConsumeMana(manaCost))
                {
                    ActivateShield();
                }
                break;
            case AbilityUseType.Held:
                // Update shield effect if needed
                break;
            case AbilityUseType.Released:
                // Could be used to trigger a shield bash effect
                break;
        }
    }

    private void ActivateShield()
    {
        isShieldActive = true;
        shieldActiveTime = Time.time + shieldDuration;

        // Create shield instance if it doesn't exist
        if (shieldInstance == null && shieldPrefab != null)
        {
            // Instantiate the shield prefab and parent it to the player
            shieldInstance = Instantiate(shieldPrefab,
                                        GameManager.Instance.player.transform.position,
                                        Quaternion.identity);
            shieldInstance.transform.parent = GameManager.Instance.player.transform;
            shieldInstance.transform.localPosition = Vector3.zero;

            // You can still adjust material properties if needed
            Renderer renderer = shieldInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.5f, 1f, 0.3f);
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.renderQueue = 3000;
            }

            // Handle any collider requirements - make sure your prefab has appropriate collider setup
        }

        // Register for damage events to provide protection
        // You'd need to implement a damage system first

        // Start shield timer
        MonoBehaviour monoBehaviour = GameManager.Instance.player.GetComponent<MonoBehaviour>();
        monoBehaviour.StartCoroutine(ShieldTimer());
    }

    private IEnumerator ShieldTimer()
    {
        while (Time.time < shieldActiveTime)
        {
            yield return null;
        }
        DeactivateShield();
    }

    private void DeactivateShield()
    {
        isShieldActive = false;
        if (shieldInstance != null)
        {
            GameObject.Destroy(shieldInstance);
            shieldInstance = null;
        }
        // Unregister from damage events
    }
}