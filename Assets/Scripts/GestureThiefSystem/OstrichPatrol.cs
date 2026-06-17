// OstrichPatrol.cs
// NavMesh-guarded ostrich patrol + chase.
//
// BALANCE DESIGN:
//   - Patrols around the NEST (not its spawn) at a RANDOM radius each leg,
//     ranging from close-to-nest to far-from-nest, so the danger zone shifts.
//   - Detects the thief within detectRange — but ONLY if the thief is NOT hidden.
//     (A hidden thief = STOP gesture given = ostrich loses the target.)
//   - When chasing, moves FASTER than the thief so it genuinely catches up,
//     creating real danger and letting the attack actually land.
//   - When the thief hides or escapes detectRange, returns to patrol.

using UnityEngine;
using UnityEngine.AI;

namespace GestureThiefSystem
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class OstrichPatrol : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Transform thiefTarget;
        [Tooltip("The nest / egg the ostrich guards. Patrol centres on this.")]
        [SerializeField] private Transform nest;

        [Header("Detection")]
        [Tooltip("How far the ostrich can notice an EXPOSED (non-hidden) thief.")]
        [SerializeField] private float detectRange = 14f;

        [Header("Speeds")]
        [SerializeField] private float patrolSpeed = 1.6f;
        [Tooltip("Must be faster than the thief's move speed so it can catch up.")]
        [SerializeField] private float chaseSpeed  = 4.5f;

        [Header("Patrol Ranging")]
        [Tooltip("Closest a patrol point can be to the nest.")]
        [SerializeField] private float patrolRadiusMin = 2.5f;
        [Tooltip("Farthest a patrol point can be from the nest.")]
        [SerializeField] private float patrolRadiusMax = 12f;
        [SerializeField] private float waitMin = 0.8f;
        [SerializeField] private float waitMax = 2.5f;

        private NavMeshAgent _agent;
        private Animator     _anim;
        private ThiefController _thiefCtrl;
        private Vector3      _patrolCenter;
        private float        _waitTimer;
        private bool         _isWaiting;
        private bool         _navReady;
        private bool         _chasing;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _anim  = GetComponent<Animator>();
        }

        private void Start()
        {
            if (thiefTarget == null)
            {
                _thiefCtrl = FindAnyObjectByType<ThiefController>();
                if (_thiefCtrl != null) thiefTarget = _thiefCtrl.transform;
            }
            else
            {
                _thiefCtrl = thiefTarget.GetComponent<ThiefController>();
            }
            // Patrol centres on the nest if assigned, otherwise the spawn point.
            _patrolCenter = nest != null ? nest.position : transform.position;
        }

        private void Update()
        {
            if (!_agent.isOnNavMesh) { SetAnimMoving(false); return; }
            if (!_navReady) { _navReady = true; PickNewPatrolDestination(); }

            // The ostrich only 'sees' the thief if the thief is NOT hidden.
            bool thiefExposed = _thiefCtrl == null || _thiefCtrl.CurrentState != ThiefState.Hidden;
            bool inRange = thiefTarget != null &&
                           Vector3.Distance(transform.position, thiefTarget.position) <= detectRange;

            if (thiefExposed && inRange) RunChase();
            else                         RunPatrol();
        }

        private void RunChase()
        {
            if (!_agent.isOnNavMesh) return;
            _chasing         = true;
            _isWaiting       = false;
            _agent.speed     = chaseSpeed;
            _agent.isStopped = false;
            _agent.SetDestination(thiefTarget.position);
            SetAnimMoving(true);
        }

        private void RunPatrol()
        {
            if (!_agent.isOnNavMesh) return;

            // Just stopped chasing? Pick a fresh patrol point immediately.
            if (_chasing) { _chasing = false; _isWaiting = false; PickNewPatrolDestination(); }

            _agent.speed = patrolSpeed;

            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                SetAnimMoving(false);
                if (_waitTimer <= 0f) { _isWaiting = false; PickNewPatrolDestination(); }
                return;
            }

            bool arrived = !_agent.pathPending && _agent.isOnNavMesh &&
                           _agent.remainingDistance <= _agent.stoppingDistance + 0.2f;
            if (arrived)
            {
                _isWaiting       = true;
                _waitTimer       = Random.Range(waitMin, waitMax);
                _agent.isStopped = true;
            }
            SetAnimMoving(!_isWaiting);
        }

        // Random distance (close..far from nest) AND random direction each leg.
        private void PickNewPatrolDestination()
        {
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = false;

            for (int i = 0; i < 16; i++)
            {
                float   radius = Random.Range(patrolRadiusMin, patrolRadiusMax);
                float   ang    = Random.Range(0f, Mathf.PI * 2f);
                Vector3 offset = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
                Vector3 pt     = _patrolCenter + offset;

                if (NavMesh.SamplePosition(pt, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    _agent.SetDestination(hit.position);
                    return;
                }
            }
            Debug.LogWarning("[OstrichPatrol] No valid NavMesh sample near nest.");
        }

        private void SetAnimMoving(bool moving)
        {
            if (_anim == null) return;
            _anim.SetBool("isMoving", moving);
        }

        public void SetThiefTarget(Transform t)
        {
            thiefTarget = t;
            _thiefCtrl  = t != null ? t.GetComponent<ThiefController>() : null;
        }
        public void SetNest(Transform t) { nest = t; if (t != null) _patrolCenter = t.position; }
    }
}