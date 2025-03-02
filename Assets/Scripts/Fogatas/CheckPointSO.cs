using UnityEngine;

public enum Map
{
    Overworld,
    Cave,
    Hell
}

[CreateAssetMenu(fileName = "Checkpoint", menuName = "Checkpoint Data", order = 1)]
public class CheckPointSO : ScriptableObject
{
    [Header("!Obligatorio llenar!")]
    public string checkpointName;
    public Sprite checkPointImage;
    public Map map;

    [Header("!Prohibido llenar!")]
    public bool isVisited;
    public Transform checkPointTransform;
}
