using System;
using UnityEngine;
using UnityEngine.UI;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private CheckPointSO checkPointData;
    [SerializeField] private float interactionDistance = 4f;

    //If will not be opened instantly
    //public float holdTime = 1f;


    [Header("UI")]
    [SerializeField] private GameObject interactUI;
    [SerializeField] private GameObject checkPointUI;
    [SerializeField] private GameObject travelUI;

    [Tooltip("Button to set as SpawnPoint")]
    [SerializeField] private Button restButon;
    [Tooltip("Opens travel UI")]
    [SerializeField] private Button travelButton;

    private bool isPlayerInRange = false;

    private void Awake()
    {
        //restButon.onClick.AddListener(SetAsRespawn);
        //travelButton.onClick.AddListener(OpenTravelUI);
    }

    private void Start()
    {
        checkPointData.checkPointTransform = spawnPoint;
        CheckPointManager.instance.RegisterCheckPoint(this);
    }

    private void Update()
    {
        if (Physics.Raycast(GameManager.Instance.mainCamera.transform.position, GameManager.Instance.mainCamera.transform.forward, out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.gameObject == gameObject)
            {
                Debug.Log("Player in range");
                isPlayerInRange = true;
                interactUI.SetActive(true);

                if (UserInput.instance.interactPressed)
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
    }

    private void OpenCheckpointUI()
    {
        checkPointUI.SetActive(true);
    }

    private void OpenTravelUI()
    {
        travelUI.SetActive(true);
    }

    private void SetAsRespawn()
    {
        
    }

    public CheckPointSO GetCheckPointData()
    {
        return checkPointData;
    }
}
