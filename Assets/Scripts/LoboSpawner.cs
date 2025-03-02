using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class LoboSpawner : MonoBehaviour
{
    public GameObject loboPrefab;
    public Transform[] puntosSpawn;
    public int cantidadLobosPorSpawn = 3;
    public int maxLobosTotales = 10;
    public float tiempoEntreSpawns = 5f;
    public float delayEntreLobos = 0.5f; 

    private int lobosSpawneados = 0;
    private List<GameObject> lobosEnEscena = new List<GameObject>();

    void Start()
    {
        InvokeRepeating("IniciarSpawnLobos", 0f, tiempoEntreSpawns);
    }

    void IniciarSpawnLobos()
    {
        if (lobosSpawneados < maxLobosTotales)
        {
            StartCoroutine(SpawnLobosConDelay());
        }
        else
        {
            CancelInvoke("IniciarSpawnLobos"); 
        }
    }

    IEnumerator SpawnLobosConDelay()
    {
        foreach (Transform punto in puntosSpawn)
        {
            for (int i = 0; i < cantidadLobosPorSpawn; i++)
            {
                if (lobosSpawneados >= maxLobosTotales)
                {
                    yield break; 
                }

                NavMeshHit hit;
                if (NavMesh.SamplePosition(punto.position, out hit, 2f, NavMesh.AllAreas))
                {
                    GameObject nuevoLobo = Instantiate(loboPrefab, hit.position, Quaternion.identity);
                    lobosEnEscena.Add(nuevoLobo);
                    lobosSpawneados++;
                }
                else
                {
                    Debug.LogWarning("No se encontró un NavMesh cercano para spawnear un lobo en " + punto.position);
                }

                yield return new WaitForSeconds(delayEntreLobos); 
            }
        }
    }

    public void LoboEliminado(GameObject lobo)
    {
        if (lobosEnEscena.Contains(lobo))
        {
            lobosEnEscena.Remove(lobo);
            Destroy(lobo);
        }
    }
}
