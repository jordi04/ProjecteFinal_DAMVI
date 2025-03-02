using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballPrefabScript : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float explosionRadius = 3f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float effectDuration = 2f;

    [Header("Physics")]
    [SerializeField] private LayerMask enemyLayer;

    private bool hasExploded = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasExploded)
        {
            // Explode on contact with any object
            Explode();
        }
    }
    
    private void Explode()
    {
        hasExploded = true;

        // Create explosion visual effect
        if (explosionEffectPrefab != null)
        {
            GameObject explosionEffect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(explosionEffect, effectDuration);
        }

        // Apply area damage
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);
        foreach (Collider hitCollider in hitColliders)
        {
            // Check for bee enemy
            BeeLifeController beeLife = hitCollider.GetComponent<BeeLifeController>();
            if (beeLife != null)
            {
                // Call the TakeDamage method instead of relying on trigger
                beeLife.TakeDamage(damage);
            }
            //else () PER COMPROVAR ALTRES ENEMICS EN UN FUTUR
        }

        Destroy(gameObject);
    }

    // Visualization for debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}