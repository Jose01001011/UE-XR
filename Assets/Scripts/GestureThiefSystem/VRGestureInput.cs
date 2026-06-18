// VRGestureInput.cs
// VR gesture detection layer using Unity XR Hands (com.unity.xr.hands).
//
// SETUP REQUIREMENTS:
//   1. Install packages via Package Manager:
//      - XR Interaction Toolkit  (com.unity.xr.interaction.toolkit) >= 3.0
//      - XR Hands                (com.unity.xr.hands) >= 1.4
//      - OpenXR Plugin           (com.unity.xr.openxr)
//   2. Import the "Gestures" sample from XR Hands in Package Manager.
//   3. Add an XRHandTrackingEvents component to each hand in your XR Rig.
//   4. Create HandShape/HandPose assets for each gesture (see docs below).
//   5. Wire the StaticHandGesture components to call OnGestureDetected().
//
// DOCS:
//   https://docs.unity3d.com/Packages/com.unity.xr.hands@1.4/manual/gestures/custom-gestures.html
//   https://blog.learnxr.io/xr-development/xr-hands-custom-gestures-now-available
//
// FINGER SHAPE REFERENCE (for HandShape assets):
//   Open palm (STOP)      -> all fingers FullCurl = 0, Spread = 0.5
//   Point forward (GO)    -> index FullCurl = 0, others FullCurl = 1
//   Downward flat (CROUCH)-> all fingers FullCurl = 0, palm facing down
//   Stay low (HIDE)       -> all fingers FullCurl = 0, palm facing down, hand lowered
//   Wave forward (RUN)    -> uses motion detection (see MotionGestureDetector below)
//
// This script acts as the bridge: when a StaticHandGesture fires its
// UnityEvent, call the matching method here to broadcast to GestureEventBus.

using UnityEngine;

namespace GestureThiefSystem
{
    /// <summary>
    /// Wire each method to the corresponding StaticHandGesture UnityEvent
    /// (Gesture Performed) in the Inspector.
    /// </summary>
    public class VRGestureInput : MonoBehaviour
    {
        [Header("Optional: Disable keyboard input when VR is active")]
        [SerializeField] private KeyboardGestureInput keyboardFallback;

        private void OnEnable()
        {
            if (keyboardFallback != null)
                keyboardFallback.enabled = false;
        }

        // -- Called by StaticHandGesture UnityEvents in the Inspector --

        /// <summary>Open palm facing outward -> STOP</summary>
        public void OnStopGesture()     => Broadcast(PlayerGesture.Stop);

        /// <summary>Index finger pointing forward -> GO FORWARD</summary>
        public void OnGoForwardGesture()=> Broadcast(PlayerGesture.GoForward);

        /// <summary>Flat hand pressed downward -> CROUCH</summary>
        public void OnCrouchGesture()   => Broadcast(PlayerGesture.Crouch);

        /// <summary>Hand flat and lowered, palm down -> HIDE</summary>
        public void OnHideGesture()     => Broadcast(PlayerGesture.Hide);

        /// <summary>Rapid forward wave detected by MotionGestureDetector -> RUN</summary>
        public void OnRunGesture()      => Broadcast(PlayerGesture.Run);

        // ----

        private void Broadcast(PlayerGesture gesture)
        {
            Debug.Log($"[VRInput] Gesture broadcast: {gesture}");
            GestureEventBus.Broadcast(gesture);
        }
    }
}
