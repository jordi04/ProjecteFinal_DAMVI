using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Image manaBar;

    private void OnEnable()
    {
        ManaSystem.instance.OnManaChanged += UpdateManaBar;
    }

    private void OnDisable()
    {
        if (ManaSystem.instance != null)
            ManaSystem.instance.OnManaChanged -= UpdateManaBar;
    }

    private void UpdateManaBar(float manaRatio)
    {
        if (manaBar != null)
            manaBar.fillAmount = manaRatio;
    }
}
