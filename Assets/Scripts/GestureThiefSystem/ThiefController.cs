// ThiefController.cs
// Thief NPC: responds to GO (move to egg) and STOP (stop + hide) only.
// Drives the ACTUAL ThiefAnimator controller parameters:
//   StartMoving (Trigger), ReachedEgg (Trigger), WasCaught (Trigger),
//   IsSpottedOrPausing (Bool).

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
        private bool         _hasStartedMoving;

        private NavMeshAgent _agent
        { get { if (_agentCache == null) _agentCache = GetComponent<NavMeshAgent>(); return _agentCache; } }
        private Animator _animator
        { get { if (_animatorCache == null) _animatorCache = GetComponent<Animator>(); return _animatorCache; } }

        public ThiefState CurrentState => _currentState;
        public bool ReachedEgg => _reachedEgg;
        public float DetectionChance =>
            _currentState == ThiefState.Hidden ? hiddenDetectionChance : normalDetectionChance;

        private void Awake() { if (_agent != null) _agent.speed = moveSpeed; }
        private void OnEnable()  { GestureEventBus.OnGesturePerformed += HandleGesture; }
        private void OnDisable() { GestureEventBus.OnGesturePerformed -= HandleGesture; }
        private void Update() { if (_agent.isOnNavMesh) CheckEggReached(); }

        public void Move()
        {
            if (_reachedEgg) return;
            SetState(ThiefState.Moving);
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = false;
            _agent.speed = moveSpeed;
            if (eggObjective != null) _agent.SetDestination(eggObjective.position);
            SafeBool("IsSpottedOrPausing", false);
            if (!_hasStartedMoving) { _hasStartedMoving = true; SafeTrigger("StartMoving"); }
            Debug.Log("[Thief] GO — moving to egg (crawling).");
        }

        public void Stop()
        {
            SetState(ThiefState.Hidden);
            if (_agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
            SafeBool("IsSpottedOrPausing", true);
            Debug.Log("[Thief] STOP — hiding.");
        }

        public void TriggerDetected()
        {
            SetState(ThiefState.Alert);
            if (_agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
            SafeBool("IsSpottedOrPausing", true);
            Debug.Log("[Thief] DETECTED!");
            OnDetected?.Invoke();
        }

        public void TriggerCaught() { SafeTrigger("WasCaught"); }

        private void HandleGesture(PlayerGesture gesture)
        {
            switch (gesture)
            {
                case PlayerGesture.GoForward: Move(); break;
                case PlayerGesture.Stop:      Stop(); break;
            }
        }

        private void SetState(ThiefState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
        }

        private void SafeBool(string param, bool value)
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
                if (p.name == param && p.type == AnimatorControllerParameterType.Bool)
                { _animator.SetBool(param, value); return; }
        }

        private void SafeTrigger(string param)
        {
            if (_animator == null) return;
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
                if (_agent.isOnNavMesh) _agent.isStopped = true;
                SetState(ThiefState.Idle);
                SafeTrigger("ReachedEgg");
                OnEggReached?.Invoke();
            }
        }
    }
}