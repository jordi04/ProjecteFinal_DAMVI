using UnityEngine;

public class PopUpTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private float detectionInterval = 0.2f; // How often to check for player

    [Header("PopUp Configuration")]
    [SerializeField] private PopUpSettings popUpSettings = new PopUpSettings();

    [Header("Detection Box Settings")]
    [SerializeField] private Vector3 boxSize = Vector3.one;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] private bool drawSolidGizmo = false;

    private bool hasTriggered = false;
    private bool playerInArea = false;
    private float nextDetectionTime = 0f;

    private void Update()
    {
        // Check for player at intervals for better performance
        if (Time.time >= nextDetectionTime)
        {
            CheckForPlayerInArea();
            nextDetectionTime = Time.time + detectionInterval;
        }
    }

    private void CheckForPlayerInArea()
    {
        // Use OverlapBox to detect colliders inside the box area
        Collider[] colliders = Physics.OverlapBox(
            transform.position,
            boxSize / 2f, // Half extents
            transform.rotation);

        bool wasPlayerInArea = playerInArea;
        playerInArea = false;

        foreach (Collider col in colliders)
        {
            if (col.CompareTag(playerTag))
            {
                playerInArea = true;

                // If player just entered the area and hasn't triggered yet (or can trigger multiple times)
                if (!wasPlayerInArea && (!hasTriggered || !triggerOnce))
                {
                    TriggerPopUp();
                }

                break;
            }
        }
    }

    public void TriggerPopUp()
    {
        if (hasTriggered && triggerOnce) return;

        if (PopUpManager.Instance != null)
        {
            PopUpManager.Instance.ShowPopUp(popUpSettings);
            hasTriggered = true;
        }
        else
        {
            Debug.LogError("No PopUpManager instance found in the scene!");
        }
    }

    // Reset the trigger (useful for testing)
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        if (drawSolidGizmo)
        {
            Gizmos.DrawCube(Vector3.zero, boxSize);
        }
        else
        {
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
        }

        // Reset the matrix
        Gizmos.matrix = Matrix4x4.identity;
    }
}
