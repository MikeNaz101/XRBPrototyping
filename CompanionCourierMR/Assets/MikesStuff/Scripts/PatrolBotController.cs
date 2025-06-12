using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Collections; // Required for Coroutines

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))] // Now requires an Animator component
public class PatrolBotController : MonoBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("The points this bot will move between. This is set by the spawner.")]
    public List<Vector3> patrolPoints;

    [Header("Detection Settings")]
    [Tooltip("How far the bot can see.")]
    public float viewRadius = 10f;
    [Tooltip("The angle of the bot's field of view (in degrees).")]
    [Range(0, 360)]
    public float viewAngle = 90f;
    [Tooltip("The layer mask for objects that can block the bot's line of sight (e.g., walls, furniture).")]
    public LayerMask obstacleMask;

    [Header("Targeting Settings")]
    [Tooltip("The layer for the player's companion sphere, used for detection.")]
    public LayerMask targetMask;
    [Tooltip("How close the bot needs to be to play its attack animation.")]
    public float attackRange = 1.5f;
    
    [Header("Death & Recovery")]
    [Tooltip("The vertical distance the bot must fall to trigger the 'death' state.")]
    public float fallHeightToDie = 1.0f;
    [Tooltip("How many seconds the bot stays in the 'collapse' state before recovering.")]
    public float recoveryTime = 5.0f;


    // --- Private Variables ---
    private NavMeshAgent agent;
    private Animator animator;
    private int currentPatrolIndex = 0;
    private Transform target; // The companion sphere
    
    // --- State Management ---
    private enum State { Patrolling, Chasing, Attacking, Dead }
    private State currentState;

    // --- Fall Detection ---
    private bool wasGrounded = true;
    private float fallStartYPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        currentState = State.Patrolling;
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex]);
        }
    }

    void Update()
    {
        // If the bot is in the Dead state, it does nothing until it recovers.
        if (currentState == State.Dead)
        {
            return;
        }

        HandleFallDetection();
        FindVisibleTarget();
        UpdateAnimations();

        switch (currentState)
        {
            case State.Patrolling:
                Patrol();
                break;
            case State.Chasing:
                Chase();
                break;
            case State.Attacking:
                Attack();
                break;
        }
    }

    void Patrol()
    {
        if (target != null)
        {
            currentState = State.Chasing;
            return;
        }
        
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            agent.SetDestination(patrolPoints[currentPatrolIndex]);
        }
    }

    void Chase()
    {
        if (target == null)
        {
            currentState = State.Patrolling;
            if(patrolPoints != null && patrolPoints.Count > 0)
            {
               agent.SetDestination(patrolPoints[currentPatrolIndex]);
            }
            return;
        }

        agent.SetDestination(target.position);
        
        // If we get in attack range, switch to attacking.
        if (Vector3.Distance(transform.position, target.position) <= attackRange)
        {
            currentState = State.Attacking;
        }
    }
    
    void Attack()
    {
        if (target == null)
        {
            currentState = State.Chasing;
            return;
        }

        agent.SetDestination(transform.position); // Stop moving to attack
        transform.LookAt(target);
        
        // Trigger the attack animation. The actual "damage" is the collision.
        animator.SetTrigger("attack");

        // If the target moves out of attack range, go back to chasing.
        if (Vector3.Distance(transform.position, target.position) > attackRange)
        {
            currentState = State.Chasing;
        }
    }

    void FindVisibleTarget()
    {
        target = null;
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        foreach (var targetCollider in targetsInViewRadius)
        {
            Transform potentialTarget = targetCollider.transform;
            Vector3 dirToTarget = (potentialTarget.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(transform.position, potentialTarget.position);
                if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    target = potentialTarget;
                    return;
                }
            }
        }
    }
    
    void UpdateAnimations()
    {
        // Calculate velocity relative to the bot's own orientation.
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
        float forwardSpeed = localVelocity.z;
        float sidewaysSpeed = localVelocity.x;

        // Set booleans for animator based on movement direction.
        animator.SetBool("isIdle", agent.velocity.magnitude < 0.1f && target == null);
        animator.SetBool("isWalkingForward", forwardSpeed > 0.1f);
        animator.SetBool("isWalkingBackward", forwardSpeed < -0.1f);
        animator.SetBool("isWalkingSidewaysRight", sidewaysSpeed > 0.1f);
        animator.SetBool("isWalkingSidewaysLeft", sidewaysSpeed < -0.1f);
    }
    
    void HandleFallDetection()
    {
        // agent.isOnNavMesh is the most reliable way to check if the bot is "grounded".
        if (agent.isOnNavMesh && !wasGrounded)
        {
            // The bot has just landed.
            float fallDistance = fallStartYPosition - transform.position.y;
            if (fallDistance > fallHeightToDie)
            {
                Die();
            }
        }
        
        wasGrounded = agent.isOnNavMesh;
        
        if (!wasGrounded)
        {
            // The bot is currently falling. Store its starting height.
            fallStartYPosition = transform.position.y;
        }
    }

    void Die()
    {
        Debug.Log($"[{name}] fell from a height and is entering the death state.");
        currentState = State.Dead;
        agent.enabled = false; // Disable navigation
        
        // Trigger the death and collapse animations.
        animator.SetTrigger("death");
        animator.SetBool("isCollapsed", true);

        // Start the recovery process.
        StartCoroutine(RecoverRoutine());
    }

    IEnumerator RecoverRoutine()
    {
        yield return new WaitForSeconds(recoveryTime);
        
        Debug.Log($"[{name}] has recovered.");
        
        // Trigger the recover animation and reset state.
        animator.SetBool("isCollapsed", false);
        animator.SetTrigger("recover");

        agent.enabled = true; // Re-enable navigation
        currentState = State.Patrolling;
    }
}
