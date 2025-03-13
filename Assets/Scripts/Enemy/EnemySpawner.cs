using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;
    public int enemiesPerSpawn = 3;
    public int maxEnemiesSpawnedAtTheSameTime = 10;
    public int maxEnemiesSpawnedTotal = 15;
    public float timeBetweenSpawnPointActivation = 5f;
    public float delayBetweenEnemiesSpawn = 0.5f;
    [SerializeField] Transform player;

    private int enemiesSpawned = 0;
    private List<GameObject> enemiesInScene = new List<GameObject>();
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

    [SerializeField] private Collider spawnerTrigger;
    [SerializeField] private Collider clearEnemiesTrigger;

    void Start()
    {
        // Make sure the colliders are set as triggers
        if (spawnerTrigger != null)
            spawnerTrigger.isTrigger = true;

        if (clearEnemiesTrigger != null)
            clearEnemiesTrigger.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject.transform == player)
        {
            if (Collider.Equals(spawnerTrigger, GetComponent<Collider>()))
            {
                // Player entered the spawn trigger
                StartSpawning();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject.transform == player)
        {
            if (Collider.Equals(spawnerTrigger, GetComponent<Collider>()))
            {
                // Player left the spawn trigger
                StopSpawning();
            }
            else if (Collider.Equals(clearEnemiesTrigger, GetComponent<Collider>()))
            {
                // Player left the clear enemies trigger
                ClearAllEnemies();
            }
        }
    }

    private void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            spawnCoroutine = StartCoroutine(SpawnEnemies());
        }
    }

    private void StopSpawning()
    {
        if (isSpawning)
        {
            isSpawning = false;
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

        // We don't decrease enemiesSpawned because we want to keep track
        // of the total enemies spawned, even if they were cleared
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

                    // Spawn the enemy
                    GameObject enemy = Instantiate(enemyPrefab, spawnPoints[i].position, spawnPoints[i].rotation);

                    // Add the enemy to our list and increment the count
                    enemiesInScene.Add(enemy);
                    enemiesSpawned++;

                    // Set up the enemy to notify us when it's eliminated
                    EnemyController enemyController = enemy.GetComponent<EnemyController>();
                    if (enemyController != null)
                    {
                        enemyController.spawner = this;
                    }

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
        }
    }

    public void EnemyEliminated(GameObject enemy)
    {
        if (enemiesInScene.Contains(enemy))
        {
            enemiesInScene.Remove(enemy);
        }
    }
}