using StarterAssets;
using UnityEngine;

public class Needle : MonoBehaviour
{
    [SerializeField] float damage;

    private void OnTriggerEnter(Collider other)
    {
        FirstPersonController player = other.GetComponent<FirstPersonController>();

        if (player)
        {
            ManaSystem.instance.TakeDamage(damage);
            Destroy(gameObject);
        }
        if(other.gameObject.CompareTag("Shield"))
            Destroy(gameObject);
    }
}
