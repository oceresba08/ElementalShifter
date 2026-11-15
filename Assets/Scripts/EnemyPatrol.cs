
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

    [Header("Resource Hunting")]
    public bool seekResources = true;
    public float sourceStopDistance = 2.2f;

    [Header("Activity Timing")]
    public float patrolSeconds = 8f;
    public float seekSeconds = 20f;

    private enum Activity
    {
        Patrol,
        Seek,
        Chase
    }

    private Activity activity = Activity.Patrol;
    private Activity previousActivity = Activity.Patrol;

    private NavMeshAgent agent;
    private EnemyChargeAttack attack;
    private ElementalVoidSiphon siphon;

    private int nextPoint;
    private float activityEndTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<EnemyChargeAttack>();
        siphon = GetComponent<ElementalVoidSiphon>();
    }

    void Start()
    {
        activityEndTime = Time.time + patrolSeconds;

        if (siphon != null)
        {
            siphon.SetSeeking(false);
            siphon.ChargeStolen += OnChargeStolen;
            siphon.AbsorptionInterrupted += OnAbsorptionInterrupted;
        }
    }


    void OnAbsorptionInterrupted()
    {
        ChangeActivity(Activity.Patrol);

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }


    void OnDestroy()
    {
        if (siphon != null)
        {
            siphon.ChargeStolen -= OnChargeStolen;
            siphon.AbsorptionInterrupted -= OnAbsorptionInterrupted;
        }
    }


    void OnChargeStolen(ElementData element)
    {
        // One successful absorption ends this search trip.
        ChangeActivity(Activity.Patrol);
    }

    void ChangeActivity(Activity next)
    {
        activity = next;

        if (activity == Activity.Patrol)
            activityEndTime = Time.time + patrolSeconds;
        else if (activity == Activity.Seek)
            activityEndTime = Time.time + seekSeconds;

        if (siphon != null)
            siphon.SetSeeking(activity == Activity.Seek);
    }

    void Update()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;


        if (siphon != null && siphon.IsInterrupted)
        {
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }


        if (player == null)
        {
            PlayerElements found =
                FindFirstObjectByType<PlayerElements>();

            if (found != null)
                player = found.transform;
        }

        if (attack != null && attack.IsCharging)
            return;

        // Player detection overrides other activities.
        if (player != null)
        {
            float distance = Vector3.Distance(
                transform.position, player.position);

            if (activity != Activity.Chase &&
                distance <= detectionDistance)
            {
                previousActivity = activity;
                ChangeActivity(Activity.Chase);
            }
            else if (activity == Activity.Chase &&
                     distance > returnDistance)
            {
                // Resume patrol after the player escapes.
                ChangeActivity(Activity.Patrol);
            }
        }

        if (activity == Activity.Chase)
        {
            ChasePlayer();
            return;
        }

        // Alternate between patrol and source searching.
        if (Time.time >= activityEndTime)
        {
            if (activity == Activity.Patrol &&
                seekResources &&
                siphon != null &&
                siphon.CanSeekResource)
            {
                ChangeActivity(Activity.Seek);
            }
            else
            {
                ChangeActivity(Activity.Patrol);
            }
        }

        if (activity == Activity.Seek)
        {
            SeekElementSource();
        }
        else
        {
            FollowPatrolPoints();
        }
    }

    void ChasePlayer()
    {
        if (player == null)
            return;

        agent.isStopped = false;
        agent.stoppingDistance = 1.7f;
        agent.SetDestination(player.position);
    }

    void SeekElementSource()
    {
        if (siphon == null || !siphon.CanSeekResource)
        {
            ChangeActivity(Activity.Patrol);
            return;
        }

        ElementSource target = siphon.TargetSource;

        if (target == null)
        {
            // Give the siphon time to find a source.
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        agent.isStopped = false;
        agent.stoppingDistance =
            Mathf.Max(0.5f, sourceStopDistance);

        agent.SetDestination(target.transform.position);
    }

    void FollowPatrolPoints()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
            return;

        Transform point = patrolPoints[nextPoint];

        if (point == null)
        {
            nextPoint =
                (nextPoint + 1) % patrolPoints.Length;
            return;
        }

        agent.isStopped = false;
        agent.stoppingDistance = 0;

        agent.SetDestination(point.position);

        if (!agent.pathPending &&
            agent.remainingDistance <=
            patrolArrivalDistance)
        {
            nextPoint =
                (nextPoint + 1) % patrolPoints.Length;
        }
    }
}
