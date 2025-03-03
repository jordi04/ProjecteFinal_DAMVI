using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearCheckpoints();
    }


    public void ClearCheckpoints()
    {
        checkPoints.Clear();
        currentCheckpoint = null;
    }

    public void RegisterCheckPoint(CheckPoint checkPoint)
    {
        CheckPointSO newCheckPointData = checkPoint.GetCheckPointData();

        checkPoints.Add(checkPoint);
        checkPoints.Sort((a, b) => a.GetCheckPointData().order.CompareTo(b.GetCheckPointData().order));

        if (newCheckPointData.checkpointName == firstCheckPointName)
        {
            newCheckPointData.isVisited = true;
            SetCurrentCheckpoint(newCheckPointData);
        }
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
