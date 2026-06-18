// ThiefController.cs
// Thief NPC: responds to GO (move to egg) and STOP (stop + hide) only.
// NavMesh-guarded. Move speed is deliberately SLOW so the player has time
// to react and the ostrich (faster when chasing) can create real danger.

using System;
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
        [SerializeField] private float reachDistance = 1.0f;

        [Tooltip("Deliberately slow — gives the player reaction time and lets the ostrich catch up.")]
        [SerializeField] private float moveSpeed = 1.2f;

        [Header("Detection Chances (0-1)")]
        [SerializeField] private float normalDetectionChance = 1.0f;
        [SerializeField] private float hiddenDetectionChance = 0.05f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnEggReached;
        public UnityEngine.Events.UnityEvent OnDetected;

        private NavMeshAgent _agentCache;
        private Animator     _animatorCache;
        private ThiefState   _currentState = ThiefState.Idle;
        private bool         _reachedEgg;

        // Lazy accessors — robust even if Awake was skipped (e.g. after a domain reload).
        private NavMeshAgent _agent
        {
            get { if (_agentCache == null) _agentCache = GetComponent<NavMeshAgent>(); return _agentCache; }
        }
        private Animator _animator
        {
            get { if (_animatorCache == null) _animatorCache = GetComponent<Animator>(); return _animatorCache; }
        }

        public ThiefState CurrentState => _currentState;
        public bool ReachedEgg => _reachedEgg;

        public float DetectionChance =>
            _currentState == ThiefState.Hidden ? hiddenDetectionChance : normalDetectionChance;

        private void Awake()
        {
            // Force the agent speed to match our slow move speed (fixes prior mismatch).
            if (_agent != null) _agent.speed = moveSpeed;
        }

        private void OnEnable()  { GestureEventBus.OnGesturePerformed += HandleGesture; }
        private void OnDisable() { GestureEventBus.OnGesturePerformed -= HandleGesture; }

        private void Update() { if (_agent.isOnNavMesh) CheckEggReached(); }

        // GO gesture -> move to egg (slowly)
        public void Move()
        {
            if (_reachedEgg) return;
            SetState(ThiefState.Moving);
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = false;
            _agent.speed     = moveSpeed;
            if (eggObjective != null) _agent.SetDestination(eggObjective.position);
            Debug.Log("[Thief] GO — moving to egg (slow).");
        }

        // STOP gesture -> stop and hide (ostrich loses target)
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
                // Only GO and STOP are active.
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
            // ThiefAnimator controller params: StartMoving(Trigger), IsSpottedOrPausing(Bool), ReachedEgg(Trigger), WasCaught(Trigger)
            switch (state)
            {
                case ThiefState.Moving:
                    SafeBool("IsSpottedOrPausing", false);
                    SafeTrigger("StartMoving");
                    break;
                case ThiefState.Hidden:
                case ThiefState.Alert:
                case ThiefState.Idle:
                    SafeBool("IsSpottedOrPausing", true);
                    break;
            }
            // Generic fallbacks if a different controller is used.
            SafeBool("isMoving", state == ThiefState.Moving);
            SafeBool("isHidden", state == ThiefState.Hidden);
        }

        private void SafeBool(string param, bool value)
        {
            foreach (var p in _animator.parameters)
                if (p.name == param && p.type == AnimatorControllerParameterType.Bool)
                { _animator.SetBool(param, value); return; }
        }

        private void SafeTrigger(string param)
        {
            foreach (var p in _animator.parameters)
                if (p.name == param && p.type == AnimatorControllerParameterType.Trigger)
                { _animator.SetTrigger(param); return; }
        }

        private void CheckEggReached()
        {
            if (eggObjective == null || _reachedEgg) return;
            if (_currentState == ThiefState.Hidden || _currentState == ThiefState.Alert) return;
            if (Vector3.Distance(transform.position, eggObjective.position) <= reachDistance)
            {
                _reachedEgg = true;
                Debug.Log("[Thief] EGG REACHED — WIN!");
                _agent.isStopped = true;
                SetState(ThiefState.Idle);
                OnEggReached?.Invoke();
            }
        }
    }
}