using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] GameObject[] optionPanels;

    public void ShowPanel(GameObject targetPanel)
    {
        // Desactiva todos los paneles
        foreach (var panel in optionPanels)
        {
            panel.SetActive(false);
        }

        // Activa solo el panel objetivo
        targetPanel.SetActive(true);
    }
}
