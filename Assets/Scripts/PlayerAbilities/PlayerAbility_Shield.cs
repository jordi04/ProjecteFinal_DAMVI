using UnityEngine;

[CreateAssetMenu(fileName = "Shield Ability", menuName = "Player Ability/Shield")]
public class PlayerAbility_Shield : PlayerAbility
{
    [SerializeField] private float shieldDuration = 3f;
    [SerializeField] private float manaCost = 15f;
    [SerializeField] private float damageReduction = 0.5f; // 50% damage reduction

    private float shieldActiveTime = 0f;
    private bool isShieldActive = false;
    private GameObject shieldVisual = null;

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

        // Create shield visual if it doesn't exist
        if (shieldVisual == null)
        {
            // Create shield visual around player
            shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldVisual.transform.localScale = new Vector3(3f, 3f, 3f);
            shieldVisual.transform.parent = GameManager.Instance.player.transform;
            shieldVisual.transform.localPosition = Vector3.zero;

            // Make it semi-transparent
            Renderer renderer = shieldVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.5f, 1f, 0.3f);
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.renderQueue = 3000;
            }

            // Remove collider as it's just visual
            Destroy(shieldVisual.GetComponent<SphereCollider>());
        }

        // Register for damage events to provide protection
        // You'd need to implement a damage system first

        // Start shield timer
        MonoBehaviour monoBehaviour = GameManager.Instance.player.GetComponent<MonoBehaviour>();
        monoBehaviour.StartCoroutine(ShieldTimer());
    }

    private System.Collections.IEnumerator ShieldTimer()
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

        if (shieldVisual != null)
        {
            GameObject.Destroy(shieldVisual);
            shieldVisual = null;
        }

        // Unregister from damage events
    }
}