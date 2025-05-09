using UnityEngine;

[System.Serializable]
public class PopUpSettings
{
    [Header("Content")]
    public string message = "This is a popup message!";

    [Header("Behavior")]
    public bool pauseGameOnShow = false;
    public bool autoHide = true;
    public float autoHideDuration = 5f;
    public float minTimeBeforeSkip = 0.5f;


}
