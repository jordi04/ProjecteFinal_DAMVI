using UnityEngine;

[CreateAssetMenu(fileName = "Swap Ability", menuName = "Player Ability/Swap")]
public class PlayerAbility_Swap : PlayerAbility
{
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private uint manaCost = 20;


    private GameObject lastHitObject;

    public override void Use(AbilityUseType useType)
    {
        switch(useType)
        {
            case AbilityUseType.Held:
                if(ManaSystem.instance.HasManaAmount(manaCost))
                    PerformRaycast();
                break;
            case AbilityUseType.Released:
                if (lastHitObject != null)
                {
                    if (ManaSystem.instance.TryConsumeMana(manaCost))
                        SwapWithObject();
                }
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

                Debug.Log("Swap Ability Held - Hit Swapable Object: " + hitObject.name);
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