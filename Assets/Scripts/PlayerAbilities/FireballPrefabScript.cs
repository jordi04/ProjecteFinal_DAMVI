using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballPrefabScript : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float explosionRadius = 3f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float effectDuration = 2f;

    [Header("Physics")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask collisionLayers; // Serialized LayerMask for collision detection

    [Header("Curving Behavior")]
    [SerializeField] private float baseSpeed = 15f; // Base speed of the fireball
    [SerializeField] private float curveStrength = 2f; // How strongly the fireball curves
    [SerializeField] private float curveDelay = 0.1f; // Short delay before curving starts

    private bool hasExploded = false;
    private Rigidbody rb;
    private float lifeTime = 0f;
    private float totalLifeTime = 3f;
    private Transform cameraTransform;
    private bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        hasExploded = false;
        lifeTime = 0f;
        isInitialized = false;

        // Get total lifetime from FireballLifetime component if it exists
        FireballLifetime lifetimeComp = GetComponent<FireballLifetime>();
        if (lifetimeComp != null)
        {
            totalLifeTime = lifetimeComp.GetLifetime();
        }

        // Force set the rigidbody properties to ensure it works properly
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.drag = 0;
            rb.angularDrag = 0;
        }
    }

    private void Start()
    {
        // Find the camera on start
        if (GameManager.instance != null && GameManager.instance.mainCamera != null)
        {
            cameraTransform = GameManager.instance.mainCamera.transform;
        }

        // Ensure the fireball starts moving forward if velocity isn't set yet
        if (rb != null && rb.velocity.magnitude < 0.1f)
        {
            rb.velocity = transform.forward * baseSpeed;
        }

        isInitialized = true;
    }

    private void Update()
    {
        if (!hasExploded)
        {
            lifeTime += Time.deltaTime;

            // Double-check camera reference
            if (cameraTransform == null && GameManager.instance != null && GameManager.instance.mainCamera != null)
            {
                cameraTransform = GameManager.instance.mainCamera.transform;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!hasExploded && rb != null && isInitialized)
        {
            // CRITICAL: Force velocity to maintain speed if it's too low
            if (rb.velocity.magnitude < 5f)
            {
                Debug.LogWarning("Fireball velocity too low, resetting to forward direction!");
                rb.velocity = transform.forward * baseSpeed;
            }

            // Apply curving once delay has passed and we have a camera reference
            if (lifeTime > curveDelay && cameraTransform != null)
            {
                ApplyCurving();
            }

            // Make sure the fireball is always rotated in the direction it's moving
            if (rb.velocity.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(rb.velocity);
            }
        }
    }

    private void ApplyCurving()
    {
        // Only curve if this is the most recent fireball
        if (this.gameObject != PlayerAbility_FireBall.mostRecentFireball)
        {
            return; // Skip curving for older fireballs
        }

        // Get camera direction and current velocity direction
        Vector3 cameraDirection = cameraTransform.forward;
        Vector3 currentDirection = rb.velocity.normalized;

        // Calculate how much to steer (more if directions are different)
        float steerFactor = 1f - Vector3.Dot(currentDirection, cameraDirection);

        // Create steering force toward camera direction
        Vector3 steeringForce = (cameraDirection - currentDirection).normalized * curveStrength * steerFactor;

        // Apply steering force
        rb.AddForce(steeringForce, ForceMode.Acceleration);

        // Normalize velocity and apply base speed to maintain consistent speed
        rb.velocity = rb.velocity.normalized * baseSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasExploded)
        {
            // Check if the collider's layer is in our collision layers
            int objectLayer = other.gameObject.layer;
            if (((1 << objectLayer) & collisionLayers.value) != 0)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        hasExploded = true;

        // Create explosion visual effect
        if (explosionEffectPrefab != null)
        {
            GameObject explosionEffect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(explosionEffect, effectDuration);
        }

        // Apply area damage
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);
        foreach (Collider hitCollider in hitColliders)
        {
            // Check for bee enemy
            BeeLifeController beeLife = hitCollider.GetComponent<BeeLifeController>();
            LoboIA mushroom = hitCollider.GetComponent<LoboIA>();
            SerpienteBoss snake = hitCollider.GetComponent<SerpienteBoss>();

            if (beeLife != null)
            {
                // Call the TakeDamage method instead of relying on trigger
                beeLife.TakeDamage(damage);
            }
            else if(mushroom != null)
            {
                mushroom.TakeDamage(damage);
            }
            else if (snake != null)
            {
                snake.TakeDamage(damage);
            }
            //else () PER COMPROVAR ALTRES ENEMICS EN UN FUTUR
        }

        // If this was the most recent fireball, clear the reference
        if (this.gameObject == PlayerAbility_FireBall.mostRecentFireball)
        {
            PlayerAbility_FireBall.mostRecentFireball = null;
        }

        Destroy(gameObject);
    }

    // Handle destruction to clear the static reference
    private void OnDestroy()
    {
        // If this was the most recent fireball, clear the reference
        if (this.gameObject == PlayerAbility_FireBall.mostRecentFireball)
        {
            PlayerAbility_FireBall.mostRecentFireball = null;
        }
    }

    // Visualization for debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}