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
    public float delay = 1f;

    [SerializeField] Transform player;

    private List<GameObject> beesInScene = new List<GameObject>();

    void Start()
    {
        InvokeRepeating("StartSpawnBees", 0f, spawnInterval);
    }

    void StartSpawnBees()
    {
        if (beesInScene.Count < maxTotalBees)
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
                if (beesInScene.Count >= maxTotalBees)
                {
                    yield break;
                }

                NavMeshHit hit;
                if (NavMesh.SamplePosition(punto.position, out hit, 2f, NavMesh.AllAreas))
                {
                    GameObject newBee = Instantiate(beePrefab, hit.position, Quaternion.identity);
                    newBee.GetComponent<BeeMovement>().SetPlayer(player);
                    newBee.GetComponent<BeeMovement>().spawner = this;
                    beesInScene.Add(newBee);
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
        }
    }
}
