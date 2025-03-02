using System.Collections.Generic;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    [Header("FirstCheckPoint")]
    [SerializeField] private string firstCheckPointName;
    public static CheckPointManager instance { get; private set; }

    private List<CheckPoint> checkPoints = new List<CheckPoint>();
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

    public void RegisterCheckPoint(CheckPoint checkPoint)
    {
        if(!checkPoints.Contains(checkPoint))
        {
            checkPoints.Add(checkPoint);
            if (checkPoint.GetCheckPointData().name == firstCheckPointName)
            {
                checkPoint.GetCheckPointData().isVisited = true;
                SetCurrentCheckpoint(checkPoint.GetCheckPointData());
            }
        }
            
    }

    public void UpdateCheckpointStatus(CheckPointSO updatedCheckPoint)
    {
        Debug.Log($"Checkpoint {updatedCheckPoint.checkpointName} visited.");
        // Add any additional logic for when a checkpoint is visited
    }

    public void SetCurrentCheckpoint(CheckPointSO newCurrentCheckpoint)
    {
        currentCheckpoint = newCurrentCheckpoint;
        Debug.Log($"New current checkpoint: {currentCheckpoint.checkpointName}");
    }

    public List<CheckPointSO> GetVisitedCheckpoints()
    {
        List<CheckPointSO> visitedCheckpoints = new List<CheckPointSO>();
        foreach (CheckPoint cp in checkPoints)
        {
            if (cp.GetCheckPointData().isVisited)
            {
                visitedCheckpoints.Add(cp.GetCheckPointData());
            }
        }
        return visitedCheckpoints;
    }
}
