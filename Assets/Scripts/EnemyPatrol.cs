using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public Transform player;
    public float detectionDistance = 7f;
    public float returnDistance = 11f;
    public float patrolArrivalDistance = 1f;
    [Header("Resource hunting")]
    public bool seekResources = true;
    public float sourceStopDistance = 2.2f;

    NavMeshAgent agent;
    EnemyChargeAttack attack;
    ElementalVoidSiphon siphon;
    int nextPoint;
    bool chasing;
    bool warned;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<EnemyChargeAttack>();
        siphon = GetComponent<ElementalVoidSiphon>();
    }

    void Update()
    {
        if (!agent.isOnNavMesh)
        {
            if (!warned) { Debug.LogWarning("Void is not on a baked NavMesh. Bake the floor and place the enemy on it.", this); warned = true; }
            return;
        }
        if (player == null)
        {
            PlayerElements found = FindFirstObjectByType<PlayerElements>();
            if (found != null) player = found.transform;
        }
        if (attack != null && attack.IsCharging) return;

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (!chasing && distance <= detectionDistance) chasing = true;
            else if (chasing && distance > returnDistance) chasing = false;
        }
        if (chasing && player != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = 1.7f;
            agent.SetDestination(player.position);
            return;
        }

        // The Siphon exposes a target even when it is out of drain range.
        if (seekResources && siphon != null && siphon.CanSeekResource && siphon.TargetSource != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.5f, sourceStopDistance);
            agent.SetDestination(siphon.TargetSource.transform.position);
            return;
        }

        agent.stoppingDistance = 0f;
        if (patrolPoints == null || patrolPoints.Length == 0) { agent.ResetPath(); return; }
        Transform point = patrolPoints[nextPoint];
        if (point == null) { nextPoint = (nextPoint + 1) % patrolPoints.Length; return; }
        agent.isStopped = false;
        agent.SetDestination(point.position);
        if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(0.2f, patrolArrivalDistance))
            nextPoint = (nextPoint + 1) % patrolPoints.Length;
    }
}
