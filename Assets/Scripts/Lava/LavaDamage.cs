using UnityEngine;

public class LavaDamage : MonoBehaviour
{
    private ManaSystem manaSystem;

    private void OnTriggerEnter(Collider collision)
    {
        manaSystem = ManaSystem.instance;
        if (collision.gameObject.CompareTag("Player"))
        {
            manaSystem.TakeDamage(100);
        }
    }
}
