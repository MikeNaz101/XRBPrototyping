// EnemyAIController.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAIController : MonoBehaviour
{
    [Header("AI Settings")]
    [Tooltip("How far the enemy can see to find a target.")]
    public float detectionRadius = 15f;
    [Tooltip("How close the enemy needs to be to start its attack routine.")]
    public float attackRange = 2f;
    [Tooltip("The layers that are considered valid targets (e.g., Villagers, Walls, Houses).")]
    public LayerMask targetMask;

    [Header("Attack Behavior")]
    [Tooltip("The speed of the lunge towards the target.")]
    public float lungeSpeed = 8f;
    [Tooltip("How long the lunge forward lasts.")]
    public float lungeDuration = 0.2f;
    [Tooltip("How far the enemy backs up after a ram.")]
    public float backupDistance = 1f;
    [Tooltip("The time between each ram attempt.")]
    public float attackCooldown = 1.5f;
    [Tooltip("The damage dealt with each ram.")]
    public int attackDamage = 10;

    private NavMeshAgent _agent;
    private Transform _currentTarget;
    private Coroutine _attackCoroutine;

    private enum State { Wandering, Chasing, Attacking }
    private State _currentState;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        _currentState = State.Wandering;
    }

    void Update()
    {
        switch (_currentState)
        {
            case State.Wandering:
                UpdateWanderState();
                break;
            case State.Chasing:
                UpdateChaseState();
                break;
            case State.Attacking:
                // Attack logic is handled in the coroutine
                break;
        }
    }

    void UpdateWanderState()
    {
        FindClosestTarget();
        if (_currentTarget != null)
        {
            _currentState = State.Chasing;
            return;
        }

        // Wander aimlessly if no target is found
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            WanderToNewPoint();
        }
    }

    void UpdateChaseState()
    {
        if (_currentTarget == null)
        {
            _currentState = State.Wandering;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
        if (distanceToTarget <= attackRange)
        {
            _currentState = State.Attacking;
            _attackCoroutine = StartCoroutine(AttackRoutine());
        }
        else
        {
            _agent.SetDestination(_currentTarget.position);
        }
    }

    void FindClosestTarget()
    {
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, detectionRadius, targetMask);
        
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider targetCollider in potentialTargets)
        {
            float distance = Vector3.Distance(transform.position, targetCollider.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = targetCollider.transform;
            }
        }
        _currentTarget = closest;
    }

    IEnumerator AttackRoutine()
    {
        while (_currentTarget != null)
        {
            _agent.isStopped = true; // Stop NavMesh movement during the attack sequence

            Vector3 targetPosition = _currentTarget.position;
            transform.LookAt(targetPosition);

            // Lunge forward
            Vector3 lungeStartPosition = transform.position;
            Vector3 lungeEndPosition = transform.position + transform.forward * 1.5f;
            float elapsedTime = 0f;
            while(elapsedTime < lungeDuration)
            {
                transform.position = Vector3.Lerp(lungeStartPosition, lungeEndPosition, (elapsedTime / lungeDuration));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Deal damage (requires the target to have a script with a 'TakeDamage' method)
            IDamageable damageable = _currentTarget.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }

            // Back up
            Vector3 backupEndPosition = transform.position - transform.forward * backupDistance;
            elapsedTime = 0f;
            while(elapsedTime < 0.5f) // Backup duration
            {
                transform.position = Vector3.Lerp(lungeEndPosition, backupEndPosition, (elapsedTime / 0.5f));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(attackCooldown);

            // Re-evaluate if target is still valid
            if (_currentTarget == null || Vector3.Distance(transform.position, _currentTarget.position) > attackRange * 1.2f)
            {
                break; // Exit coroutine if target is destroyed or out of range
            }
        }
        
        _agent.isStopped = false; // Resume NavMesh movement
        _currentState = State.Chasing; // Go back to chasing to re-evaluate
    }
    
    void WanderToNewPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 10f;
        randomDirection += transform.position;
        NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 10f, NavMesh.AllAreas);
        _agent.SetDestination(hit.position);
    }
}