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

                if (remainingSpawns == 0)
                    ApplyStrengthMultiplier(enemy);
            }
        }
    }

    private void ApplyStrengthMultiplier(GameObject enemy)
    {
        enemy.transform.localScale *= Mathf.Sqrt(enemyStrengthMultiplier);

        Renderer renderer = enemy.GetComponentInChildren<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(1.0f, 0.6f, 0.6f);
    }

    public bool IsSpellReady() => Time.time - lastCastTime >= spellCooldown && remainingSpawns > 0;
    public bool IsCasting() => isCasting;
}
