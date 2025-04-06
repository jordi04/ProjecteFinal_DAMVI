using UnityEngine;

public class RockProjectile : MonoBehaviour
{
    [Header("Fragmentation Settings")]
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField] private int fragmentCount = 5;
    [SerializeField] private float fragmentSpread = 3f;
    [SerializeField] private float fragmentDamageMultiplier = 0.5f;

    [Header("Impact Settings")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private LayerMask collisionLayers;

    private float damage;
    private GameObject owner;
    private bool hasCollided = false;

    private Vector3 targetPosition;
    private float speed;
    private float arcHeight;
    private Vector3 startPosition;
    private float progress = 0f;

    public void InitializeLobbedTrajectory(Vector3 start, Vector3 target, float arcHeight, float speed)
    {
        startPosition = start;
        targetPosition = target;
        this.arcHeight = arcHeight;
        this.speed = speed;
    }

    public void SetDamage(float damage) => this.damage = damage;
    public void SetOwner(GameObject owner) => this.owner = owner;

    void Update()
    {
        if (hasCollided) return;

        progress += Time.deltaTime * speed;
        progress = Mathf.Clamp01(progress);

        Vector3 currentPosition = CalculateParabolicPosition(startPosition, targetPosition, progress, arcHeight);
        transform.position = currentPosition;

        if (progress >= 1f)
        {
            HandleCollision(null);
        }
    }

    private Vector3 CalculateParabolicPosition(Vector3 start, Vector3 end, float t, float height)
    {
        float parabolicT = t * 2 - 1;
        Vector3 horizontal = Vector3.Lerp(start, end, t);
        Vector3 vertical = Mathf.Pow(parabolicT, 2) * height * Vector3.up;
        return horizontal + vertical;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided || !IsValidCollision(collision)) return;

        HandleCollision(collision);
    }

    private bool IsValidCollision(Collision collision)
    {
        return collision.gameObject != owner &&
              (collisionLayers.value & (1 << collision.gameObject.layer)) != 0;
    }

    private void HandleCollision(Collision collision)
    {
        hasCollided = true;

        // Daño inicial
        ApplyDamage(transform.position, explosionRadius, damage);

        // Fragmentación
        if (fragmentPrefab != null)
        {
            CreateFragments();
        }

        // Efectos
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void ApplyDamage(Vector3 center, float radius, float damageAmount)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, radius);
        foreach (var hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null && hitCollider.gameObject != owner)
            {
                damageable.TakeDamage(damageAmount);
            }
        }
    }

    private void CreateFragments()
    {
        for (int i = 0; i < fragmentCount; i++)
        {
            Vector3 spread = new Vector3(
                Random.Range(-fragmentSpread, fragmentSpread),
                0.5f,
                Random.Range(-fragmentSpread, fragmentSpread)
            );

            Vector3 spawnPosition = transform.position + spread * 0.5f;
            GameObject fragment = Instantiate(fragmentPrefab, spawnPosition, Quaternion.identity);

            RockFragment fragmentScript = fragment.GetComponent<RockFragment>();
            if (fragmentScript != null)
            {
                fragmentScript.InitializeFragment(damage * fragmentDamageMultiplier, owner);
            }
        }
    }
}