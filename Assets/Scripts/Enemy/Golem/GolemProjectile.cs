using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GolemProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float defaultDamage = 25f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float impactRadius = 1.5f;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private bool isRock = true; // Set to true for rocks, false for energy balls

    // Private members
    private float damage;
    private GameObject owner;
    private bool hasHit = false;
    private bool isInitialized = false;
    private Rigidbody rb;
    private Collider col;

    // For rocks that are picked up and thrown by the golem
    public bool canBePickedUp = true;

    private void Awake()
    {
        // Get required components
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // For rocks that start in the scene, make sure they're prepared for physics
        if (isRock)
        {
            // By default, make sure physics are enabled for rocks in the scene
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
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

        // Destroy the projectile after lifeTime seconds if it hasn't hit anything
        // Only start the timer if this is a thrown object (energy balls or thrown rocks)
        if (!canBePickedUp || !isRock)
        {
            Destroy(gameObject, lifeTime);
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

            // Start lifetime destruction countdown
            Destroy(gameObject, lifeTime);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        //si ja ha pegat no tornar-ho a fer
        if (hasHit) return;

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
        }
        else if (other.CompareTag("Ground"))
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

            // Destroy energy balls on impact, but only destroy rocks if they were thrown
            if (!isRock || (isRock && !canBePickedUp))
            {
                // Destroy the projectile
                Destroy(gameObject);
            }
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