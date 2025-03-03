using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private CheckPointSO checkPointData;
    [SerializeField] private float interactionDistance = 4f;

    //If will not be opened instantly
    //public float holdTime = 1f;

    [Header("Turn of")]
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject hud;

    [Header("UI Interact")]
    [SerializeField] private GameObject interactUI;

    [Header("UI Main")]
    [SerializeField] private GameObject main_Panel;
    [SerializeField] private TextMeshProUGUI checkPointName;

    [Header("UI Travel")]
    [SerializeField] private GameObject travel_Panel;
    [SerializeField] private Transform mapListParent;
    [SerializeField] private Transform checkpointListParent;
    [SerializeField] private GameObject mapButtonPrefab;
    [SerializeField] private GameObject checkpointButtonPrefab;
    [SerializeField] private Image selectedCheckpointImage;
    [SerializeField] private Color visitedColor = Color.white;
    [SerializeField] private Color unvisitedColor = Color.gray;
    private Dictionary<Map, List<CheckPointSO>> checkpointsByMap = new Dictionary<Map, List<CheckPointSO>>();

    [Header("UI Upgrades")]
    [SerializeField] private GameObject upgrades_Panel;

    [Header("VFX_Fire")]
    [SerializeField] private GameObject fireVFX;


    private bool isPlayerInRange = false;

    private void Start()
    {
        checkPointData.checkPointTransform = spawnPoint;
        CheckPointManager.instance.RegisterCheckPoint(this);

        checkPointName.text = checkPointData.checkpointName;
        fireVFX.SetActive(checkPointData.isVisited);
    }



    private void Update()
    {
        if (Physics.Raycast(GameManager.instance.mainCamera.transform.position, GameManager.instance.mainCamera.transform.forward, out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.gameObject == gameObject)
            {
                isPlayerInRange = true;
                interactUI.SetActive(true);

                if (UserInput.instance.interactPressed && UserInput.instance.IsInGameMode)
                {
                    OpenCheckpointUI();
                    Debug.Log("OpenCheckpointUI");
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

        if (UserInput.instance.pauseMenuPressed && main_Panel.activeSelf)
        {
            CloseCheckpointUI();
        }
        else if (UserInput.instance.pauseMenuPressed && travel_Panel.activeSelf)
        {
            travel_Panel.SetActive(false);
            main_Panel.SetActive(true);
        }
        else if (UserInput.instance.pauseMenuPressed && upgrades_Panel.activeSelf)
        {
            upgrades_Panel.SetActive(false);
            main_Panel.SetActive(true);
        }
    }

    private void OpenCheckpointUI()
    {
        VisitCheckpoint();
        main_Panel.SetActive(true);

        interactUI.SetActive(false);
        hud.SetActive(false);
        pauseMenu.otherMenuOpen = true;
        UserInput.instance.switchActionMap(UserInput.ActionMap.InMenu);
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseCheckpointUI()
    {
        main_Panel.SetActive(false);

        hud.SetActive(true);
        pauseMenu.otherMenuOpen = false;
        UserInput.instance.switchActionMap(UserInput.ActionMap.InGame);
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenTravelUI()
    {
        main_Panel.SetActive(false);
        travel_Panel.SetActive(true);
    }

    private void OpenUpgradesUI()
    {
        main_Panel.SetActive(false);
        upgrades_Panel.SetActive(true);
    }

    private void VisitCheckpoint()
    {
        if (!checkPointData.isVisited)
        {
            checkPointData.isVisited = true;
            fireVFX.SetActive(true);
        }
            setUpTravelUI();
    }
    private void setUpTravelUI()
    {
        // Clear existing buttons
        foreach (Transform child in mapListParent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in checkpointListParent)
        {
            Destroy(child.gameObject);
        }

        // Get all checkpoints and organize them by map
        List<CheckPointSO> allCheckpoints = CheckPointManager.instance.GetAllCheckpoints();
        checkpointsByMap.Clear();
        foreach (CheckPointSO checkpoint in allCheckpoints)
        {
            if (!checkpointsByMap.ContainsKey(checkpoint.map))
            {
                checkpointsByMap[checkpoint.map] = new List<CheckPointSO>();
            }
            checkpointsByMap[checkpoint.map].Add(checkpoint);
        }

        // Create map buttons
        foreach (Map map in checkpointsByMap.Keys)
        {
            GameObject mapButtonObj = Instantiate(mapButtonPrefab, mapListParent);
            Button mapButton = mapButtonObj.GetComponent<Button>();
            TMPro.TextMeshProUGUI mapButtonText = mapButtonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            mapButtonText.text = map.ToString();
            mapButton.onClick.AddListener(() => ShowCheckpointsForMap(map));
        }

        // Show checkpoints for the first map by default
        if (checkpointsByMap.Keys.Count > 0)
        {
            ShowCheckpointsForMap(checkpointsByMap.Keys.First());
        }
    }

    private void ShowCheckpointsForMap(Map map)
    {
        // Clear existing checkpoint buttons
        foreach (Transform child in checkpointListParent)
        {
            Destroy(child.gameObject);
        }

        // Create checkpoint buttons for the selected map
        foreach (CheckPointSO checkpoint in checkpointsByMap[map])
        {
            GameObject buttonObj = Instantiate(checkpointButtonPrefab, checkpointListParent);
            Button button = buttonObj.GetComponent<Button>();
            TMPro.TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

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

            // Close UI panels
            travel_Panel.SetActive(false);
            CloseCheckpointUI();
        }
    }

    public void SetAsRespawn()
    {
        //Animacio de descançar o algun feedback al jugador
        CheckPointManager.instance.SetCurrentCheckpoint(checkPointData);
    }

    public CheckPointSO GetCheckPointData()
    {
        return checkPointData;
    }
}


//TODO:Particulas paradas en el menu.
//TODO:El fuego se activa al salir del menu deveria ser al entrar
//TODO:Ordenar mapas y checkpoints por orden de aparicion
//TODO:Bloquear mapas no visitados