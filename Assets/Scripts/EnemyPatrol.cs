using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    public Transform[] patrolPoints;
    public Transform player;
    public float detectionDistance = 7f;
    public float returnDistance = 11f;
    public float patrolArrivalDistance = 1f;

    private NavMeshAgent agent;
    private int nextPoint;
    private bool chasing;

    private void Awake() => agent = GetComponent<NavMeshAgent>();

    private void Update()
    {
        if (!agent.isOnNavMesh || player == null) return;
        float distance = Vector3.Distance(transform.position, player.position);
        if (!chasing && distance <= detectionDistance) chasing = true;
        else if (chasing && distance > returnDistance) chasing = false;

        if (chasing)
        {
            agent.SetDestination(player.position);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0) return;
        Transform destination = patrolPoints[nextPoint];
        if (destination == null) return;
        agent.SetDestination(destination.position);
        if (!agent.pathPending && agent.remainingDistance <= patrolArrivalDistance)
            nextPoint = (nextPoint + 1) % patrolPoints.Length;
    }
}
