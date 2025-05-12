using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private CheckPointSO checkPointData;
    [SerializeField] private float interactionDistance = 4f;

    [Header("Turn off")]
    [SerializeField] private GameObject hud;

    [Header("UI Interact")]
    [SerializeField] private GameObject interactUI;

    [Header("UI Main")]
    [SerializeField] private GameObject main_Panel;
    [SerializeField] private GameObject travel_Panel;
    [SerializeField] private TextMeshProUGUI checkPointName;

    [Header("Checkpoints List")]
    [SerializeField] private Transform checkpointListParent;
    [SerializeField] private GameObject checkpointButtonPrefab;
    [SerializeField] private Image selectedCheckpointImage;
    [SerializeField] private Color visitedColor = Color.white;
    [SerializeField] private Color unvisitedColor = Color.gray;

    [Header("VFX_Fire")]
    [SerializeField] private GameObject fireVFX;

    [Header("Playable Director")]
    [SerializeField] private PlayableDirector playableDirector;

    private bool isPlayerInRange = false;
    private bool hideInteractUI = false;

    private void Start()
    {
        checkPointData.checkPointTransform = spawnPoint;
        fireVFX.SetActive(checkPointData.isVisited);
    }

    private void Update()
    {
        // Player interaction check
        if (Physics.Raycast(GameManager.instance.mainCamera.transform.position, GameManager.instance.mainCamera.transform.forward, out RaycastHit hit, interactionDistance) && !hideInteractUI)
        {
            if (hit.collider.gameObject == gameObject)
            {
                isPlayerInRange = true;
                interactUI.SetActive(true);
                if (UserInput.instance.interactPressed && UserInput.instance.IsInGameMode)
                {
                    OpenCheckpointUI();
                }
            }
            else
            {
                isPlayerInRange = false;
                interactUI.SetActive(false);
            }
        }
        else
        {
            isPlayerInRange = false;
            interactUI.SetActive(false);
        }

        // Handle UI navigation with Escape key
        if (UserInput.instance.pauseMenuPressed)
        {
            if (travel_Panel.activeSelf)
            {
                // If in travel panel, go back to main panel
                travel_Panel.SetActive(false);
                main_Panel.SetActive(true);
            }
            else if (main_Panel.activeSelf)
            {
                // If in main panel, close everything
                CloseCheckpointUI();
            }
        }
    }

    private void OpenCheckpointUI()
    {
        if (!checkPointData.isVisited)
        {
            // Don't pause the game yet for first-time interaction
            StartCoroutine(PlayFirstTimeInteraction());
        }
        else
        {
            // For subsequent interactions, show UI immediately
            ShowCheckpointUI();
        }
    }

    private IEnumerator PlayFirstTimeInteraction()
    {
        hideInteractUI = true;
        // Deactivate game HUD
        hud.SetActive(false);
        // Deactivate the bonfire HUD that says E to interact
        interactUI.SetActive(false);
        UserInput.instance.switchActionMap(UserInput.ActionMap.InCinematic);

        // Play the cinematic
        playableDirector.Play();

        // Wait for the cinematic to finish
        yield return new WaitForSeconds((float)playableDirector.duration);

        // Mark checkpoint as visited after cinematic
        VisitCheckpoint();

        // Now show the UI
        ShowCheckpointUI();
    }

    private void ShowCheckpointUI()
    {
        hideInteractUI = true;

        // Make sure travel panel is closed
        travel_Panel.SetActive(false);

        // Open main panel
        main_Panel.SetActive(true);
        interactUI.SetActive(false);
        hud.SetActive(false);

        PauseMenu.otherMenuOpen = true;
        UserInput.instance.switchActionMap(UserInput.ActionMap.InMenu);
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Update the checkpoint name text
        if (checkPointName != null)
        {
            checkPointName.text = checkPointData.checkpointName;
        }
    }

    // Public method to connect to UI buttons
    public void ShowCheckpoints()
    {
        SetUpCheckpointList();
    }

    private void CloseCheckpointUI()
    {
        // Close all UI panels
        main_Panel.SetActive(false);
        travel_Panel.SetActive(false);

        // Restore game state
        hud.SetActive(true);
        PauseMenu.otherMenuOpen = false;
        UserInput.instance.switchActionMap(UserInput.ActionMap.InGame);
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        hideInteractUI = false;
    }

    private void VisitCheckpoint()
    {
        checkPointData.isVisited = true;
        fireVFX.SetActive(true);
        SetUpCheckpointList();
    }

    // Called when a button is pressed to open the travel panel
    public void OpenTravelUI()
    {
        Debug.Log("OpenTravelUI called");
        main_Panel.SetActive(false);
        travel_Panel.SetActive(true);
        SetUpCheckpointList();
    }

    // Go back to main menu from travel UI
    public void BackToMainMenu()
    {
        travel_Panel.SetActive(false);
        main_Panel.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;
    }

    private void SetUpCheckpointList()
    {
        // Clear existing buttons
        foreach (Transform child in checkpointListParent)
        {
            Destroy(child.gameObject);
        }

        // Get all checkpoints
        List<CheckPointSO> allCheckpoints = CheckPointManager.instance.GetAllCheckpoints();

        // Sort checkpoints by order
        allCheckpoints.Sort((a, b) => a.order.CompareTo(b.order));

        // Create checkpoint buttons
        foreach (CheckPointSO checkpoint in allCheckpoints)
        {
            GameObject buttonObj = Instantiate(checkpointButtonPrefab, checkpointListParent);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            buttonText.text = checkpoint.checkpointName;
            buttonText.color = checkpoint.isVisited ? visitedColor : unvisitedColor;

            button.interactable = checkpoint.isVisited;
            button.onClick.AddListener(() => TravelToCheckpoint(checkpoint));

            // Add hover listener to show image
            EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { SelectCheckpoint(checkpoint); });
            trigger.triggers.Add(entry);

            // Add hover exit listener to hide image
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { HideCheckpointImage(); });
            trigger.triggers.Add(exitEntry);
        }
    }

    private void SelectCheckpoint(CheckPointSO checkpoint)
    {
        if (checkpoint.isVisited)
        {
            selectedCheckpointImage.sprite = checkpoint.checkPointImage;
            selectedCheckpointImage.gameObject.SetActive(true);
        }
        else
        {
            selectedCheckpointImage.gameObject.SetActive(false);
        }
    }

    private void HideCheckpointImage()
    {
        selectedCheckpointImage.gameObject.SetActive(false);
    }

    private void TravelToCheckpoint(CheckPointSO checkpoint)
    {
        if (checkpoint.isVisited)
        {
            CharacterController characterController = GameManager.instance.player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                GameManager.instance.player.transform.position = checkpoint.checkPointTransform.position;
                characterController.enabled = true;
            }
            else
            {
                GameManager.instance.player.transform.position = checkpoint.checkPointTransform.position;
            }

            CheckPointManager.instance.SetCurrentCheckpoint(checkpoint);

            // Close all UI
            CloseCheckpointUI();
        }
    }

    public void SetAsRespawn()
    {
        CheckPointManager.instance.SetCurrentCheckpoint(checkPointData);
    }

    public CheckPointSO GetCheckPointData()
    {
        return checkPointData;
    }
}