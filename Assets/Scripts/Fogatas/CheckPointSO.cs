using UnityEngine;

[CreateAssetMenu(fileName = "Checkpoint", menuName = "Checkpoint Data", order = 1)]

public class CheckPointSO : ScriptableObject
{
    [Header("!Obligatorio llenar!")]
    public string checkpointName;
    public Sprite checkPointImage;

    [Header("!Prohibido llenar!")]
    public bool isVisited;
    public Transform checkPointTransform;
}
