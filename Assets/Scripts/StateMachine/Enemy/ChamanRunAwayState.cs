using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChamanRunAwayState : ChamanBaseState
{
    readonly NavMeshAgent agent;
    readonly float runAwayDistance;
    readonly float playerDetectionRange;
    readonly Transform playerTransform;
    readonly float updateDestinationInterval = 0.5f;

    float nextDestinationUpdate;

    public ChamanRunAwayState(Chaman chaman, Animator animator, NavMeshAgent agent, Transform playerTransform, float runAwayDistance, float playerDetectionRange)
        : base(chaman, animator)
    {
        this.agent = agent;
        this.playerTransform = playerTransform;
        this.runAwayDistance = runAwayDistance;
        this.playerDetectionRange = playerDetectionRange;
    }

    public override void OnEnter()
    {
        if (animator != null)
        {
            animator.CrossFade(RunHash, crossFadeDuration);
        }

        agent.speed *= 1.5f; // Run faster when fleeing
        agent.updateRotation = true;
        agent.isStopped = false;
        nextDestinationUpdate = 0f;
        UpdateFleeDestination();
    }

    public override void Update()
    {
        if (Time.time >= nextDestinationUpdate)
        {
            UpdateFleeDestination();
            nextDestinationUpdate = Time.time + updateDestinationInterval;
        }
    }

    public override void OnExit()
    {
        // Reset speed to original value
        agent.speed /= 1.5f;
    }

    private void UpdateFleeDestination()
    {
        if (playerTransform == null) return;

        // Direction away from player
        Vector3 directionAwayFromPlayer = chaman.transform.position - playerTransform.position;
        directionAwayFromPlayer.y = 0; // Keep on the same vertical level

        if (directionAwayFromPlayer.sqrMagnitude < 0.01f)
        {
            directionAwayFromPlayer = Random.insideUnitSphere;
            directionAwayFromPlayer.y = 0;
        }

        directionAwayFromPlayer = directionAwayFromPlayer.normalized * runAwayDistance;

        Vector3 targetPosition = chaman.transform.position + directionAwayFromPlayer;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, runAwayDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Try a random direction if we can't find a valid position
            Vector3 randomDirection = Random.insideUnitSphere * runAwayDistance;
            randomDirection.y = 0;

            if (NavMesh.SamplePosition(chaman.transform.position + randomDirection, out hit, runAwayDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;

        return Vector3.Distance(chaman.transform.position, playerTransform.position) <= playerDetectionRange;
    }

    public bool IsPlayerOutOfRange()
    {
        if (playerTransform == null) return true;

        // Use a slightly larger range to prevent rapid state switching
        float extendedRange = playerDetectionRange * 1.2f;
        return Vector3.Distance(chaman.transform.position, playerTransform.position) > extendedRange;
    }
}
