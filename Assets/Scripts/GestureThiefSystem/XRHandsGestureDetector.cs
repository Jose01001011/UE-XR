// XRHandsGestureDetector.cs
// STOP / GO gesture detection using Unity XR Hands (com.unity.xr.hands).
// Reads XRHandSubsystem joint data each frame. Works once OpenXR Hand Tracking
// is enabled. NO Gestures sample or pose assets needed.
//
//   STOP = open palm  -> 4 fingers extended            -> PlayerGesture.Stop
//   GO   = thumbs-up  -> thumb extended, 4 fingers curled -> PlayerGesture.GoForward
//
// Robust curl detection: a finger is 'extended' when its TIP is farther from the
// WRIST than its middle knuckle is — this auto-scales to any hand size, unlike a
// fixed centimetre threshold.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace GestureThiefSystem
{
    public class XRHandsGestureDetector : MonoBehaviour
    {
        [Header("Hands")]
        [Tooltip("Detect on EITHER hand (recommended). If false, uses the single hand below.")]
        [SerializeField] private bool useEitherHand = true;
        [SerializeField] private bool useRightHand = true;

        [Header("Tuning")]
        [Tooltip("Seconds the pose must be held before it fires once.")]
        [SerializeField] private float holdTime = 0.35f;
        [Tooltip("Tip-vs-knuckle margin (metres). Higher = must extend more to count.")]
        [SerializeField] private float extendMargin = 0.02f;

        [Header("Debug")]
        [Tooltip("Logs finger states + fires to the Console / device logcat.")]
        [SerializeField] private bool logDebug = true;

        private XRHandSubsystem _subsystem;
        private readonly List<XRHandSubsystem> _subsystems = new List<XRHandSubsystem>();
        private PlayerGesture _candidate = PlayerGesture.None;
        private float _holdTimer;
        private PlayerGesture _lastFired = PlayerGesture.None;
        private float _logThrottle;

        private void Update()
        {
            EnsureSubsystem();
            if (_subsystem == null || !_subsystem.running) return;

            PlayerGesture detected = PlayerGesture.None;

            if (useEitherHand)
            {
                detected = ClassifyHand(_subsystem.rightHand);
                if (detected == PlayerGesture.None)
                    detected = ClassifyHand(_subsystem.leftHand);
            }
            else
            {
                detected = ClassifyHand(useRightHand ? _subsystem.rightHand : _subsystem.leftHand);
            }

            if (detected == _candidate && detected != PlayerGesture.None)
            {
                _holdTimer += Time.deltaTime;
                if (_holdTimer >= holdTime && _lastFired != detected)
                {
                    Fire(detected);
                    _lastFired = detected;
                }
            }
            else
            {
                _candidate = detected;
                _holdTimer = 0f;
                if (detected != _lastFired) _lastFired = PlayerGesture.None;
            }
        }

        private PlayerGesture ClassifyHand(XRHand hand)
        {
            if (!hand.isTracked) return PlayerGesture.None;
            if (!TryJoint(hand, XRHandJointID.Wrist, out Vector3 wrist)) return PlayerGesture.None;

            bool index  = FingerExtended(hand, wrist, XRHandJointID.IndexProximal,  XRHandJointID.IndexTip);
            bool middle = FingerExtended(hand, wrist, XRHandJointID.MiddleProximal, XRHandJointID.MiddleTip);
            bool ring   = FingerExtended(hand, wrist, XRHandJointID.RingProximal,   XRHandJointID.RingTip);
            bool little = FingerExtended(hand, wrist, XRHandJointID.LittleProximal, XRHandJointID.LittleTip);
            bool thumb  = FingerExtended(hand, wrist, XRHandJointID.ThumbProximal,  XRHandJointID.ThumbTip);

            bool fourExtended = index && middle && ring && little;
            bool fourCurled   = !index && !middle && !ring && !little;

            if (logDebug && Time.time - _logThrottle > 0.5f)
            {
                _logThrottle = Time.time;
                Debug.Log($"[XRHands] {hand.handedness} T:{thumb} I:{index} M:{middle} R:{ring} L:{little}");
            }

            if (fourExtended) return PlayerGesture.Stop;        // open palm
            if (thumb && fourCurled) return PlayerGesture.GoForward; // thumbs-up
            return PlayerGesture.None;
        }

        // Extended when the TIP is farther from the wrist than the knuckle (+margin).
        private bool FingerExtended(XRHand hand, Vector3 wrist, XRHandJointID knuckle, XRHandJointID tip)
        {
            if (!TryJoint(hand, knuckle, out Vector3 k)) return false;
            if (!TryJoint(hand, tip, out Vector3 t)) return false;
            float dKnuckle = Vector3.Distance(wrist, k);
            float dTip     = Vector3.Distance(wrist, t);
            return dTip > dKnuckle + extendMargin;
        }

        private bool TryJoint(XRHand hand, XRHandJointID id, out Vector3 pos)
        {
            pos = Vector3.zero;
            var j = hand.GetJoint(id);
            if (j.TryGetPose(out Pose p)) { pos = p.position; return true; }
            return false;
        }

        private void EnsureSubsystem()
        {
            if (_subsystem != null && _subsystem.running) return;
            SubsystemManager.GetSubsystems(_subsystems);
            if (_subsystems.Count > 0) _subsystem = _subsystems[0];
        }

        private void Fire(PlayerGesture g)
        {
            Debug.Log("[XRHands] Gesture FIRED: " + g);
            // New event bus (OstrichHeist architecture)
            if (g == PlayerGesture.GoForward) OstrichHeist.GameEvents.RaiseGo();
            else if (g == PlayerGesture.Stop) OstrichHeist.GameEvents.RaiseStop();
        }
    }
}