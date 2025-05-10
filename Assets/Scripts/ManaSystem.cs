using System.Collections;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using TMPro;

public class ManaSystem : MonoBehaviour, IDamageable
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
        
        if (currentMana < 1)
        {
            currentMana = 0;
            OnManaDepleted?.Invoke();
            StartCoroutine(HandleDeath());
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

    public void TakeDamage(float amount)
    {
        Debug.Log("ManaSystem.TakeDamage() called.");
        if (isDead) return; // Evitar múltiples muertes seguidas

        currentMana -= amount;
        if (currentMana < 1)
        {
            currentMana = 0;
            OnManaDepleted?.Invoke();
            StartCoroutine(HandleDeath());
        }
        Debug.Log("Damage taken");
        OnManaChanged?.Invoke(currentMana / maxMana);
        lastManaRatio = currentMana / maxMana;
    }
    public float GetManaRatio() => currentMana / maxMana;

    public float GetCurrentHealth()
    {
        return ((IDamageable)instance).GetCurrentHealth();
    }

    public float GetMaxHealth()
    {
        return ((IDamageable)instance).GetMaxHealth();
    }

    public bool IsDead()
    {
        return ((IDamageable)instance).IsDead();
    }
}