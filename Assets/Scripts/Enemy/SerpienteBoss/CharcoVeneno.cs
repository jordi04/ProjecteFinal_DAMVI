using UnityEngine;
using System.Collections;

public class CharcoVeneno : MonoBehaviour
{
    public float duracionDPS; // Duración total del DPS tras pisar la zona
    public float danoPorSegundo; // Daño cada segundo

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ManaSystem.instance.ActivateDPS(duracionDPS, danoPorSegundo);
        }
    }
}
