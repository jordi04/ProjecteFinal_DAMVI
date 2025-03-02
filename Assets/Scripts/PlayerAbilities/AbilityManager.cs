using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    [Header("UI")]
    [SerializeField] private Image[] abilityIcons; // Array of icons for abilities (preconfigured in the UI)
    [SerializeField] private Image[] cooldownOverlays; // Visual cooldown representation

    private Dictionary<PlayerAbility, CoroutineInfo> cooldownCoroutines = new Dictionary<PlayerAbility, CoroutineInfo>();
    private Dictionary<AbilityType, int> abilityUIIndexMap = new Dictionary<AbilityType, int>();

    private class CoroutineInfo
    {
        public Coroutine routine;
        public float totalCooldown;
        public float remainingCooldown;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Map ability types to UI indices
        abilityUIIndexMap[AbilityType.Fireball] = 0;
        abilityUIIndexMap[AbilityType.Shield] = 1;
        abilityUIIndexMap[AbilityType.Swap] = 2;
    }

    public bool IsAbilityOnCooldown(PlayerAbility ability)
    {
        return cooldownCoroutines.ContainsKey(ability);
    }

    public void StartCooldown(PlayerAbility ability, float cooldownTime, AbilityType type)
    {
        if (cooldownCoroutines.ContainsKey(ability))
        {
            StopCoroutine(cooldownCoroutines[ability].routine);
        }

        CoroutineInfo info = new CoroutineInfo
        {
            totalCooldown = cooldownTime,
            remainingCooldown = cooldownTime
        };

        info.routine = StartCoroutine(CooldownRoutine(ability, info, type));
        cooldownCoroutines[ability] = info;
    }

    private IEnumerator CooldownRoutine(PlayerAbility ability, CoroutineInfo info, AbilityType type)
    {
        // Get UI index for this ability
        if (abilityUIIndexMap.TryGetValue(type, out int index))
        {
            // Initialize cooldown overlay if we have one
            if (index < cooldownOverlays.Length && cooldownOverlays[index] != null)
            {
                cooldownOverlays[index].fillAmount = 1f; // Start fully filled
                cooldownOverlays[index].gameObject.SetActive(true);
            }
        }

        while (info.remainingCooldown > 0)
        {
            // Update the cooldown fill amount
            if (abilityUIIndexMap.TryGetValue(type, out int uiIndex) &&
                uiIndex < cooldownOverlays.Length &&
                cooldownOverlays[uiIndex] != null)
            {
                // Calculate the fill amount based on remaining cooldown
                float fillAmount = info.remainingCooldown / info.totalCooldown;
                cooldownOverlays[uiIndex].fillAmount = fillAmount;
            }

            yield return null;
            info.remainingCooldown -= Time.deltaTime;
        }

        // Cooldown finished
        if (abilityUIIndexMap.TryGetValue(type, out int uiIdx))
        {
            // Hide cooldown overlay
            if (uiIdx < cooldownOverlays.Length && cooldownOverlays[uiIdx] != null)
            {
                cooldownOverlays[uiIdx].gameObject.SetActive(false);
            }
        }

        cooldownCoroutines.Remove(ability);
    }
}