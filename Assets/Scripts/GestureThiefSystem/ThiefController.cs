// ThiefController.cs
// Core NPC controller for the Thief character.
//
// Implements the full state machine defined in the design document:
//   Idle -> Moving -> Crouching -> Hidden -> Running -> Alert
//
// Listens to GestureEventBus and transitions states accordingly.
// Uses Unity NavMeshAgent for pathfinding to the egg objective.
// Uses Animator for state-driven animations.
//
// ANIMATOR SETUP:
//   Create an Animator Controller with these bool/trigger parameters:
//     isMoving   (Bool)
//     isCrouching(Bool)
//     isHidden   (Bool)
//     isRunning  (Bool)
//     isAlert    (Bool)
//   Transitions: driven by the parameters set in SetAnimatorState().
//
// NAVMESH SETUP:
//   Bake a NavMesh on your scene geometry.
//   Assign eggObjective to the egg target transform.

using UnityEngine;
using UnityEngine.AI;

namespace GestureThiefSystem
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class ThiefController : MonoBehaviour
    {
        // -- Inspector References --
        [Header("Navigation")]
        [Tooltip("The egg the thief is trying to steal.")]
        [SerializeField] private Transform eggObjective;

        [Tooltip("Distance from egg considered 'reached'.")]
        [SerializeField] private float reachDistance = 0.5f;

        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed    = 2.0f;
        [SerializeField] private float crouchSpeed  = 0.8f;
        [SerializeField] private float runSpeed     = 5.0f;

        [Header("Detection Chances (0-1)")]
        [SerializeField] private float normalDetectionChance  = 1.0f;
        [SerializeField] private float crouchDetectionChance  = 0.5f;
        [SerializeField] private float hiddenDetectionChance  = 0.05f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnEggReached;
        public UnityEngine.Events.UnityEvent OnDetected;

        // -- Internal --
        private NavMeshAgent _agent;
        private Animator     _animator;
        private ThiefState   _currentState = ThiefState.Idle;

        public ThiefState CurrentState => _currentState;

        /// <summary>Current detection multiplier based on active state.</summary>
        public float DetectionChance
        {
            get
            {
                return _currentState switch
                {
                    ThiefState.Crouching => crouchDetectionChance,
                    ThiefState.Hidden    => hiddenDetectionChance,
                    _                    => normalDetectionChance
                };
            }
        }

        // -- Unity Lifecycle --
        private void Awake()
        {
            _agent    = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            GestureEventBus.OnGesturePerformed += HandleGesture;
        }

        private void OnDisable()
        {
            GestureEventBus.OnGesturePerformed -= HandleGesture;
        }

        private void Update()
        {
            CheckEggReached();
        }

        // -- Public API (matches design document mapping) --

        /// <summary>STOP gesture -> freeze, enter alert idle.</summary>
        public void Stop()
        {
            SetState(ThiefState.Alert);
            _agent.isStopped = true;
            _agent.velocity  = Vector3.zero;
            Debug.Log("[Thief] Stop() -- Alert idle.");
        }

        /// <summary>GO FORWARD gesture -> resume movement to egg.</summary>
        public void Move()
        {
            SetState(ThiefState.Moving);
            _agent.isStopped = false;
            _agent.speed     = walkSpeed;
            if (eggObjective != null)
                _agent.SetDestination(eggObjective.position);
            Debug.Log("[Thief] Move() -- Heading to egg.");
        }

        /// <summary>CROUCH gesture -> reduced visibility, slow movement.</summary>
        public void Crouch()
        {
            SetState(ThiefState.Crouching);
            _agent.isStopped = false;
            _agent.speed     = crouchSpeed;
            if (eggObjective != null)
                _agent.SetDestination(eggObjective.position);
            Debug.Log("[Thief] Crouch() -- Slow and low.");
        }

        /// <summary>HIDE gesture -> fully hidden, completely still.</summary>
        public void Hide()
        {
            SetState(ThiefState.Hidden);
            _agent.isStopped = true;
            _agent.velocity  = Vector3.zero;
            Debug.Log("[Thief] Hide() -- Fully hidden.");
        }

        /// <summary>RUN gesture -> sprint toward egg or escape.</summary>
        public void Run()
        {
            SetState(ThiefState.Running);
            _agent.isStopped = false;
            _agent.speed     = runSpeed;
            if (eggObjective != null)
                _agent.SetDestination(eggObjective.position);
            Debug.Log("[Thief] Run() -- Sprinting!");
        }

        /// <summary>
        /// Called by OstrichDetection when the ostrich catches the thief.
        /// Triggers detection event and stops the thief.
        /// </summary>
        public void TriggerDetected()
        {
            Stop();
            Debug.Log("[Thief] DETECTED! Game over.");
            OnDetected?.Invoke();
        }

        // -- Internal --
        private void HandleGesture(PlayerGesture gesture)
        {
            switch (gesture)
            {
                case PlayerGesture.Stop:      Stop();   break;
                case PlayerGesture.GoForward: Move();   break;
                case PlayerGesture.Crouch:    Crouch(); break;
                case PlayerGesture.Hide:      Hide();   break;
                case PlayerGesture.Run:       Run();    break;
            }
        }

        private void SetState(ThiefState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
            SetAnimatorState(newState);
        }

        private void SetAnimatorState(ThiefState state)
        {
            if (_animator == null) return;

            // Reset all before setting new one
            _animator.SetBool("isMoving",    false);
            _animator.SetBool("isCrouching", false);
            _animator.SetBool("isHidden",    false);
            _animator.SetBool("isRunning",   false);
            _animator.SetBool("isAlert",     false);

            switch (state)
            {
                case ThiefState.Moving:    _animator.SetBool("isMoving",    true); break;
                case ThiefState.Crouching: _animator.SetBool("isCrouching", true); break;
                case ThiefState.Hidden:    _animator.SetBool("isHidden",    true); break;
                case ThiefState.Running:   _animator.SetBool("isRunning",   true); break;
                case ThiefState.Alert:     _animator.SetBool("isAlert",     true); break;
                // Idle: all false -> Animator defaults to idle clip
            }
        }

        private void CheckEggReached()
        {
            if (eggObjective == null) return;
            if (_currentState == ThiefState.Hidden || _currentState == ThiefState.Alert) return;

            float dist = Vector3.Distance(transform.position, eggObjective.position);
            if (dist <= reachDistance)
            {
                Debug.Log("[Thief] Egg reached!");
                _agent.isStopped = true;
                SetState(ThiefState.Idle);
                OnEggReached?.Invoke();
            }
        }
    }
}
