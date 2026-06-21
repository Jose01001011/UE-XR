// OstrichAI.cs
// Ostrich brain with TWO concentric detection zones (spec section C/D).
using UnityEngine;
using UnityEngine.AI;

namespace OstrichHeist
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class OstrichAI : NpcBase
    {
        [Header("Targets")]
        [SerializeField] private ThiefAI thief;
        [SerializeField] private Transform nest;

        [Header("Detection Zones (radii in metres)")]
        [Tooltip("OUTER warning zone — ostrich gets suspicious & approaches; player can still hide to disengage.")]
        [SerializeField] private float dangerRadius = 8f;
        [Tooltip("INNER attack zone — entering it while exposed locks the ostrich onto the thief.")]
        [SerializeField] private float attackRadius = 3.5f;

        [Header("Speeds")]
        [SerializeField] private float patrolSpeed = 1.3f;
        [SerializeField] private float chaseSpeed  = 2.7f;

        [Header("Patrol")]
        [SerializeField] private float patrolRadiusMin = 2.5f;
        [SerializeField] private float patrolRadiusMax = 10f;
        [SerializeField] private float waitMin = 0.8f;
        [SerializeField] private float waitMax = 2.2f;

        [Header("Attack")]
        [SerializeField] private float strikeRange = 2.4f;
        [SerializeField] private float windUpTime = 1.1f;
        [SerializeField] private float attackInterval = 1.8f;
        [SerializeField] private int   hitsToDefeat = 3;

        public OstrichState State { get; private set; } = OstrichState.Idle;
        public int Hits { get; private set; }
        public bool LockedOn { get; private set; }

        private NavMeshAgent _agent;
        private Vector3 _patrolCenter;
        private float _waitTimer;
        private bool  _isWaiting;
        private bool  _navReady;
        private float _attackTimer;
        private bool  _engaged;
        private bool  _gameEnded;

        protected override void Awake()
        {
            base.Awake();
            _agent = GetComponent<NavMeshAgent>();
            SyncVisualZones();
        }

        // Visual zone colliders (WarningZone/DangerZone) are sized manually in the
        // Inspector by the designer. We do NOT override them here — we only read them
        // so gameplay radii can match if desired. Left intentionally hands-off.
        private void SyncVisualZones()
        {
            // no-op: respect designer-set collider sizes
        }

        private void OnEnable()
        {
            GameEvents.OnSafeZoneReached += StopAll;
            GameEvents.OnThiefDefeated   += StopAll;
        }
        private void OnDisable()
        {
            GameEvents.OnSafeZoneReached -= StopAll;
            GameEvents.OnThiefDefeated   -= StopAll;
        }

        private void Start()
        {
            if (thief == null) thief = FindAnyObjectByType<ThiefAI>();
            _patrolCenter = nest != null ? nest.position : transform.position;
            EnterState(OstrichState.Patrolling);
        }

        private void Update()
        {
            if (_gameEnded || !_agent.isOnNavMesh) { SetBoolSafe("isMoving", false); return; }
            if (!_navReady) { _navReady = true; PickPatrolPoint(); }
            if (thief == null) { DoPatrol(); return; }

            float dist = Vector3.Distance(transform.position, thief.transform.position);
            bool exposed = !thief.IsHidden
                              && thief.State != ThiefState.Victory
                              && thief.State != ThiefState.Idle      // idle/looking = not yet a target
                              && thief.State != ThiefState.Attacked; // already resolving

            if (!LockedOn && exposed && dist <= attackRadius)
            {
                LockedOn = true;
                Log("LOCKED ON — thief entered attack zone exposed");
            }

            if (LockedOn)
            {
                if (dist <= strikeRange) DoAttack(dist);
                else DoChase();
            }
            else if (exposed && dist <= dangerRadius)
            {
                GameEvents.RaiseThiefDetected();
                EnterState(OstrichState.Investigating);
                _agent.speed = chaseSpeed * 0.85f;
                _agent.isStopped = false;
                _agent.SetDestination(thief.transform.position);
                SetBoolSafe("isMoving", true);
            }
            else
            {
                _engaged = false;
                DoPatrol();
            }

            if (nest != null && Vector3.Distance(transform.position, nest.position) <= patrolRadiusMin + 1f)
                GameEvents.RaiseDangerNearNest();
        }

        public void ChaseThief() => DoChase();
        private void DoChase()
        {
            EnterState(OstrichState.Chasing);
            _engaged = false;
            _agent.speed = chaseSpeed;
            _agent.isStopped = false;
            _agent.SetDestination(thief.transform.position);
            SetBoolSafe("isMoving", true);
        }

        public void AttackThief() { }
        private void DoAttack(float dist)
        {
            EnterState(OstrichState.Attacking);
            _agent.isStopped = true;
            SetBoolSafe("isMoving", false);
            Vector3 look = thief.transform.position - transform.position; look.y = 0;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 6f);
            if (!_engaged) { _engaged = true; _attackTimer = attackInterval - windUpTime; }
            _attackTimer += Time.deltaTime;
            if (_attackTimer >= attackInterval) { _attackTimer = 0f; LandHit(); }
        }

        private void LandHit()
        {
            if (_gameEnded || thief == null) return;
            Hits++;
            SetTriggerSafe("Attack");
            thief.TakeDamage();
            Log("Hit " + Hits + "/" + hitsToDefeat);
            if (Hits >= hitsToDefeat)
            {
                _gameEnded = true;
                thief.Defeated();
                GameEvents.RaiseThiefDefeated();
                Log("Thief defeated -> game over");
            }
        }

        private void DoPatrol()
        {
            if (State != OstrichState.Patrolling) EnterState(OstrichState.Patrolling);
            _agent.speed = patrolSpeed;
            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                SetBoolSafe("isMoving", false);
                if (_waitTimer <= 0f) { _isWaiting = false; PickPatrolPoint(); }
                return;
            }
            bool arrived = !_agent.pathPending && _agent.isOnNavMesh &&
                           _agent.remainingDistance <= _agent.stoppingDistance + 0.2f;
            if (arrived)
            {
                _isWaiting = true;
                _waitTimer = Random.Range(waitMin, waitMax);
                _agent.isStopped = true;
            }
            SetBoolSafe("isMoving", !_isWaiting);
        }

        public void ReturnToNest()
        {
            EnterState(OstrichState.Returning);
            if (_agent.isOnNavMesh && nest != null) _agent.SetDestination(nest.position);
        }

        private void PickPatrolPoint()
        {
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = false;
            for (int i = 0; i < 16; i++)
            {
                float r = Random.Range(patrolRadiusMin, patrolRadiusMax);
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector3 pt = _patrolCenter + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (NavMesh.SamplePosition(pt, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                { _agent.SetDestination(hit.position); return; }
            }
        }

        private void StopAll()
        {
            _gameEnded = true;
            if (_agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
            SetBoolSafe("isMoving", false);
            EnterState(OstrichState.Idle);
        }

        private void EnterState(OstrichState next)
        {
            if (State == next) return;
            Log(State + " -> " + next);
            State = next;
        }

        public void SetTargets(ThiefAI t, Transform nestT)
        { thief = t; nest = nestT; _patrolCenter = nestT != null ? nestT.position : transform.position; }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.6f);
            DrawCircle(transform.position, dangerRadius);
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.7f);
            DrawCircle(transform.position, attackRadius);
        }
        private void DrawCircle(Vector3 c, float r)
        {
            const int seg = 48; Vector3 prev = c + new Vector3(r, 0.05f, 0);
            for (int i = 1; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                Vector3 next = c + new Vector3(Mathf.Cos(a)*r, 0.05f, Mathf.Sin(a)*r);
                Gizmos.DrawLine(prev, next); prev = next;
            }
        }
    }
}