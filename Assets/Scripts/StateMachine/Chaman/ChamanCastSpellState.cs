using UnityEngine.AI;
using UnityEngine;

public class ChamanCastSpellState : ChamanBaseState
{
    readonly NavMeshAgent agent;
    readonly Transform playerTransform;

    readonly float castTime = 2.0f;
    readonly float spellCooldown;
    readonly GameObject enemyPrefab;
    readonly float spawnRadius;

    private int remainingSpawns;
    private float enemyStrengthMultiplier;
    private float currentMultiplier = 1.0f;

    float castStartTime;
    float lastCastTime;
    bool isCasting = false;

    public ChamanCastSpellState(
        Chaman chaman,
        Animator animator,
        NavMeshAgent agent,
        Transform playerTransform,
        float spellCooldown,
        GameObject enemyPrefab,
        float spawnRadius,
        int remainingSpawns,
        float enemyStrengthMultiplier)
        : base(chaman, animator)
    {
        this.agent = agent;
        this.playerTransform = playerTransform;
        this.spellCooldown = spellCooldown;
        this.enemyPrefab = enemyPrefab;
        this.spawnRadius = spawnRadius;
        this.remainingSpawns = remainingSpawns;
        this.enemyStrengthMultiplier = enemyStrengthMultiplier;

        lastCastTime = -spellCooldown;
    }

    public override void OnEnter()
    {
        if (animator != null)
            animator.CrossFade(RiseHandsHash, crossFadeDuration);

        agent.isStopped = true;

        if (playerTransform != null)
        {
            Vector3 dir = playerTransform.position - chaman.transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                chaman.transform.rotation = Quaternion.LookRotation(dir);
        }

        castStartTime = Time.time;
        isCasting = true;
    }

    public override void Update()
    {
        if (isCasting && Time.time - castStartTime >= castTime)
        {
            CastSpell();
            lastCastTime = Time.time;
            isCasting = false;
        }
    }

    public override void OnExit()
    {
        agent.isStopped = false;
        isCasting = false;
    }

    private void CastSpell()
    {
        if (playerTransform == null || remainingSpawns <= 0 || enemyPrefab == null)
            return;

        int enemiesToSpawn = remainingSpawns;
        remainingSpawns--;

        currentMultiplier *= enemyStrengthMultiplier;

        float angleStep = 360f / enemiesToSpawn;
        Vector3 forward = chaman.transform.forward;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Quaternion rotation = Quaternion.Euler(0, angleStep * i, 0) * Quaternion.LookRotation(forward);
            Vector3 spawnDir = rotation * Vector3.forward;
            Vector3 spawnPos = chaman.transform.position + spawnDir * spawnRadius;

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
            {
                GameObject enemy = Object.Instantiate(enemyPrefab, hit.position, rotation);
                ApplyStrengthMultiplier(enemy, currentMultiplier);
            }
        }
    }

    private void ApplyStrengthMultiplier(GameObject enemy, float multiplier)
    {
        // Escala
        Vector3 originalScale = enemyPrefab.transform.localScale;
        enemy.transform.localScale = new Vector3(
            originalScale.x * Mathf.Sqrt(multiplier),
            originalScale.y * Mathf.Sqrt(multiplier),
            originalScale.z * Mathf.Sqrt(multiplier)
        );

        // Color
        Renderer renderer = enemy.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Material newMat = new Material(renderer.material);
            newMat.color = Color.Lerp(
                Color.white,
                new Color(1f, 0.5f, 0.5f),
                Mathf.Clamp01((multiplier - 1f) * 0.5f)
            );
            renderer.material = newMat;
        }

        // Estadísticas
        EnemyController ec = enemy.GetComponent<EnemyController>();
        if (ec != null)
        {
            ec.SetDamageMultiplier(multiplier);
            ec.SetHealthMultiplier(multiplier);
            ec.SetSpeedMultiplier(Mathf.Sqrt(multiplier));

            Debug.Log($"Goblin mejorado - " +
                     $"Daño: {ec.GetAttackDamage()} (+{multiplier}x) | " +
                     $"Salud: {ec.GetMaxHealth()} | " +
                     $"Velocidad: {ec.GetSpeed()}");
        }
    }

    public bool IsSpellReady() => Time.time - lastCastTime >= spellCooldown && remainingSpawns > 0;
    public bool IsCasting() => isCasting;
}