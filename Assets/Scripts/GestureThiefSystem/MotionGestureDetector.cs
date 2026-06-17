// MotionGestureDetector.cs
// Detects motion-based gestures (specifically the RUN forward wave)
// by tracking hand velocity over time.
//
// The XR Hands StaticHandGesture system only handles static poses.
// Motion gestures like "fast repeated forward wave" need this script.
//
// HOW IT WORKS:
//   Tracks the dominant hand's world position each frame.
//   When it detects a rapid forward (Z+) acceleration above the threshold,
//   it counts a "wave". Two waves within the time window fires RUN.
//
// ATTACH TO: Your XR Rig's right-hand controller/anchor transform.
// SET: handTransform to the right-hand XR controller or hand tracking anchor.

using UnityEngine;

namespace GestureThiefSystem
{
    public class MotionGestureDetector : MonoBehaviour
    {
        [Header("Hand to track")]
        [Tooltip("Assign the right-hand controller or hand tracking anchor.")]
        [SerializeField] private Transform handTransform;

        [Header("Wave Detection Settings")]
        [Tooltip("Minimum forward speed (m/s) to count as one wave peak.")]
        [SerializeField] private float waveVelocityThreshold = 1.5f;

        [Tooltip("Two waves within this window (seconds) trigger RUN.")]
        [SerializeField] private float waveWindowSeconds = 1.2f;

        [Tooltip("Minimum seconds between re-triggering RUN.")]
        [SerializeField] private float cooldownSeconds = 1.5f;

        // -- Internal state --
        private Vector3 _lastPosition;
        private float   _lastWaveTime   = -99f;
        private int     _waveCount      = 0;
        private float   _firstWaveTime  = 0f;
        private float   _lastTriggerTime= -99f;

        private void Start()
        {
            if (handTransform == null)
                handTransform = transform;

            _lastPosition = handTransform.position;
        }

        private void Update()
        {
            if (handTransform == null) return;

            Vector3 currentPos = handTransform.position;
            Vector3 velocity   = (currentPos - _lastPosition) / Time.deltaTime;
            _lastPosition = currentPos;

            float forwardSpeed = velocity.z; // Z = forward in world space

            if (forwardSpeed > waveVelocityThreshold)
            {
                float now = Time.time;

                // Ignore if on cooldown
                if (now - _lastTriggerTime < cooldownSeconds) return;

                // Start new wave sequence or add to existing
                if (_waveCount == 0 || now - _firstWaveTime > waveWindowSeconds)
                {
                    _waveCount     = 1;
                    _firstWaveTime = now;
                }
                else if (now - _lastWaveTime > 0.15f) // debounce within same wave
                {
                    _waveCount++;
                }

                _lastWaveTime = now;

                if (_waveCount >= 2)
                {
                    _waveCount       = 0;
                    _lastTriggerTime = now;
                    Debug.Log("[MotionDetector] RUN wave gesture detected.");
                    GestureEventBus.Broadcast(PlayerGesture.Run);
                }
            }
        }
    }
}
