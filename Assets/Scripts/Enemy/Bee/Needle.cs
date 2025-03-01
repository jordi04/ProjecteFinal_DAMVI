using StarterAssets;
using UnityEngine;

public class Needle : MonoBehaviour
{
    [SerializeField] float damage = 100;

    private void OnTriggerEnter(Collider other)
    {
        FirstPersonController player = other.GetComponent<FirstPersonController>();

        if (player)
        {
            ManaSystem.Instance.TakeDamage(damage);
            Destroy(gameObject);
        }        
    }
}
