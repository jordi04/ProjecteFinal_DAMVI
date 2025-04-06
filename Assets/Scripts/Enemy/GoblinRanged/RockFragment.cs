using UnityEngine;

public class RockFragment : MonoBehaviour
{
    [SerializeField] private float fragmentSpeed = 5f;
    [SerializeField] private float fragmentLifetime = 1.5f;
    [SerializeField] private float fragmentDamageRadius = 1.5f;

    private float damage;
    private GameObject owner;
    private Vector3 randomDirection;

    public void InitializeFragment(float damage, GameObject owner)
    {
        this.damage = damage;
        this.owner = owner;
        randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.5f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        Destroy(gameObject, fragmentLifetime);
    }

    void Update()
    {
        transform.position += randomDirection * fragmentSpeed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == owner) return;

        ApplyFragmentDamage();
        Destroy(gameObject);
    }

    private void ApplyFragmentDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, fragmentDamageRadius);
        foreach (var hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null && hitCollider.gameObject != owner)
            {
                damageable.TakeDamage(damage);
            }
        }
    }
}