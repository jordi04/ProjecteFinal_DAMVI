using UnityEngine;
using UnityEngine.UI;
public class HUD : MonoBehaviour
{
    [SerializeField] private Image manaBar;

    private void Start()
    {
        // Subscribe to the event if the instance exists
        if (ManaSystem.instance != null)
        {
            SubscribeToEvents();
        }
    }

    private void OnEnable()
    {
        // Only try to subscribe if the instance already exists
        if (ManaSystem.instance != null)
        {
            SubscribeToEvents();
        }
    }

    private void OnDisable()
    {
        if (ManaSystem.instance != null)
            ManaSystem.instance.OnManaChanged -= UpdateManaBar;
    }

    private void SubscribeToEvents()
    {
        ManaSystem.instance.OnManaChanged += UpdateManaBar;
        // Initialize the bar with current mana value
        UpdateManaBar(ManaSystem.instance.GetManaRatio());
    }

    private void UpdateManaBar(float manaRatio)
    {
        if (manaBar != null)
            manaBar.fillAmount = manaRatio;
    }
}