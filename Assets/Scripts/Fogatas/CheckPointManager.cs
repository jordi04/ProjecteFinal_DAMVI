using System.Collections.Generic;
using System.Linq;
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
        if (!checkPoints.Contains(checkPoint))
        {
            // Check if the order is already taken
            if (checkPoints.Exists(cp => cp.GetCheckPointData().order == checkPoint.GetCheckPointData().order))
            {
                Debug.LogWarning($"Checkpoint {checkPoint.GetCheckPointData().checkpointName} has a duplicate order {checkPoint.GetCheckPointData().order}. It will be added to the end.");
                checkPoint.GetCheckPointData().order = checkPoints.Count > 0 ? checkPoints.Max(cp => cp.GetCheckPointData().order) + 1 : 0;
            }

            checkPoints.Add(checkPoint);
            checkPoints.Sort((a, b) => a.GetCheckPointData().order.CompareTo(b.GetCheckPointData().order)); // Sort the list by order

            if (checkPoint.GetCheckPointData().checkpointName == firstCheckPointName)
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

    public List<CheckPointSO> GetAllCheckpoints()
    {
        return checkPoints.Select(cp => cp.GetCheckPointData()).ToList();
    }
    public List<CheckPointSO> GetUnvisitedCheckpoints()
    {
        return checkPoints.Where(cp => !cp.GetCheckPointData().isVisited).Select(cp => cp.GetCheckPointData()).ToList();
    }
    public List<CheckPointSO> GetVisitedCheckpoints()
    {
        return checkPoints.Where(cp => cp.GetCheckPointData().isVisited).Select(cp => cp.GetCheckPointData()).ToList();
    }
}
