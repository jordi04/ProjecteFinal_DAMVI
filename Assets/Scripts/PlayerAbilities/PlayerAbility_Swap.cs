using UnityEngine;

[CreateAssetMenu(fileName = "Swap Ability", menuName = "Player Ability/Swap")]
public class PlayerAbility_Swap : PlayerAbility
{
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private uint manaCost = 20;
    private GameObject lastHitObject;
    private bool isHolding = false;

    public override void Use(AbilityUseType useType)
    {
        switch (useType)
        {
            case AbilityUseType.Pressed:
                // Initial press - check if we can use the ability
                if (CanUse() && ManaSystem.instance.HasManaAmount(manaCost))
                {
                    isHolding = true;
                }
                break;

            case AbilityUseType.Held:
                // If we're allowed to hold, perform the raycast without checking mana every frame
                if (isHolding)
                {
                    PerformRaycast();
                }
                break;

            case AbilityUseType.Released:
                // Only try to swap if we were holding and found a valid object
                if (isHolding && lastHitObject != null)
                {
                    if (ManaSystem.instance.TryConsumeMana(manaCost))
                    {
                        SwapWithObject();
                        // Start cooldown after successful swap
                        AbilityManager.instance.StartCooldown(this, cooldownTime, AbilityType.Swap);
                    }
                }
                // Reset state regardless of whether swap was successful
                isHolding = false;
                ResetLastHitObject();
                break;
        }
    }

    private void PerformRaycast()
    {
        Ray ray = GameManager.instance.mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            if (hit.collider.CompareTag("Swapable"))
            {
                GameObject hitObject = hit.collider.gameObject;
                if (lastHitObject != null && lastHitObject != hitObject)
                {
                    ResetLastHitObject();
                }
                // Set the new object red
                Renderer renderer = hitObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                    lastHitObject = hitObject;
                }
            }
            else
            {
                ResetLastHitObject();
            }
        }
        else
        {
            ResetLastHitObject();
        }
    }

    private void ResetLastHitObject()
    {
        if (lastHitObject != null)
        {
            Renderer renderer = lastHitObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.white; // Reset to default color
            }
            lastHitObject = null;
        }
    }

    private void SwapWithObject()
    {
        Vector3 playerPos = GameManager.instance.player.transform.position;
        Vector3 objectPos = lastHitObject.transform.position;
        Rigidbody playerRb = GameManager.instance.player.GetComponent<Rigidbody>();
        CharacterController playerCC = GameManager.instance.player.GetComponent<CharacterController>();
        bool wasKinematic = false;
        if (playerRb != null)
        {
            wasKinematic = playerRb.isKinematic;
            playerRb.isKinematic = true;
        }
        if (playerCC != null)
        {
            playerCC.enabled = false;
        }
        // Perform the swap
        GameManager.instance.player.transform.position = objectPos;
        lastHitObject.transform.position = playerPos;
        // Re-enable physics components
        if (playerRb != null)
        {
            playerRb.isKinematic = wasKinematic;
        }
        if (playerCC != null)
        {
            playerCC.enabled = true;
        }
        Debug.Log("Swapped positions with " + lastHitObject.name);
    }
}