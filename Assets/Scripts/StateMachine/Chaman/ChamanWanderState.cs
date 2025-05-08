using UnityEngine;
using UnityEngine.AI;

public class ChamanWanderState : ChamanBaseState
{
    readonly NavMeshAgent agent;
    readonly Vector3 startPoint;
    readonly float wanderRadius;
    readonly Chaman chaman;

    Vector3 currentDestination;
    bool destinationSet;

    public ChamanWanderState(Chaman chaman, Animator animator, NavMeshAgent agent, float wanderRadius) : base(chaman, animator)
    {
        this.chaman = chaman;
        this.agent = agent;
        this.startPoint = chaman.transform.position;
        this.wanderRadius = wanderRadius;
    }

    public override void OnEnter()
    {
        if (animator != null)
        {
            animator.CrossFade(RunHash, crossFadeDuration);
        }

        agent.updateRotation = false; // We will rotate manually
        agent.isStopped = true; // Wait until facing destination
        SetNewRandomDestination();
    }

    public override void Update()
    {
        RotateTowardsDestination();

        if (HasReachedDestination())
        {
            SetNewRandomDestination();
        }
    }

    private void SetNewRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += startPoint;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            currentDestination = hit.position;
            agent.SetDestination(currentDestination);
            destinationSet = true;
        }
    }

    private void RotateTowardsDestination()
    {
        if (!destinationSet) return;

        Vector3 direction = currentDestination - chaman.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        chaman.transform.rotation = Quaternion.Slerp(
            chaman.transform.rotation,
            targetRotation,
            Time.deltaTime * 5f
        );

        float angle = Quaternion.Angle(chaman.transform.rotation, targetRotation);
        if (angle < 5f)
        {
            agent.isStopped = false; // Start moving when mostly facing the target
        }
        else
        {
            agent.isStopped = true; // Keep rotating in place
        }
    }

    private bool HasReachedDestination()
    {
        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance &&
               (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
    }
}
