using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GolemProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float defaultDamage = 25f;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float impactRadius = 1.5f;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private bool isRock = true; // Set to true for rocks, false for energy balls

    [Header("Rock Settings")]
    [SerializeField] private bool resetPositionAfterThrow = true; // Whether to reset rock to original position after throw
    [SerializeField] private float resetDelay = 5f; // Time after impact before resetting rock position

    // Private members
    private float damage;
    private GameObject owner;
    private bool hasHit = false;
    private bool isInitialized = false;
    private Rigidbody rb;
    private Collider col;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isResetting = false;

    // For rocks that are picked up and thrown by the golem
    public bool canBePickedUp = true;

    private void Awake()
    {
        // Store original position and rotation
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Get required components
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // For rocks that start in the scene, make sure they're prepared for physics
        if (isRock)
        {
            // By default, make sure physics are enabled for rocks in the scene
            if (rb != null)
            {
                rb.isKinematic = true; // Make kinematic until thrown
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll; // Freeze all constraints to prevent movement
            }

            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = false;
            }
        }
    }

    private void Start()
    {
        // If not explicitly initialized, use default values
        if (!isInitialized)
        {
            damage = defaultDamage;
        }

        // For energy balls (non-rocks), we can still destroy them after a while
        if (!isRock)
        {
            Destroy(gameObject, 5f); // Default lifetime for energy balls
        }
    }

    public void Initialize(float damageAmount, GameObject projectileOwner)
    {
        damage = damageAmount;
        owner = projectileOwner;
        isInitialized = true;

        // Mark as no longer pickupable after being thrown
        if (isRock)
        {
            canBePickedUp = false;

            // Unfreeze the rock for physics when thrown
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None; // Remove constraints when thrown
            }
        }
    }

    // Call this when the golem picks up the rock
    public void OnPickedUp()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (col != null)
        {
            col.enabled = false; // Disable collider while held
        }

        // Cancel any reset in progress
        CancelInvoke("ResetRock");
        isResetting = false;
    }

    // Reset the rock to its original position
    /*
    private void ResetRock()
    {
        if (!isRock) return;

        isResetting = true;

        // Detach from any parent
        transform.SetParent(null);

        // Reset position and rotation
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // Reset physics state
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // Reset flags
        hasHit = false;
        canBePickedUp = true;
        isResetting = false;
    }*/

    private void OnTriggerEnter(Collider other)
    {
        // Skip if already hit or if rock can still be picked up
        if (hasHit || (isRock && canBePickedUp)) return;

        if (other.CompareTag("Player"))
        {
            hasHit = true;
            // Create impact effect if specified
            if (impactEffect != null)
            {
                Instantiate(impactEffect, other.transform.position, Quaternion.identity);
            }
            // Apply area damage
            if (impactRadius > 0)
            {
                ApplyAreaDamage(other.transform.position);
            }
            Destroy(gameObject);
            
        }
        else if (other.CompareTag("Ground"))
        {
            hasHit = true;
            // Create impact effect if specified
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }
            // Apply area damage
            if (impactRadius > 0)
            {
                ApplyAreaDamage(transform.position);
            }

            // For rocks, schedule reset instead of destroying
            if (isRock && resetPositionAfterThrow)
            {
                Invoke("ResetRock", resetDelay);
            }
            else if (!isRock)
            {
                // Only destroy energy balls
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // For rocks that can be picked up, only apply damage when thrown (not pickupable)
        if (isRock && canBePickedUp) return;

        // Avoid handling collision twice
        if (hasHit) return;

        // Ignore collisions with the owner
        if (collision.gameObject == owner) return;

        // For thrown objects, handle impact
        if (!isRock || !canBePickedUp)
        {
            hasHit = true;

            // Create impact effect if specified
            if (impactEffect != null)
            {
                Instantiate(impactEffect, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
            }

            // Apply area damage
            if (impactRadius > 0)
            {
                ApplyAreaDamage(collision.contacts[0].point);
            }
            else
            {
                // Apply direct damage only to the hit object
                IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    //si es player fer-li mal
                    if (collision.gameObject.CompareTag("Player"))
                    {
                        ManaSystem.instance.TakeDamage(damage);
                    }
                }
            }
            Destroy(gameObject);
        }
    }

    private void ApplyAreaDamage(Vector3 center)
    {
        // Find all colliders in the impact radius
        Collider[] colliders = Physics.OverlapSphere(center, impactRadius, damageableLayers);

        foreach (Collider hit in colliders)
        {
            // Skip the owner
            if (hit.gameObject == owner) continue;

            // Calculate damage falloff based on distance
            float distance = Vector3.Distance(center, hit.transform.position);
            float damageMultiplier = 1 - (distance / impactRadius);
            float actualDamage = damage * Mathf.Clamp01(damageMultiplier);

            // Apply damage
            if (hit.CompareTag("Player"))
            {
                ManaSystem.instance.TakeDamage(actualDamage);
            }
            else
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(actualDamage);
                }
            }

            // Add force to rigidbodies
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 direction = (hit.transform.position - center).normalized;
                float forceMagnitude = 10f * damageMultiplier;
                rb.AddForce(direction * forceMagnitude + Vector3.up * 2f, ForceMode.Impulse);
            }
        }
    }

    // Draw gizmos for debug visualization
    private void OnDrawGizmosSelected()
    {
        if (impactRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, impactRadius);
        }
    }
}
