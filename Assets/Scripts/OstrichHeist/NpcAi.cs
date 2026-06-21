// NpcAi.cs
// Shared NPC AI foundation for Ostrich Egg Heist.
// Defines the finite-state enums, a central event bus, and an NPCBase class
// that ThiefAI / OstrichAI / SignalerAI inherit from.
//
// Design goals (per spec):
//   - One state active at a time (enum FSM, not scattered bools)
//   - Event-driven communication (no tight coupling)
//   - Each AI owns its own responsibilities

using System;
using UnityEngine;

namespace OstrichHeist
{
    // ---------------- STATE ENUMS ----------------
    public enum ThiefState   { Idle, Moving, Hidden, PickingUpEgg, Running, Attacked, Victory }
    public enum OstrichState { Idle, Patrolling, Investigating, Chasing, Attacking, Returning }
    public enum SignalerState{ Idle, LookingAround, Alerting, Cooldown }

    // ---------------- CENTRAL EVENT BUS ----------------
    // Decouples systems. Anyone can raise; anyone can listen.
    public static class GameEvents
    {
        // Gesture input
        public static event Action OnGoGesture;
        public static event Action OnStopGesture;
        public static void RaiseGo()   { OnGoGesture?.Invoke(); }
        public static void RaiseStop() { OnStopGesture?.Invoke(); }

        // Gameplay milestones
        public static event Action OnThiefDetected;     // ostrich sees an exposed thief
        public static event Action OnThiefHidden;        // thief became hidden
        public static event Action OnEggPickedUp;        // thief grabbed the egg
        public static event Action OnSafeZoneReached;    // WIN
        public static event Action OnThiefDefeated;      // LOSE (3 hits)
        public static event Action OnDangerNearNest;     // ostrich near nest -> signaler
        public static event Action<ThiefState> OnThiefStateChanged; // for plumbob color

        public static void RaiseThiefDetected()  { OnThiefDetected?.Invoke(); }
        public static void RaiseThiefHidden()     { OnThiefHidden?.Invoke(); }
        public static void RaiseEggPickedUp()     { OnEggPickedUp?.Invoke(); }
        public static void RaiseSafeZoneReached() { OnSafeZoneReached?.Invoke(); }
        public static void RaiseThiefDefeated()   { OnThiefDefeated?.Invoke(); }
        public static void RaiseThiefState(ThiefState s) { OnThiefStateChanged?.Invoke(s); }
        public static void RaiseDangerNearNest()  { OnDangerNearNest?.Invoke(); }

        // Clear all subscriptions (call on scene reload to avoid stale handlers)
        // Runs ONCE before any scene object's Awake/OnEnable, so component
        // subscriptions made in OnEnable are never wiped by a later Reset.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            OnGoGesture = null; OnStopGesture = null;
            OnThiefDetected = null; OnThiefHidden = null; OnEggPickedUp = null;
            OnSafeZoneReached = null; OnThiefDefeated = null; OnDangerNearNest = null;
            OnThiefStateChanged = null;
        }
    }

    // ---------------- NPC BASE ----------------
    // Shared services for all NPC AIs: animator access, debug logging.
    [RequireComponent(typeof(Animator))]
    public abstract class NpcBase : MonoBehaviour
    {
        [Header("NPC Base")]
        [SerializeField] protected bool debugLogs = true;

        protected Animator animator;

        protected virtual void Awake()
        {
            animator = GetComponent<Animator>();
        }

        // Safe animator helpers — never throw if a param is missing.
        protected void SetBoolSafe(string p, bool v)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var prm in animator.parameters)
                if (prm.name == p && prm.type == AnimatorControllerParameterType.Bool)
                { animator.SetBool(p, v); return; }
        }

        protected void SetTriggerSafe(string p)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var prm in animator.parameters)
                if (prm.name == p && prm.type == AnimatorControllerParameterType.Trigger)
                { animator.SetTrigger(p); return; }
        }

        protected void Log(string msg)
        {
            if (debugLogs) Debug.Log("[" + GetType().Name + "] " + msg);
        }
    }
}
