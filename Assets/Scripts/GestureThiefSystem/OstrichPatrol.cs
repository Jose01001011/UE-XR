// OstrichPatrol.cs
// Drives the ostrich NPC using a NavMeshAgent.
//
// BEHAVIOUR:
//   - Randomly wanders within a set radius of its spawn position.
//   - When the thief comes within chaseRange, switches to chase mode.
//   - On arriving at a patrol point, waits a random interval before picking the next one.
//   - Feeds an isMoving bool to the Animator so walk/idle clips play correctly.
//
// SETUP:
//   1. Attach to the Ostrich GameObject.
//   2. Bake a NavMesh on the scene terrain first (Window > AI > Navigation > Bake).
//   3. Assign thiefTarget in the Inspector, or leave empty and the Bootstrap
//      will resolve it at runtime via GestureThiefBootstrap.
//
// NOTE: OstrichAttack (on the same object) handles the actual hit delivery —
//       this script only controls movement.

using UnityEngine;
using UnityEngine.AI;

namespace GestureThiefSystem
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class OstrichPatrol : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Inspector
        // ----------------------------------------------------------------
        [Header("Chase")]
        [Tooltip("Transform of the Thief. Left empty = GestureThiefBootstrap fills this.")]
        [SerializeField] private Transform thiefTarget;

        [Tooltip("Distance at which the ostrich stops patrolling and starts chasing.")]
        [SerializeField] private float chaseRange = 7f;

        [Tooltip("Movement speed during patrol wander.")]
        [SerializeField] private float patrolSpeed = 1.4f;

        [Tooltip("Movement speed when chasing the thief.")]
        [SerializeField] private float chaseSpeed  = 3.5f;

        [Header("Patrol")]
        [Tooltip("Radius around spawn point that the ostrich wanders.")]
        [SerializeField] private float patrolRadius = 9f;

        [Tooltip("Min and max seconds to stand still between patrol moves.")]
        [SerializeField] private float waitMin = 1.5f;
        [SerializeField] private float waitMax = 4f;

        // ----------------------------------------------------------------
        // Internal
        // ----------------------------------------------------------------
        private NavMeshAgent _agent;
        private Animator     _anim;
        private Vector3      _spawnOrigin;   // fixed reference for patrol radius
        private float        _waitTimer;
        private bool         _isWaiting;

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------
        private void Awake()
        {
            _agent       = GetComponent<NavMeshAgent>();
            _anim        = GetComponent<Animator>();   // optional — no error if absent
            _spawnOrigin = transform.position;
        }

        private void Start()
        {
            // If the thief wasn't assigned in the Inspector, look for it now.
            // GestureThiefBootstrap also does this, but Start() runs slightly later,
            // so the assignment is usually set before we need it.
            if (thiefTarget == null)
            {
                var tc = FindAnyObjectByType<ThiefController>();
                if (tc != null)
                    thiefTarget = tc.transform;
            }

            PickNewPatrolDestination();
        }

        private void Update()
        {
            if (thiefTarget != null &&
                Vector3.Distance(transform.position, thiefTarget.position) <= chaseRange)
            {
                RunChase();
            }
            else
            {
                RunPatrol();
            }
        }

        // ----------------------------------------------------------------
        // Chase
        // ----------------------------------------------------------------
        private void RunChase()
        {
            _isWaiting        = false;
            _agent.speed      = chaseSpeed;
            _agent.isStopped  = false;
            _agent.SetDestination(thiefTarget.position);
            SetAnimMoving(true);
        }

        // ----------------------------------------------------------------
        // Patrol
        // ----------------------------------------------------------------
        private void RunPatrol()
        {
            _agent.speed = patrolSpeed;

            if (_isWaiting)
            {
                // Count down rest time, then move again.
                _waitTimer -= Time.deltaTime;
                SetAnimMoving(false);
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    PickNewPatrolDestination();
                }
                return;
            }

            // Check if we've reached the current waypoint.
            bool arrived = !_agent.pathPending &&
                           _agent.remainingDistance <= _agent.stoppingDistance + 0.15f;
            if (arrived)
            {
                _isWaiting = true;
                _waitTimer = Random.Range(waitMin, waitMax);
                _agent.isStopped = true;
            }

            SetAnimMoving(!_isWaiting);
        }

        // Picks a random point on the NavMesh within patrolRadius of spawn.
        // Retries up to 12 times to ensure a valid NavMesh sample.
        private void PickNewPatrolDestination()
        {
            _agent.isStopped = false;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                // Random direction, flat (y=0 offset)
                Vector2 circle  = Random.insideUnitCircle * patrolRadius;
                Vector3 target  = _spawnOrigin + new Vector3(circle.x, 0f, circle.y);

                if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                {
                    _agent.SetDestination(hit.position);
                    return;
                }
            }

            // Fall back to spawn origin if no valid point found (e.g. NavMesh not baked yet).
            Debug.LogWarning("[OstrichPatrol] Could not sample a valid NavMesh point — is the NavMesh baked?");
            _agent.SetDestination(_spawnOrigin);
        }

        // ----------------------------------------------------------------
        // Helper
        // ----------------------------------------------------------------
        private void SetAnimMoving(bool moving)
        {
            if (_anim == null) return;
            // ThiefAnimator uses "isMoving"; reuse the same parameter name here.
            _anim.SetBool("isMoving", moving);
        }

        // Allow GestureThiefBootstrap to inject the thief reference at runtime.
        public void SetThiefTarget(Transform target) => thiefTarget = target;
    }
}
