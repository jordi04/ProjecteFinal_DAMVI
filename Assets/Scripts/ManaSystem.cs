using System.Collections;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using TMPro;

public class ManaSystem : MonoBehaviour
{
    public static ManaSystem instance { get; private set; }
    public event Action<float> OnManaChanged;
    public event Action OnManaDepleted;

    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float regenRate = 5f; // Mana per second
    [SerializeField] private float currentMana;


    [Header("Respawn UI")]
    [SerializeField] private GameObject respawnUIPrefab;

    private GameObject respawnUIInstance;
    private TextMeshProUGUI loadingText;



    private float lastManaRatio = -1f;
    private bool isDead = false;

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
        currentMana = maxMana;
    }

    private void Update()
    {
        if (currentMana < maxMana)
        {
            // Smooth regeneration based on delta time
            currentMana = Mathf.Min(currentMana + (regenRate * Time.deltaTime), maxMana);

            // Only trigger the event if the value changed enough (prevents excessive updates)
            float currentRatio = currentMana / maxMana;
            if (Mathf.Abs(currentRatio - lastManaRatio) > 0.001f)
            {
                lastManaRatio = currentRatio;
                OnManaChanged?.Invoke(currentRatio);
            }
        }
    }


    public bool TryConsumeMana(float amount)
    {
        if (currentMana < amount) return false;
        currentMana -= amount;
        OnManaChanged?.Invoke(currentMana / maxMana);
        lastManaRatio = currentMana / maxMana;
        return true;
    }

    public bool HasManaAmount(float amount)
    {
        return currentMana >= amount;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return; // Evitar múltiples muertes seguidas

        currentMana -= amount;
        if (currentMana <= 0)
        {
            currentMana = 0;
            OnManaDepleted?.Invoke();
            StartCoroutine(HandleDeath());
        }
        OnManaChanged?.Invoke(currentMana / maxMana);
        lastManaRatio = currentMana / maxMana;
    }

    private IEnumerator HandleDeath()
    {
        isDead = true;
        Time.timeScale = 0; // Pause the game

        // Instantiate the respawn UI
        respawnUIInstance = Instantiate(respawnUIPrefab, Vector3.zero, Quaternion.identity);
        respawnUIInstance.transform.SetParent(Canvas.FindObjectOfType<Canvas>().transform, false);

        // Find the loading text component
        loadingText = respawnUIInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (loadingText == null)
        {
            Debug.LogError("Loading text not found in respawn UI prefab");
            yield break;
        }

        float elapsedTime = 0f;
        string[] loadingStates = { ".", "..", "..." };

        while (elapsedTime < 3f)
        {
            loadingText.text = "Respawning" + loadingStates[(int)(elapsedTime % loadingStates.Length)];
            yield return new WaitForSecondsRealtime(0.5f);
            elapsedTime += 0.5f;
        }

        RespawnPlayer();
    }


    private void RespawnPlayer()
    {
        if (respawnUIInstance != null)
        {
            Destroy(respawnUIInstance);
        }
        ResetMana();
        Time.timeScale = 1;

        //Tp to spawn point
        CharacterController characterController = GameManager.instance.player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            GameManager.instance.player.transform.position = CheckPointManager.instance.currentCheckpoint.checkPointTransform.position;
            characterController.enabled = true;
        }
        else
        {
            GameManager.instance.player.transform.position = CheckPointManager.instance.currentCheckpoint.checkPointTransform.position;
        }


        isDead = false;
    }

    public void ResetMana()
    {
        currentMana = maxMana;
        OnManaChanged?.Invoke(currentMana / maxMana);
        lastManaRatio = currentMana / maxMana;
    }

    public bool TryConsumeManaOverTime(float baseDrainRate, float exponentialFactor, float elapsedTime)
    {
        float drainAmount = baseDrainRate * Mathf.Pow(1 + exponentialFactor, elapsedTime);
        return TryConsumeMana(drainAmount * Time.deltaTime);
    }

    public float GetManaRatio() => currentMana / maxMana;
}