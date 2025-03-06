using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    [Header("FirstCheckPoint")]
    [SerializeField] private string firstCheckPointName;
    public static CheckPointManager instance { get; private set; }

    [SerializeField]private List<CheckPointSO> checkPoints = new List<CheckPointSO>();
    public CheckPointSO currentCheckpoint { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void SetCurrentCheckpoint(CheckPointSO newCurrentCheckpoint)
    {
        currentCheckpoint = newCurrentCheckpoint;
        Debug.Log($"New current checkpoint: {currentCheckpoint.checkpointName}");
    }

    public List<CheckPointSO> GetAllCheckpoints()
    {
        return checkPoints.Select(cp => cp).ToList();
    }
    public List<CheckPointSO> GetUnvisitedCheckpoints()
    {
        return checkPoints.Where(cp => !cp.isVisited).Select(cp => cp).ToList();
    }
    public List<CheckPointSO> GetVisitedCheckpoints()
    {
        return checkPoints.Where(cp => cp.isVisited).Select(cp => cp).ToList();
    }
}


//Crear VAO
//Bindear VAO

//Crear VBO
//Bindear VBO