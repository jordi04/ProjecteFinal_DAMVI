using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] Transform player;
    private NavMeshAgent enemy;

    [SerializeField] float stoppingDistance = 5f;
    [SerializeField] float velocityThreshold = 0.1f;

    private bool isEnemyStopped = false;

    // Variables para el disparo
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform shootPoint;
    [SerializeField] GameObject targetObject; // Objeto al que apuntar
    [SerializeField] float fireRate = 1f;
    [SerializeField] float bulletSpeed = 1f;
    private float nextFireTime = 0f;

    private void Start()
    {
        enemy = GetComponent<NavMeshAgent>();
        enemy.stoppingDistance = stoppingDistance;
    }

    void Update()
    {
        enemy.SetDestination(player.position);
        CheckIfStopped();

        if (isEnemyStopped)
        {
            TryToShoot();
        }
    }

    void CheckIfStopped()
    {
        if (enemy.remainingDistance <= enemy.stoppingDistance)
        {
            if (!isEnemyStopped)
            {
                isEnemyStopped = true;
                Debug.Log("El enemigo se ha detenido");
            }
        }
        else
        {
            isEnemyStopped = false;
        }
    }

    void TryToShoot()
    {
        if (Time.time > nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    void Shoot()
    {
        if (bulletPrefab != null && shootPoint != null && targetObject != null)
        {
            // Calcula la dirección hacia el objetivo
            Vector3 directionToTarget = (targetObject.transform.position - shootPoint.position).normalized;

            // Crea la bala y oriéntala hacia el objetivo
            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.LookRotation(directionToTarget));

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.velocity = directionToTarget * bulletSpeed;
                bulletRb.maxLinearVelocity = bulletSpeed;
            }
        }
        else
        {
            Debug.LogWarning("Falta asignar el prefab de la bala, el punto de disparo o el objetivo");
        }
    }
}
