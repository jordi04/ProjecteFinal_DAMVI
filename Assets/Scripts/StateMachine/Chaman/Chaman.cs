using UnityEngine;
using UnityEngine.AI;

public class Chaman : MonoBehaviour
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator animator;
    Transform playerTransform;

    [Header("Detection Settings")]
    [SerializeField] float playerDetectionRange = 10f;
    [SerializeField] float runAwayDistance = 8f;
    [SerializeField] float spellCastCooldown = 5f;
    [SerializeField] float wanderRadius = 5f;

    [Header("Spell Settings")]
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] float spawnRadius = 3f;
    [SerializeField] private int remainingSpawns = 5;
    [SerializeField] float enemyStrengthMultiplier = 1.3f;


    [Header("Debug")]
    [SerializeField] bool debugStateSwitching = true;
    [SerializeField] bool forceSpellCast = false;

    StateMachine stateMachine;

    // States
    private ChamanWanderState wanderState;
    private ChamanRunAwayState runAwayState;
    private ChamanCastSpellState castSpellState;

    private void OnValidate()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        playerTransform = GameManager.instance.player.transform;
        SetupStateMachine();
    }

    private void SetupStateMachine()
    {
        stateMachine = new StateMachine();

        wanderState = new ChamanWanderState(this, animator, agent, wanderRadius);
        runAwayState = new ChamanRunAwayState(this, animator, agent, playerTransform, runAwayDistance, playerDetectionRange);
        castSpellState = new ChamanCastSpellState(this, animator, agent, playerTransform, spellCastCooldown, enemyPrefab, spawnRadius, remainingSpawns, enemyStrengthMultiplier);


        // Wander to RunAway: when player is detected
        At(wanderState, runAwayState, new FuncPredicate(() =>
        {
            bool transition = runAwayState.IsPlayerInRange();
            if (transition && debugStateSwitching) Debug.Log("TRANSITION: Wander -> RunAway");
            return transition;
        }));

        // RunAway to Wander: when player is out of range
        At(runAwayState, wanderState, new FuncPredicate(() =>
        {
            bool transition = runAwayState.IsPlayerOutOfRange();
            if (transition && debugStateSwitching) Debug.Log("TRANSITION: RunAway -> Wander");
            return transition;
        }));

        // RunAway to CastSpell: when spell is ready and player is in range
        At(runAwayState, castSpellState, new FuncPredicate(() =>
        {
            bool spellReady = castSpellState.IsSpellReady();
            bool playerInRange = runAwayState.IsPlayerInRange();
            bool transition = (spellReady && playerInRange) || forceSpellCast;

            if (debugStateSwitching)
            {
                if (spellReady) Debug.Log("Spell is ready");
                if (playerInRange) Debug.Log("Player is in range");
                if (transition) Debug.Log("TRANSITION: RunAway -> CastSpell");
            }

            return transition;
        }));

        // CastSpell to RunAway: after spell is cast
        At(castSpellState, runAwayState, new FuncPredicate(() =>
        {
            bool transition = !castSpellState.IsCasting();
            if (transition && debugStateSwitching) Debug.Log("TRANSITION: CastSpell -> RunAway");
            return transition;
        }));

        // Set initial state
        Debug.Log("Setting initial state to Wander");
        stateMachine.SetState(wanderState);
    }

    void At(Istate from, Istate to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
    void Any(Istate to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);

    void Update()
    {
        // Debug: Force spell cast if requested
        if (forceSpellCast)
        {
            Debug.Log("Forcing transition to CastSpell state");
            stateMachine.SetState(castSpellState);
            forceSpellCast = false;
        }

        stateMachine.Update();
    }

    private void FixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    // Draw gizmos to visualize detection range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}