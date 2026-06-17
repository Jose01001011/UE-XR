// OstrichPatrol.cs
// NavMesh-guarded ostrich patrol and chase behaviour.
// All agent calls are gated on _agent.isOnNavMesh to prevent the
// 'GetRemainingDistance on agent not placed on NavMesh' error flood.

using UnityEngine;
using UnityEngine.AI;

namespace GestureThiefSystem
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class OstrichPatrol : MonoBehaviour
    {
        [Header("Chase")]
        [SerializeField] private Transform thiefTarget;
        [SerializeField] private float chaseRange   = 12f;
        [SerializeField] private float patrolSpeed  = 1.4f;
        [SerializeField] private float chaseSpeed   = 3.5f;

        [Header("Patrol")]
        [SerializeField] private float patrolRadius = 9f;
        [SerializeField] private float waitMin      = 1.5f;
        [SerializeField] private float waitMax      = 4f;

        private NavMeshAgent _agent;
        private Animator     _anim;
        private Vector3      _spawnOrigin;
        private float        _waitTimer;
        private bool         _isWaiting;
        private bool         _navReady;

        private void Awake()
        {
            _agent       = GetComponent<NavMeshAgent>();
            _anim        = GetComponent<Animator>();
            _spawnOrigin = transform.position;
        }

        private void Start()
        {
            if (thiefTarget == null)
            {
                var tc = FindAnyObjectByType<ThiefController>();
                if (tc != null) thiefTarget = tc.transform;
            }
        }

        private void Update()
        {
            if (!_agent.isOnNavMesh) { SetAnimMoving(false); return; }

            if (!_navReady) { _navReady = true; PickNewPatrolDestination(); }

            bool near = thiefTarget != null &&
                        Vector3.Distance(transform.position, thiefTarget.position) <= chaseRange;
            if (near) RunChase(); else RunPatrol();
        }

        private void RunChase()
        {
            if (!_agent.isOnNavMesh) return;
            _isWaiting       = false;
            _agent.speed     = chaseSpeed;
            _agent.isStopped = false;
            _agent.SetDestination(thiefTarget.position);
            SetAnimMoving(true);
        }

        private void RunPatrol()
        {
            if (!_agent.isOnNavMesh) return;
            _agent.speed = patrolSpeed;

            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                SetAnimMoving(false);
                if (_waitTimer <= 0f) { _isWaiting = false; PickNewPatrolDestination(); }
                return;
            }

            bool arrived = !_agent.pathPending && _agent.isOnNavMesh &&
                           _agent.remainingDistance <= _agent.stoppingDistance + 0.15f;
            if (arrived)
            {
                _isWaiting = true;
                _waitTimer = Random.Range(waitMin, waitMax);
                _agent.isStopped = true;
            }
            SetAnimMoving(!_isWaiting);
        }

        private void PickNewPatrolDestination()
        {
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = false;
            for (int i = 0; i < 12; i++)
            {
                Vector2 c = Random.insideUnitCircle * patrolRadius;
                Vector3 pt = _spawnOrigin + new Vector3(c.x, 0f, c.y);
                if (NavMesh.SamplePosition(pt, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                { _agent.SetDestination(hit.position); return; }
            }
            Debug.LogWarning("[OstrichPatrol] No valid NavMesh sample — bake NavMesh first.");
        }

        private void SetAnimMoving(bool moving)
        {
            if (_anim == null) return;
            _anim.SetBool("isMoving", moving);
        }

        public void SetThiefTarget(Transform t) => thiefTarget = t;
    }
}