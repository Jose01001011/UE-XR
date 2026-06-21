// SignalerAI.cs
// Signaler NPC: continuously "looks around" and waves when danger is signalled.
// FSM: Idle -> LookingAround -> Alerting -> Cooldown -> LookingAround
// Listens to GameEvents (OnThiefDetected, OnDangerNearNest). Cooldown prevents spam.
//
// Animator params (clean controller):
//   bool  IsLooking   (loop look-around)
//   trig  Wave        (hand-wave one-shot)

using UnityEngine;

namespace OstrichHeist
{
    public class SignalerAI : NpcBase
    {
        [Header("Signal")]
        [Tooltip("Seconds the wave plays before returning to look-around.")]
        [SerializeField] private float waveDuration = 2.0f;
        [Tooltip("Minimum seconds between signals (anti-spam).")]
        [SerializeField] private float cooldown = 3.0f;

        public SignalerState State { get; private set; } = SignalerState.Idle;

        private float _stateTimer;
        private float _lastSignalTime = -999f;

        private void OnEnable()
        {
            GameEvents.OnThiefDetected  += SignalDanger;
            GameEvents.OnDangerNearNest += SignalDanger;
        }
        private void OnDisable()
        {
            GameEvents.OnThiefDetected  -= SignalDanger;
            GameEvents.OnDangerNearNest -= SignalDanger;
        }

        private void Start()
        {
            StartLookingAround();
        }

        private void Update()
        {
            switch (State)
            {
                case SignalerState.Alerting:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f) EnterCooldown();
                    break;
                case SignalerState.Cooldown:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f) StartLookingAround();
                    break;
            }
        }

        // ---------------- PUBLIC API ----------------
        public void StartLookingAround()
        {
            EnterState(SignalerState.LookingAround);
            SetBoolSafe("IsLooking", true);
        }

        public void SignalDanger()
        {
            // Cooldown gate prevents repeated/stacked triggers
            if (State == SignalerState.Alerting) return;
            if (Time.time - _lastSignalTime < cooldown) return;

            _lastSignalTime = Time.time;
            EnterState(SignalerState.Alerting);
            SetBoolSafe("IsLooking", false);
            SetTriggerSafe("Wave");
            _stateTimer = waveDuration;
            Log("SignalDanger -> waving");
        }

        public void StopSignal() => StartLookingAround();

        public void ResetSignal()
        {
            _lastSignalTime = -999f;
            StartLookingAround();
        }

        private void EnterCooldown()
        {
            EnterState(SignalerState.Cooldown);
            SetBoolSafe("IsLooking", true); // resume look-around visually during cooldown
            _stateTimer = cooldown;
        }

        private void EnterState(SignalerState next)
        {
            if (State == next) return;
            Log(State + " -> " + next);
            State = next;
        }
    }
}
