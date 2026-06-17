// XRHandsGestureDetector.cs
// Self-contained STOP / GO gesture detection using Unity XR Hands (com.unity.xr.hands).
//
// Reads the XRHandSubsystem joint data directly each frame — NO Gestures sample,
// NO HandShape/HandPose assets, NO inspector wiring required. Works as long as
// OpenXR Hand Tracking is enabled in XR Plug-in Management.
//
// GESTURES (only two, per design spec):
//   STOP  = open palm  -> all four fingers extended           -> PlayerGesture.Stop
//   GO    = thumbs-up  -> thumb extended, four fingers curled  -> PlayerGesture.GoForward
//
// Each gesture must be HELD steadily for holdTime seconds before it fires once.
// It will not fire again until a different gesture (or no gesture) is seen,
// preventing the auto-spam problem the keyboard input had.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace GestureThiefSystem
{
    public class XRHandsGestureDetector : MonoBehaviour
    {
        [Header("Which hand to read")]
        [Tooltip("Use the right hand for gestures. If false, uses left.")]
        [SerializeField] private bool useRightHand = true;

        [Header("Tuning")]
        [Tooltip("Seconds the pose must be held before the gesture fires.")]
        [SerializeField] private float holdTime = 0.4f;

        [Tooltip("A fingertip farther than this (metres) from the palm counts as EXTENDED.")]
        [SerializeField] private float extendedThreshold = 0.09f;

        [Tooltip("A fingertip closer than this (metres) to the palm counts as CURLED.")]
        [SerializeField] private float curledThreshold = 0.06f;

        [Header("Debug")]
        [SerializeField] private bool logDebug = false;

        // ---- internal ----
        private XRHandSubsystem _subsystem;
        private readonly List<XRHandSubsystem> _subsystems = new List<XRHandSubsystem>();

        private PlayerGesture _candidate = PlayerGesture.None;
        private float         _holdTimer;
        private PlayerGesture _lastFired = PlayerGesture.None;

        private void Update()
        {
            EnsureSubsystem();
            if (_subsystem == null || !_subsystem.running)
                return;

            XRHand hand = useRightHand ? _subsystem.rightHand : _subsystem.leftHand;
            if (!hand.isTracked)
            {
                ResetCandidate();
                return;
            }

            PlayerGesture detected = ClassifyHand(hand);

            // Debounce: require the same pose held for holdTime, fire once.
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
                // When the hand leaves the fired pose, allow that gesture to fire again later.
                if (detected != _lastFired)
                    _lastFired = PlayerGesture.None;
            }
        }

        // ---- gesture classification ----
        private PlayerGesture ClassifyHand(XRHand hand)
        {
            // Palm reference joint
            if (!TryJointPos(hand, XRHandJointID.Palm, out Vector3 palm))
                return PlayerGesture.None;

            bool thumb  = IsFingerExtended(hand, XRHandJointID.ThumbTip,  palm);
            bool index  = IsFingerExtended(hand, XRHandJointID.IndexTip,  palm);
            bool middle = IsFingerExtended(hand, XRHandJointID.MiddleTip, palm);
            bool ring   = IsFingerExtended(hand, XRHandJointID.RingTip,   palm);
            bool little = IsFingerExtended(hand, XRHandJointID.LittleTip, palm);

            bool fourFingersExtended = index && middle && ring && little;
            bool fourFingersCurled   = !index && !middle && !ring && !little;

            if (logDebug)
                Debug.Log($"[XRHands] T:{thumb} I:{index} M:{middle} R:{ring} L:{little}");

            // STOP = open palm (all four fingers out)
            if (fourFingersExtended)
                return PlayerGesture.Stop;

            // GO = thumbs-up (thumb out, four fingers curled)
            if (thumb && fourFingersCurled)
                return PlayerGesture.GoForward;

            return PlayerGesture.None;
        }

        // A finger is extended if its tip is far from the palm; curled if close.
        // Returns true = extended, false = curled (or unknown -> treated as curled).
        private bool IsFingerExtended(XRHand hand, XRHandJointID tip, Vector3 palm)
        {
            if (!TryJointPos(hand, tip, out Vector3 tipPos))
                return false;
            float dist = Vector3.Distance(tipPos, palm);
            if (dist >= extendedThreshold) return true;
            if (dist <= curledThreshold)   return false;
            // In-between: bias toward curled for stability
            return false;
        }

        private bool TryJointPos(XRHand hand, XRHandJointID id, out Vector3 pos)
        {
            pos = Vector3.zero;
            var joint = hand.GetJoint(id);
            if (joint.TryGetPose(out Pose pose))
            {
                pos = pose.position;
                return true;
            }
            return false;
        }

        // ---- subsystem plumbing ----
        private void EnsureSubsystem()
        {
            if (_subsystem != null && _subsystem.running) return;

            SubsystemManager.GetSubsystems(_subsystems);
            if (_subsystems.Count > 0)
                _subsystem = _subsystems[0];
        }

        private void Fire(PlayerGesture gesture)
        {
            Debug.Log("[XRHands] Gesture FIRED: " + gesture);
            GestureEventBus.Broadcast(gesture);
        }

        private void ResetCandidate()
        {
            _candidate = PlayerGesture.None;
            _holdTimer = 0f;
            _lastFired = PlayerGesture.None;
        }
    }
}
