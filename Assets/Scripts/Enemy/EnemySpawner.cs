using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Spawning Settings")]
    public GameObject[] enemyPrefabs;   
    public Transform[] spawnPoints;
    public int enemiesPerSpawn = 3;
    public int maxEnemiesSpawnedAtTheSameTime = 10;
    public int maxEnemiesSpawnedTotal = 15;
    public float timeBetweenSpawnPointActivation = 5f;
    public float delayBetweenEnemiesSpawn = 0.5f;
    [SerializeField] Transform player;

    [Header("Spawn Effect Settings")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float enemySpawnDelay = 1.5f;
    [SerializeField] private bool destroyEffectAfterSpawn = true;

    [Header("Spawn Area Settings")]
    [SerializeField] private bool useRandomPositionInArea = true;
    [SerializeField] private float spawnAreaRadius = 2f;
    [SerializeField] private Color spawnAreaColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    [SerializeField] private bool showSpawnAreas = true;

    [Header("Trigger Settings")]
    [SerializeField] private float triggerRadius = 10f;
    [SerializeField] private float clearEnemiesRadius = 20f;
    [SerializeField] private bool showDebugSpheres = true;
    [SerializeField] private Color triggerColor = Color.green;
    [SerializeField] private Color clearColor = Color.red;

    private int enemiesSpawned = 0;
    private List<GameObject> enemiesInScene = new List<GameObject>();
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

    void Start()
    {
        // Ensure player reference is set
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null)
            {
                Debug.LogError("No player found. Please assign the player transform or tag a GameObject as 'Player'.");
            }
        }
    }

    void Update()
    {
        if (player != null)
        {
            // Check if player is within spawn trigger radius
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Player entered spawn trigger
            if (distanceToPlayer <= triggerRadius && !isSpawning)
            {
                Debug.Log("Player entered spawn trigger zone");
                StartSpawning();
            }
            // Player exited spawn trigger
            else if (distanceToPlayer > triggerRadius && isSpawning)
            {
                Debug.Log("Player exited spawn trigger zone");
                StopSpawning();
            }

            // Player exited clear enemies radius
            if (distanceToPlayer > clearEnemiesRadius && enemiesInScene.Count > 0)
            {
                Debug.Log("Player exited clear enemies zone");
                ClearAllEnemies();
            }
        }
    }

    private void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            Debug.Log("Starting to spawn enemies");
            spawnCoroutine = StartCoroutine(SpawnEnemies());
        }
    }

    private void StopSpawning()
    {
        if (isSpawning)
        {
            isSpawning = false;
            Debug.Log("Stopping enemy spawning");
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
    }

    private void ClearAllEnemies()
    {
        int enemiesCleared = enemiesInScene.Count;

        // Destroy all enemies in the scene
        foreach (GameObject enemy in enemiesInScene)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        // Clear the list but keep track of the total spawned
        enemiesInScene.Clear();

        Debug.Log($"Cleared {enemiesCleared} enemies. Total spawned: {enemiesSpawned}");
    }

    private IEnumerator SpawnEnemies()
    {
        while (isSpawning && enemiesSpawned < maxEnemiesSpawnedTotal)
        {
            // Wait until we have room to spawn more enemies
            yield return new WaitUntil(() => enemiesInScene.Count < maxEnemiesSpawnedAtTheSameTime);

            // Activate a spawn point
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (!isSpawning) break;

                // Spawn enemies at this spawn point
                for (int j = 0; j < enemiesPerSpawn; j++)
                {
                    if (!isSpawning ||
                        enemiesInScene.Count >= maxEnemiesSpawnedAtTheSameTime ||
                        enemiesSpawned >= maxEnemiesSpawnedTotal)
                        break;

                    // Select a random enemy prefab
                    GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

                    // Get spawn position (either exact or within radius)
                    Vector3 spawnPosition = GetSpawnPosition(spawnPoints[i]);

                    // First spawn the effect
                    StartCoroutine(SpawnEnemyWithEffect(enemyPrefab, spawnPosition, spawnPoints[i].rotation));

                    // Wait between enemy spawns
                    yield return new WaitForSeconds(delayBetweenEnemiesSpawn);
                }

                // Wait between spawn point activations
                yield return new WaitForSeconds(timeBetweenSpawnPointActivation);
            }
        }

        // If we reached the max total enemies, stop spawning
        if (enemiesSpawned >= maxEnemiesSpawnedTotal)
        {
            isSpawning = false;
            Debug.Log("Reached maximum number of enemies to spawn");
        }
    }

    private Vector3 GetSpawnPosition(Transform spawnPoint)
    {
        if (!useRandomPositionInArea)
            return spawnPoint.position;

        // Get random position within spawn area
        Vector3 randomOffset = Random.insideUnitSphere * spawnAreaRadius;
        randomOffset.y = 0; // Keep on same Y level, remove if you want 3D spawn volume

        return spawnPoint.position + randomOffset;
    }

    private IEnumerator SpawnEnemyWithEffect(GameObject enemyPrefab, Vector3 position, Quaternion rotation)
    {
        GameObject spawnEffect = null;

        // Create spawn effect if available
        if (spawnEffectPrefab != null)
        {
            spawnEffect = Instantiate(spawnEffectPrefab, position, rotation);
            Debug.Log($"Spawned effect at {position}");
        }

        // Wait for the delay
        yield return new WaitForSeconds(enemySpawnDelay);

        // Spawn the actual enemy
        GameObject enemy = Instantiate(enemyPrefab, position, rotation);

        // Set the target for the enemy
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            Debug.Log($"Setting target for enemy {enemy.name}");
            enemyController.SetTarget(player);
            enemyController.spawner = this;
        }
        else
        {
            Debug.LogWarning($"Enemy {enemy.name} does not have an EnemyController component!");
        }

        // Add the enemy to our list and increment the count
        enemiesInScene.Add(enemy);
        enemiesSpawned++;
        Debug.Log($"Spawned enemy {enemiesSpawned}/{maxEnemiesSpawnedTotal}");

        // Destroy the effect if needed
        if (spawnEffect != null && destroyEffectAfterSpawn)
        {
            Destroy(spawnEffect);
        }
    }

    public virtual void EnemyEliminated(GameObject enemy)
    {
        if (enemiesInScene.Contains(enemy))
        {
            enemiesInScene.Remove(enemy);
            Debug.Log($"Enemy eliminated. Remaining enemies: {enemiesInScene.Count}");
        }
    }

    // Visualize the trigger areas and spawn points in the editor
    void OnDrawGizmos()
    {
        // Draw trigger areas
        if (showDebugSpheres)
        {
            // Draw spawn trigger
            Gizmos.color = triggerColor;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);

            // Draw clear trigger
            Gizmos.color = clearColor;
            Gizmos.DrawWireSphere(transform.position, clearEnemiesRadius);
        }

        // Draw spawn areas
        if (showSpawnAreas && spawnPoints != null)
        {
            Gizmos.color = spawnAreaColor;

            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    // Draw spawn point
                    Gizmos.DrawSphere(spawnPoint.position, 0.3f);

                    // Draw spawn area
                    if (useRandomPositionInArea)
                    {
                        Gizmos.DrawWireSphere(spawnPoint.position, spawnAreaRadius);

                        // Draw semi-transparent sphere to show the area
                        Gizmos.color = new Color(spawnAreaColor.r, spawnAreaColor.g, spawnAreaColor.b, 0.1f);
                        Gizmos.DrawSphere(spawnPoint.position, spawnAreaRadius);
                        Gizmos.color = spawnAreaColor;
                    }
                }
            }
        }
    }
}
