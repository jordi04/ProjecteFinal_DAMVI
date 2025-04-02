using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class ToggleKinematicBehaviour : MonoBehaviour
{
    [Header("Objects")]
    public GameObject targetObject;
    public GameObject spellPrefab;
    public GameObject nucleo;

    [Header("Spell Spawn Settings")]
    public Transform spellSpawnPoint;
    public float spellSpeed = 10f;

    private List<Rigidbody> rigidbodies = new List<Rigidbody>();
    private Dictionary<Rigidbody, bool> originalStates = new Dictionary<Rigidbody, bool>();

    private GameObject currentSpell;
    private bool spellActive = false;
    public void Awake()
    {
        if (targetObject == null) return;

        rigidbodies.AddRange(targetObject.GetComponentsInChildren<Rigidbody>());

        foreach (var rb in rigidbodies)
        {
            originalStates[rb] = rb.isKinematic;
        }
    }

    private void Update()
    {
        if (spellActive && currentSpell != null)
        {
            Vector3 spellPos = currentSpell.transform.position;
            Vector3 targetPos = targetObject.transform.position;

            // Calculate distance to target
            float distance = Vector3.Distance(spellPos, targetPos);

            // Define maximum scaling factor and threshold distance
            float maxScale = 7f; // Maximum scale multiplier
            float startScalingDistance = 20f; // Distance at which scaling begins

            if (distance <= startScalingDistance)
            {
                // Exponential scaling based on proximity
                float normalizedDistance = Mathf.Clamp01(distance / startScalingDistance); // Normalize distance to [0, 1]
                float scaleFactor = Mathf.Lerp(maxScale, 1f, Mathf.Pow(normalizedDistance, 2)); // Exponential scaling

                currentSpell.transform.localScale = Vector3.one * scaleFactor;
            }

            // Stop the spell when close enough
            float stopThreshold = 0.5f; // Threshold for stopping the spell
            if (distance < stopThreshold)
            {
                Rigidbody spellRb = currentSpell.GetComponent<Rigidbody>();
                if (spellRb != null)
                {
                    spellRb.velocity = Vector3.zero;
                    spellRb.angularVelocity = Vector3.zero;
                    spellRb.isKinematic = true;
                }

                // Trigger destruction sequence
                StartCoroutine(DestroySpell());
                spellActive = false;
            }
        }
    }

    private IEnumerator DestroySpell()
    {
        yield return new WaitForSeconds(0.5f); // Delay before destruction

        if (currentSpell != null)
        {
            Destroy(currentSpell);
        }
    }



    public void LaunchSpell()
    {
        if (spellPrefab == null || spellSpawnPoint == null) return;

        // Set spell target position slightly offset from targetObject
        Vector3 targetPosition = targetObject.transform.position;

        // Instantiate spell and set its direction
        GameObject spellInstance = Instantiate(spellPrefab, spellSpawnPoint.position, Quaternion.identity);
        currentSpell = spellInstance;

        // Ensure it has a Rigidbody
        Rigidbody spellRb = spellInstance.GetComponent<Rigidbody>();
        if (spellRb != null)
        {
            Vector3 direction = (targetPosition - spellSpawnPoint.position).normalized;
            spellRb.velocity = direction * spellSpeed;
        }
        spellActive = true;

    }

    public void activarNucleo()
    {
        nucleo.SetActive(true);
    }

    public void PlayBlackHole()
    {
        foreach (var rb in rigidbodies)
        {
            if (rb != null) rb.isKinematic = false;
        }

        foreach (var rb in rigidbodies)
        {
            if (rb != null && originalStates.ContainsKey(rb))
                rb.isKinematic = originalStates[rb];
        }

        if (targetObject != null)
        {
            BlackHoleEffect shrinkScript = targetObject.GetComponent<BlackHoleEffect>();
            if (shrinkScript == null)
                shrinkScript = targetObject.AddComponent<BlackHoleEffect>();

            shrinkScript.StartBlackHoleEffect();
        }
    }
}
