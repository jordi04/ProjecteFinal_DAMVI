using UnityEngine;
using UnityEngine.Animations;
using System.Collections;
using System.Collections.Generic;

public class BeeLifeController : MonoBehaviour
{
    [SerializeField] LookAtConstraint lookConstraint;
    [SerializeField] GameObject parent;
    [SerializeField] private float life = 100f;
    [SerializeField] private float flashDuration = 0.2f;

    private Transform player;
    private Renderer beeRenderer;
    private MaterialPropertyBlock materialPropertyBlock;
    private Color originalColor;
    private bool isDead;

    private void Start()
    {
        // Original start configuration
        if (player != null)
        {
            ConstraintSource source = new ConstraintSource();
            source.sourceTransform = player.transform;
            source.weight = 1;
            lookConstraint.AddSource(source);
            lookConstraint.constraintActive = true;
        }

        // New color setup
        beeRenderer = GetComponentInChildren<Renderer>();
        materialPropertyBlock = new MaterialPropertyBlock();
        originalColor = beeRenderer.material.color;
    }

    public void SetPlayer(Transform player)
    {
        this.player = player;

        // Set up constraint if Start has already been called
        if (lookConstraint != null && lookConstraint.sourceCount == 0)
        {
            ConstraintSource source = new ConstraintSource();
            source.sourceTransform = player.transform;
            source.weight = 1;
            lookConstraint.AddSource(source);
            lookConstraint.constraintActive = true;
        }
    }

    // New method to handle damage from fireball script directly
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        life -= damageAmount;
        // Flash on every hit
        StartCoroutine(DamageFlash());

        if (life <= 0)
        {
            isDead = true;
            StartCoroutine(DeathSequence());
        }
    }

    // Keep the original OnTriggerEnter for backward compatibility
    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("FireBall"))
        {
            // Try to get the damage amount from the fireball
            FireballPrefabScript fireball = other.GetComponent<FireballPrefabScript>();

            // If we can't get the fireball script, use default damage
            if (fireball == null)
            {
                life -= 10f;
            }

            // The actual damage is now handled by the TakeDamage method
            // which the fireball script will call directly

            // Flash on every hit
            StartCoroutine(DamageFlash());

            if (life <= 0)
            {
                isDead = true;
                StartCoroutine(DeathSequence());
            }
        }
    }

    private IEnumerator DamageFlash()
    {
        SetColor(Color.red);
        yield return new WaitForSeconds(flashDuration);
        if (!isDead) ResetColor();
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(flashDuration);
        Destroy(parent);
    }

    private void SetColor(Color color)
    {
        beeRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_BaseColor", color);
        beeRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void ResetColor()
    {
        SetColor(originalColor);
    }
}