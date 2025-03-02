using UnityEngine;
using System;

public class ManaSystem : MonoBehaviour
{
    public static ManaSystem instance { get; private set; }
    public event Action<float> OnManaChanged;
    public event Action OnManaDepleted;

    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float regenRate = 5f; // Mana per second
    [SerializeField] private float currentMana;

    // Optional: Add this if you want to control when regeneration happens
    [SerializeField] private bool shouldRegenerate = true;

    private float lastManaRatio = -1f; // Used to only invoke events when the value actually changes

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
        if (shouldRegenerate && currentMana < maxMana)
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
        currentMana -= amount;
        if (currentMana <= 0)
        {
            currentMana = 0;
            OnManaDepleted?.Invoke();
        }
        OnManaChanged?.Invoke(currentMana / maxMana);
        lastManaRatio = currentMana / maxMana;
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

    // Remove the RegenerateMana method as we're now using Update()

    public float GetManaRatio() => currentMana / maxMana;

    // Optional: Methods to enable/disable regeneration
    public void EnableRegeneration() => shouldRegenerate = true;
    public void DisableRegeneration() => shouldRegenerate = false;
}