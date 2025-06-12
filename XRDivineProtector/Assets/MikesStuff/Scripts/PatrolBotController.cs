// Script 1: PatrolBotController.cs
// Attach this script to your Patrol Bot prefab.

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI; // We'll use NavMesh for intelligent movement.

[RequireComponent(typeof(NavMeshAgent))]
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

    [Header("Attack Settings")]
    [Tooltip("The layer for the player's companion sphere, used for detection.")]
    public LayerMask targetMask;
    [Tooltip("How close the bot needs to be to attack.")]
    public float attackRange = 2f;
    [Tooltip("A reference to the projectile prefab the bot will fire.")]
    public GameObject projectilePrefab;
    [Tooltip("The point from which the projectile is fired.")]
    public Transform firePoint;
    [Tooltip("How often the bot can fire (in seconds).")]
    public float fireRate = 1f;


    // --- Private Variables ---
    private NavMeshAgent agent;
    private int currentPatrolIndex = 0;
    private Transform target; // The companion sphere
    private float timeSinceLastAttack = 0f;
    
    private enum State { Patrolling, Chasing, Attacking }
    private State currentState;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
        FindVisibleTarget();
        timeSinceLastAttack += Time.deltaTime;

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

    /// <summary>
    /// Moves the bot along its designated patrol route.
    /// </summary>
    void Patrol()
    {
        if (target != null)
        {
            currentState = State.Chasing;
            return;
        }
        
        // If the bot has reached its current destination, move to the next one.
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            agent.SetDestination(patrolPoints[currentPatrolIndex]);
        }
    }

    /// <summary>
    /// Moves towards the detected target.
    /// </summary>
    void Chase()
    {
        if (target == null)
        {
            currentState = State.Patrolling;
            // Return to the patrol route
            agent.SetDestination(patrolPoints[currentPatrolIndex]);
            return;
        }

        agent.SetDestination(target.position);
        
        // If we get in attack range, switch to attacking.
        if (Vector3.Distance(transform.position, target.position) <= attackRange)
        {
            currentState = State.Attacking;
        }
    }

    /// <summary>
    /// Attacks the target when in range.
    /// </summary>
    void Attack()
    {
        if (target == null)
        {
            currentState = State.Chasing; // Go back to chasing if target is lost
            return;
        }

        // Stop moving to attack
        agent.SetDestination(transform.position);
        
        // Look at the target
        transform.LookAt(target);

        // Fire a projectile if the cooldown has passed.
        if (timeSinceLastAttack >= fireRate)
        {
            if (projectilePrefab && firePoint)
            {
                Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                timeSinceLastAttack = 0f;
            }
        }
        
        // If the target moves out of attack range, go back to chasing.
        if (Vector3.Distance(transform.position, target.position) > attackRange)
        {
            currentState = State.Chasing;
        }
    }

    /// <summary>
    /// Uses a sphere cast and raycast to find a visible target.
    /// </summary>
    void FindVisibleTarget()
    {
        target = null;
        // Find all colliders within the view radius that are on the target layer.
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        foreach (var targetCollider in targetsInViewRadius)
        {
            Transform potentialTarget = targetCollider.transform;
            Vector3 dirToTarget = (potentialTarget.position - transform.position).normalized;

            // Check if the target is within the bot's field of view.
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(transform.position, potentialTarget.position);

                // Check if there are any obstacles blocking the line of sight.
                if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    // If all checks pass, we have found our target.
                    target = potentialTarget;
                    return; // Exit after finding the first valid target.
                }
            }
        }
    }
}