// ThiefAI.cs
// The thief's complete brain: FSM + movement + animation + color feedback.
//
// Color system: Uses per-renderer Unlit/Color material instances created at runtime.
// This ensures colors are VIVID and LIGHTING-INDEPENDENT (no more white-washing).
// Unlit/Color ignores scene lighting entirely — what you set is what you see.
//
// State flow (per spec):
//   Idle (look around)
//     -- GO  --> Moving (get down -> crawl toward nest)
//     -- STOP --> Hidden (freeze in crawl pose, ostrich can't see)
//     -- GO again --> resume crawl from same spot
//   At nest: PickingUpEgg (grab) -> Running (run to safe zone)
//   3 hits  --> Attacked -> defeated
//   Safe zone with egg --> Victory
//
// Colors: Yellow=Idle, Green=Moving/Running, Purple=Hidden, Red=Attacked, Blue=Victory
//
// Animator params used (clean controller built for this):
//   bool  IsMoving       (crawl loop)
//   bool  IsRunning      (egg run)
//   trig  GetDown        (kneel/crouch into crawl)
//   trig  Grab           (pick up egg)
//   trig  Reset
// All animator calls are Safe (no errors if a param is absent).

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace OstrichHeist
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ThiefAI : NpcBase
    {
        [Header("Targets")]
        [SerializeField] private Transform nest;       // where the egg is
        [SerializeField] private Transform safeZone;   // win location

        [Header("Egg (physical pickup)")]
        [Tooltip("The egg GameObject that sits in the nest and gets carried.")]
        [SerializeField] private Transform egg;
        [Tooltip("Bone/transform to attach the egg to (e.g. mixamorig:RightHand).")]
        [SerializeField] private Transform eggAttachPoint;
        [SerializeField] private Vector3 eggLocalPos = new Vector3(0.05f, 0.02f, 0.03f);
        [SerializeField] private Vector3 eggLocalScale = new Vector3(0.5f, 0.5f, 0.5f);

        [Header("Movement")]
        [SerializeField] private float crawlSpeed = 1.0f;
        [SerializeField] private float runSpeed   = 2.6f;
        [SerializeField] private float reachDistance = 1.1f;

        public ThiefState State { get; private set; } = ThiefState.Idle;
        public bool IsHidden => State == ThiefState.Hidden;
        public bool HasEgg   { get; private set; }

        private NavMeshAgent _agent;
        private Transform _eggHomeParent;
        private Vector3 _eggHomePos, _eggHomeScale;
        private Quaternion _eggHomeRot;
        private float _attackedFlashUntil;
        private bool  _carryingToSafe;

        protected override void Awake()
        {
            base.Awake();
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = crawlSpeed;


            // NOTE: The thief BODY keeps its real textured material (M_Character).
            // State color is shown on the PLUMBOB (ThiefIndicator), not the body.

            // Remember the egg's resting place for reset-on-defeat
            if (egg != null)
            {
                _eggHomeParent = egg.parent;
                _eggHomePos    = egg.position;
                _eggHomeRot    = egg.rotation;
                _eggHomeScale  = egg.localScale;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGoGesture   += StartMovement;
            GameEvents.OnStopGesture += Hide;
        }
        private void OnDisable()
        {
            GameEvents.OnGoGesture   -= StartMovement;
            GameEvents.OnStopGesture -= Hide;
        }



        private void Start()
        {
            EnterState(ThiefState.Idle);
        }

        private void Update()
        {
            // Attacked flash auto-clears back to the underlying state colour
            if (_attackedFlashUntil > 0f && Time.time >= _attackedFlashUntil)
            {
                _attackedFlashUntil = 0f;
                ApplyStateColor();
            }

            if (!_agent.isOnNavMesh) return;

            // Arrival checks while moving
            if (State == ThiefState.Moving && !HasEgg && nest != null)
            {
                if (Vector3.Distance(transform.position, nest.position) <= reachDistance)
                    PickUpEgg();
            }
            else if (State == ThiefState.Running && HasEgg && safeZone != null)
            {
                if (Vector3.Distance(transform.position, safeZone.position) <= reachDistance)
                    WinGame();
            }
        }

        // ---------------- PUBLIC API (per spec) ----------------
        public void StartMovement()
        {
            if (State == ThiefState.Victory || State == ThiefState.Attacked) return;

            if (HasEgg)
            {
                RunToSafeZone();
                return;
            }
            // From Idle or Hidden -> begin/resume crawl toward nest
            EnterState(ThiefState.Moving);
            SetTriggerSafe("GetDown");
            SetBoolSafe("IsMoving", true);
            SetBoolSafe("IsRunning", false);
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.speed = crawlSpeed;
                if (nest != null) _agent.SetDestination(nest.position);
            }
            Log("StartMovement -> crawling to nest");
        }

        public void StopMovement() => Hide();

        public void Hide()
        {
            if (State == ThiefState.Victory || State == ThiefState.Attacked) return;

            if (_agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }

            if (HasEgg)
            {
                // RUN_WITH_EGG -> CRAWL_WITH_EGG: stay low, keep egg, do NOT stand
                EnterState(ThiefState.Hidden);
                SetBoolSafe("IsRunning", false);
                SetBoolSafe("IsMoving", false);
                SetTriggerSafe("GetDown");
                GameEvents.RaiseThiefHidden();
                Log("STOP with egg -> crawl with egg (low, protected)");
            }
            else
            {
                EnterState(ThiefState.Hidden);
                SetBoolSafe("IsMoving", false);
                GameEvents.RaiseThiefHidden();
                Log("Hide -> frozen in crawl, hidden from ostrich");
            }
        }

        public void ResumeMovement() => StartMovement();

        public void PickUpEgg()
        {
            if (HasEgg) return;
            HasEgg = true;
            AttachEgg();
            EnterState(ThiefState.PickingUpEgg);
            if (_agent.isOnNavMesh) _agent.isStopped = true;
            SetBoolSafe("IsMoving", false);
            SetTriggerSafe("Grab");
            GameEvents.RaiseEggPickedUp();
            Log("PickUpEgg -> grabbing");
            Invoke(nameof(RunToSafeZone), 1.2f);
        }

        public void RunToSafeZone()
        {
            if (State == ThiefState.Victory || State == ThiefState.Attacked) return;
            EnterState(ThiefState.Running);
            SetBoolSafe("IsMoving", false);
            SetBoolSafe("IsRunning", true);
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.speed = runSpeed;
                if (safeZone != null) _agent.SetDestination(safeZone.position);
            }
            Log("RunToSafeZone -> running with egg");
        }

        public void TakeDamage()
        {
            if (State == ThiefState.Victory) return;
            _attackedFlashUntil = Time.time + 0.4f;
            // Tell the plumbob to flash red (it handles the visual).
            GameEvents.RaiseThiefState(ThiefState.Attacked);
            Log("TakeDamage -> plumbob red flash");
        }

        public void Defeated()
        {
            EnterState(ThiefState.Attacked);
            DropEgg();
            if (_agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
            SetBoolSafe("IsMoving", false);
            SetBoolSafe("IsRunning", false);
            Log("Defeated -> game over");
        }

        public void WinGame()
        {
            EnterState(ThiefState.Victory);
            if (_agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
            SetBoolSafe("IsMoving", false);
            SetBoolSafe("IsRunning", false);
            GameEvents.RaiseSafeZoneReached();
            Log("WinGame -> victory!");
        }

        // Reset full state for Try Again
        public void ResetThief(Vector3 startPos, Quaternion startRot)
        {
            CancelInvoke();
            HasEgg = false;
            _attackedFlashUntil = 0f;
            _carryingToSafe = false;
            if (_agent.isOnNavMesh) { _agent.isStopped = false; _agent.Warp(startPos); }
            transform.rotation = startRot;
            SetTriggerSafe("Reset");
            SetBoolSafe("IsMoving", false);
            SetBoolSafe("IsRunning", false);
            ResetEgg();
            State = ThiefState.Idle;
            ApplyStateColor();
            Log("Thief reset to Idle");
        }

        // ---------------- STATE / COLOR ----------------
        private void EnterState(ThiefState next)
        {
            if (State == next) return;
            Log(State + " -> " + next);
            State = next;
            ApplyStateColor();
        }

        // Broadcasts the current state so the PLUMBOB (ThiefIndicator) can recolor.
        // The thief body itself is never recolored — it keeps its textured material.
        private void ApplyStateColor()
        {
            GameEvents.RaiseThiefState(State);
        }



        // ---------------- EGG HELPERS ----------------
        private void AttachEgg()
        {
            if (egg == null) return;
            Transform hand = eggAttachPoint != null ? eggAttachPoint : transform;
            var rb = egg.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }
            var col = egg.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            egg.SetParent(hand, true);
            egg.localPosition = eggLocalPos;
            egg.localRotation = Quaternion.identity;
            egg.localScale = eggLocalScale;
            Log("Egg attached to " + hand.name);
        }

        private void DropEgg()
        {
            if (egg == null) return;
            egg.SetParent(_eggHomeParent, true);
            egg.position = transform.position + Vector3.up * 0.3f;
            egg.localScale = _eggHomeScale;
            var col = egg.GetComponent<Collider>();
            if (col != null) col.enabled = true;
            var rb = egg.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = false; rb.detectCollisions = true; }
            HasEgg = false;
            Log("Egg dropped at defeat spot.");
        }

        private void ResetEgg()
        {
            if (egg == null) return;
            var rb = egg.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }
            var col = egg.GetComponent<Collider>();
            if (col != null) col.enabled = true;
            egg.SetParent(_eggHomeParent, true);
            egg.position    = _eggHomePos;
            egg.rotation    = _eggHomeRot;
            egg.localScale  = _eggHomeScale;
            if (rb != null) { rb.isKinematic = false; rb.detectCollisions = true; }
            HasEgg = false;
            Log("Egg reset to nest");
        }

        // Allow GameManager to wire targets if needed
        public void SetTargets(Transform nestT, Transform safeZoneT) { nest = nestT; safeZone = safeZoneT; }
        public void SetEgg(Transform e, Transform attach) { egg = e; eggAttachPoint = attach; }
    }
}
