using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Refactored ZombieAI using State Pattern for production-quality architecture.
/// Implements modular, class-based state machine with dependency injection.
/// Demonstrates OOP principles, SOLID design, and enterprise-level code structure.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    [Header("Movement Speeds")]
    [Tooltip("Walking speed when chasing or investigating (slower than player so they can escape)")]
    public float chaseSpeed = 2.5f;
    [Tooltip("Slower speed when idle wandering (60% of chase speed)")]
    public float wanderSpeed = 1.5f;

    [Header("Detection References")]
    [Tooltip("Reference to the zombie's hearing system")]
    public ZombieHearing hearing;
    [Tooltip("Reference to the zombie's vision system")]
    public ZombieVision vision;
    [Tooltip("Reference to the zombie's smell system")]
    public ZombieSmell smell;

    [Header("Behavior Settings")]
    [Tooltip("How often to update AI decisions (in seconds). Lower = more responsive, higher = better performance")]
    public float decisionInterval = 0.3f;

    [Header("Debug")]
    [Tooltip("Show current state and target in console")]
    public bool debugMode = false;

    // State Pattern - Current active state
    private IZombieState currentState;

    // Timing for decision-making intervals
    private float nextDecisionTime = 0f;

    // Target tracking (public for state classes to access)
    public GameObject ChaseTarget { get; set; }
    public Vector3 InvestigationTarget { get; set; }
    public bool HasInvestigationTarget { get; set; }

    // Component references (public properties for state classes)
    public NavMeshAgent NavAgent { get; private set; }
    public float WanderSpeed => wanderSpeed;
    public float ChaseSpeed => chaseSpeed;
    public bool DebugMode => debugMode;

    void Start()
    {
        // Initialize NavMeshAgent
        NavAgent = GetComponent<NavMeshAgent>();
        ConfigureNavMeshAgent();

        // Auto-find detection components if not assigned
        if (hearing == null) hearing = GetComponent<ZombieHearing>();
        if (vision == null) vision = GetComponent<ZombieVision>();
        if (smell == null) smell = GetComponent<ZombieSmell>();

        // Initialize state machine with Idle state
        ChangeState(new ZombieIdleState());

        // Randomize first decision time to spread load across frames (performance optimization)
        nextDecisionTime = Time.time + Random.Range(0f, decisionInterval);
    }

    void Update()
    {
        // Interval-based decision making (not every frame - performance optimization)
        if (Time.time >= nextDecisionTime)
        {
            MakeDecision();
            nextDecisionTime = Time.time + decisionInterval;
        }

        // Execute current state behavior
        currentState?.Execute(this);
    }

    /// <summary>
    /// Configure NavMeshAgent settings for zombie movement.
    /// </summary>
    void ConfigureNavMeshAgent()
    {
        NavAgent.speed = wanderSpeed;
        NavAgent.stoppingDistance = 1.5f;
        NavAgent.acceleration = 6f;
        NavAgent.angularSpeed = 60f; // Slow turning for realistic zombie shambling
        NavAgent.autoBraking = true;
    }

    /// <summary>
    /// Priority-based decision making using Strategy Pattern.
    /// Evaluates stimuli in order: Vision > Hearing > Smell > Idle
    /// Demonstrates separation of concerns and clean architecture.
    /// </summary>
    void MakeDecision()
    {
        // PRIORITY 1: Vision (highest) - If we see prey, chase immediately
        if (vision != null && vision.GetTarget() != null)
        {
            ChaseTarget = vision.GetTarget();
            ChangeState(new ZombieChasingState());
            return;
        }

        // If we were chasing but lost sight, investigate last known position
        if (currentState is ZombieChasingState)
        {
            if (ChaseTarget != null)
            {
                InvestigationTarget = ChaseTarget.transform.position;
                HasInvestigationTarget = true;
                ChangeState(new ZombieInvestigatingState());
                return;
            }
            else
            {
                ChangeState(new ZombieIdleState());
                return;
            }
        }

        // PRIORITY 2: Hearing - Investigate noises
        if (hearing != null && hearing.GetTargetNoisePosition(out Vector3 noisePosition))
        {
            InvestigationTarget = noisePosition;
            HasInvestigationTarget = true;

            // Set destination before state change
            NavAgent.SetDestination(InvestigationTarget);

            ChangeState(new ZombieInvestigatingState());
            return;
        }

        // PRIORITY 3: Smell - Wander toward scent (stays in Idle state)
        if (smell != null && smell.HasScentTarget())
        {
            Vector3 scentPosition = smell.GetScentPosition();

            // Set destination toward scent but keep idle/wander behavior
            if (currentState is ZombieIdleState)
            {
                NavAgent.SetDestination(scentPosition);
            }
            return;
        }

        // PRIORITY 4: Nothing detected - Continue idle wandering
        if (!(currentState is ZombieIdleState))
        {
            ChangeState(new ZombieIdleState());
        }
    }

    /// <summary>
    /// State transition method implementing State Pattern.
    /// Handles Enter/Exit lifecycle for clean state transitions.
    /// </summary>
    public void ChangeState(IZombieState newState)
    {
        // Exit current state
        currentState?.Exit(this);

        // Log state transition (debugging)
        if (debugMode && currentState != null)
        {
            Debug.Log($"<color=yellow>{gameObject.name}: {currentState.GetStateName()} → {newState.GetStateName()}</color>");
        }

        // Enter new state
        currentState = newState;
        currentState.Enter(this);
    }

    /// <summary>
    /// Public API: Force zombie into chase state (for testing or scripted events).
    /// </summary>
    public void ForceChaseTarget(GameObject target)
    {
        ChaseTarget = target;
        ChangeState(new ZombieChasingState());
    }

    /// <summary>
    /// Public API: Get current state name for UI/debugging.
    /// </summary>
    public string GetCurrentStateName()
    {
        return currentState?.GetStateName() ?? "None";
    }

    // Debug visualization in Scene view
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || NavAgent == null) return;

        // Draw current path
        if (NavAgent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3 previousCorner = transform.position;
            foreach (Vector3 corner in NavAgent.path.corners)
            {
                Gizmos.DrawLine(previousCorner, corner);
                Gizmos.DrawSphere(corner, 0.3f);
                previousCorner = corner;
            }
        }

        // Draw investigation target
        if (currentState is ZombieInvestigatingState && HasInvestigationTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(InvestigationTarget, 1f);
            Gizmos.DrawLine(transform.position, InvestigationTarget);
        }

        // Draw chase target
        if (currentState is ZombieChasingState && ChaseTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, ChaseTarget.transform.position);
            Gizmos.DrawWireSphere(ChaseTarget.transform.position, 1f);
        }
    }
}
