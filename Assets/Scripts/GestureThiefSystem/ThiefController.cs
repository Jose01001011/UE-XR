// ThiefController.cs
// Core NPC controller for the Thief. Listens to GestureEventBus (GO / STOP only).
// Guards all NavMeshAgent calls with isOnNavMesh checks.

using UnityEngine;
using UnityEngine.AI;

namespace GestureThiefSystem
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class ThiefController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Transform eggObjective;
        [SerializeField] private float reachDistance = 0.8f;
        [SerializeField] private float walkSpeed     = 1.5f;

        [Header("Detection Chances (0-1)")]
        [SerializeField] private float normalDetectionChance = 1.0f;
        [SerializeField] private float hiddenDetectionChance = 0.05f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnEggReached;
        public UnityEngine.Events.UnityEvent OnDetected;

        private NavMeshAgent _agent;
        private Animator     _animator;
        private ThiefState   _currentState = ThiefState.Idle;

        public ThiefState CurrentState => _currentState;

        public float DetectionChance =>
            _currentState == ThiefState.Hidden ? hiddenDetectionChance : normalDetectionChance;

        private void Awake()
        {
            _agent    = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()  { GestureEventBus.OnGesturePerformed += HandleGesture; }
        private void OnDisable() { GestureEventBus.OnGesturePerformed -= HandleGesture; }

        private void Update() { if (_agent.isOnNavMesh) CheckEggReached(); }

        // GO gesture
        public void Move()
        {
            SetState(ThiefState.Moving);
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = false;
            _agent.speed     = walkSpeed;
            if (eggObjective != null) _agent.SetDestination(eggObjective.position);
            Debug.Log("[Thief] GO — moving to egg.");
        }

        // STOP gesture — thief stops and hides
        public void Stop()
        {
            SetState(ThiefState.Hidden);
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = true;
            _agent.velocity  = Vector3.zero;
            Debug.Log("[Thief] STOP — hiding.");
        }

        public void TriggerDetected()
        {
            SetState(ThiefState.Alert);
            if (_agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
            Debug.Log("[Thief] DETECTED!");
            OnDetected?.Invoke();
        }

        private void HandleGesture(PlayerGesture gesture)
        {
            switch (gesture)
            {
                case PlayerGesture.GoForward: Move(); break;
                case PlayerGesture.Stop:      Stop(); break;
                // All other gestures ignored — only GO and STOP active
            }
        }

        private void SetState(ThiefState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
            UpdateAnimator(newState);
        }

        private void UpdateAnimator(ThiefState state)
        {
            if (_animator == null) return;
            _animator.SetBool("isMoving",  state == ThiefState.Moving);
            _animator.SetBool("isHidden",  state == ThiefState.Hidden);
            _animator.SetBool("isAlert",   state == ThiefState.Alert);
            _animator.SetBool("isRunning", false);
            _animator.SetBool("isCrouching", false);
        }

        private void CheckEggReached()
        {
            if (eggObjective == null) return;
            if (_currentState == ThiefState.Hidden || _currentState == ThiefState.Alert) return;
            if (Vector3.Distance(transform.position, eggObjective.position) <= reachDistance)
            {
                Debug.Log("[Thief] Egg reached!");
                _agent.isStopped = true;
                SetState(ThiefState.Idle);
                OnEggReached?.Invoke();
            }
        }
    }
}