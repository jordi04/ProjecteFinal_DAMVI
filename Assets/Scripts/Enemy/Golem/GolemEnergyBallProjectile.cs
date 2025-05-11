using UnityEngine;
using System.Collections;
using StarterAssets;

public class GolemEnergyBallProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float fallSpeed = 15f;

    [Header("Growth Settings")]
    [SerializeField] private float initialScale = 0.2f;
    [SerializeField] private float finalScale = 1.0f;
    [SerializeField] private float growthDuration = 1.5f;
    [SerializeField] private AnimationCurve growthCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Impact Settings")]
    [SerializeField] private float impactRadius = 3f;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private LayerMask groundLayer; // Layer for ground detection
    [SerializeField] private float explosionForce = 10f;

    [Header("Player Knockback")]
    [SerializeField] private float knockbackStrength = 10f;
    [SerializeField] private float knockbackDuration = 0.3f;
    [SerializeField] private AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // Private members
    private GameObject owner;
    private bool hasHit = false;
    private bool isInitialized = false;
    private Vector3 velocity;
    private bool isAttached = true; // Whether the ball is still attached to the golem's hand
    private bool isGrowing = true;  // Whether the ball is in growing phase
    private float growthTimer = 0f;
    private Vector3 originalScale;

    private void Awake()
    {
        // Store original scale for reference
        originalScale = transform.localScale;

        // Start with initial scale
        transform.localScale = originalScale * initialScale;
    }

    private void Start()
    {
        // If not explicitly initialized, use default values
        if (!isInitialized)
        {
            Initialize(damage, null);
        }

        // Start growing effect
        StartCoroutine(GrowOverTime());
    }

    private void Update()
    {
        // Only apply falling velocity if the ball has been thrown and not hit anything
        if (!isAttached && !hasHit)
        {
            // Apply velocity to move the energy ball
            transform.position += velocity * Time.deltaTime;
        }
    }

    public void Initialize(float damageAmount, GameObject projectileOwner)
    {
        damage = damageAmount;
        owner = projectileOwner;
        isInitialized = true;
    }

    // Called when the golem throws the energy ball
    public void OnThrow(float throwForce, Vector3 direction)
    {
        // Detach from parent
        transform.SetParent(null);
        isAttached = false;

        // Set velocity (primarily downward with a bit of forward direction)
        velocity = (Vector3.down * fallSpeed) + (direction.normalized * throwForce);

        // Make sure the ball is at full size when thrown
        StopAllCoroutines();
        transform.localScale = originalScale * finalScale;
        isGrowing = false;
    }

    private IEnumerator GrowOverTime()
    {
        growthTimer = 0f;

        while (growthTimer < growthDuration && isGrowing)
        {
            growthTimer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(growthTimer / growthDuration);
            float scaleFactor = Mathf.Lerp(initialScale, finalScale, growthCurve.Evaluate(normalizedTime));

            transform.localScale = originalScale * scaleFactor;

            yield return null;
        }

        // Ensure we reach the final scale
        if (isGrowing)
        {
            transform.localScale = originalScale * finalScale;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only check for collisions if the ball has been thrown
        if (isAttached || hasHit) return;

        // Check if we hit the player
        if (other.CompareTag("Player"))
        {
            HandleImpact(other.transform.position);
        }
        // Check if we hit the ground
        else if (((1 << other.gameObject.layer) & groundLayer.value) != 0)
        {
            HandleImpact(transform.position);
        }
        // Check if we hit any other damageable object
        else if (((1 << other.gameObject.layer) & damageableLayers.value) != 0)
        {
            HandleImpact(transform.position);
        }
    }

    private void HandleImpact(Vector3 impactPoint)
    {
        hasHit = true;

        // Create impact effect if specified
        if (impactEffect != null)
        {
            Instantiate(impactEffect, impactPoint, Quaternion.identity);
        }

        // Apply area damage and effects
        ApplyAreaEffects(impactPoint);

        // Destroy the energy ball
        Destroy(gameObject);
    }

    private void ApplyAreaEffects(Vector3 center)
    {
        // Find all colliders in the impact radius
        Collider[] colliders = Physics.OverlapSphere(center, impactRadius, damageableLayers);

        foreach (Collider hit in colliders)
        {
            // Skip the owner
            if (hit.gameObject == owner) continue;

            // Calculate damage and effect falloff based on distance
            float distance = Vector3.Distance(center, hit.transform.position);
            float effectMultiplier = 1 - (distance / impactRadius);
            float actualDamage = damage;
            Debug.Log("EnergyBallCrashed");
            // Apply damage
            if (hit.CompareTag("Player"))
            {
                // Apply damage to player
                Debug.Log("Player hit by energy ball!");
                ManaSystem.instance.TakeDamage(actualDamage);

                // Apply knockback to player
                //ApplyPlayerKnockback(hit.gameObject, center, effectMultiplier);
            }
            else
            {
                // Apply damage to other damageable objects
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(actualDamage);
                }

                // Apply force to rigidbodies
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 direction = (hit.transform.position - center).normalized;
                    float forceMagnitude = explosionForce * effectMultiplier;
                    rb.AddForce(direction * forceMagnitude + Vector3.up * (forceMagnitude * 0.5f), ForceMode.Impulse);
                }
            }
        }
    }

    private void ApplyPlayerKnockback(GameObject player, Vector3 impactPoint, float intensityMultiplier)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            // Calculate knockback direction (away from impact point, but maintain Y position)
            Vector3 knockbackDirection = player.transform.position - impactPoint;
            knockbackDirection.y = 0; // Keep level for a more controlled knockback
            knockbackDirection.Normalize();

            // Start the knockback coroutine
            StartCoroutine(KnockbackCoroutine(
                player,
                controller,
                knockbackDirection,
                knockbackStrength * intensityMultiplier
            ));
        }
    }

    private IEnumerator KnockbackCoroutine(GameObject player, CharacterController controller, Vector3 direction, float strength)
    {
        float timer = 0;

        // Store the player's movement script to temporarily disable it
        FirstPersonController playerMovement = player.GetComponent<FirstPersonController>();
        bool wasEnabled = false;

        // Disable player movement during knockback if we found the script
        if (playerMovement != null)
        {
            wasEnabled = playerMovement.enabled;
            playerMovement.enabled = false;
        }

        while (timer < knockbackDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / knockbackDuration;

            // Use the animation curve to control the knockback intensity over time
            float curveValue = knockbackCurve.Evaluate(progress);

            // Calculate the movement for this frame
            Vector3 movement = direction * strength * curveValue * Time.deltaTime;

            // Move the character controller
            controller.Move(movement);

            yield return null;
        }

        // Re-enable player movement if it was enabled before
        if (playerMovement != null && wasEnabled)
        {
            playerMovement.enabled = true;
        }
    }

    // Draw gizmos for debug visualization
    private void OnDrawGizmosSelected()
    {
        if (impactRadius > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, impactRadius);
        }
    }
}
