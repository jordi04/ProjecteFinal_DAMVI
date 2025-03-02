using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class BeeSpawner : MonoBehaviour
{
    public GameObject beePrefab;
    public Transform[] spawnPoints;
    public int maxBeesQuantities = 3;
    public int maxTotalBees = 10;
    public float spawnInterval = 5f;
    public float delay = 0.5f;

    [SerializeField] Transform player;

    private int spawnedBees = 0;
    private List<GameObject> beesInScene = new List<GameObject>();

    void Start()
    {
        InvokeRepeating("StartSpawnBees", 0f, spawnInterval);
    }

    void StartSpawnBees()
    {
        if (spawnedBees < maxTotalBees)
        {
            StartCoroutine(SpawnBeesWithDelay());
        }
        else
        {
            CancelInvoke("StartSpawnBees");
        }
    }

    IEnumerator SpawnBeesWithDelay()
    {
        foreach (Transform punto in spawnPoints)
        {
            for (int i = 0; i < maxBeesQuantities; i++)
            {
                if (spawnedBees >= maxTotalBees)
                {
                    yield break;
                }

                NavMeshHit hit;
                if (NavMesh.SamplePosition(punto.position, out hit, 2f, NavMesh.AllAreas))
                {
                    GameObject newBee = Instantiate(beePrefab, hit.position, Quaternion.identity);
                    newBee.GetComponent<BeeMovement>().SetPlayer(player);
                    beesInScene.Add(newBee);
                    spawnedBees++;
                }
                else
                {
                    Debug.LogWarning("No se encontró un NavMesh cercano para spawnear un lobo en " + punto.position);
                }

                yield return new WaitForSeconds(delay);
            }
        }
    }

    public void BeeEliminated(GameObject bee)
    {
        if (beesInScene.Contains(bee))
        {
            beesInScene.Remove(bee);
            Destroy(bee);
        }
    }
}
