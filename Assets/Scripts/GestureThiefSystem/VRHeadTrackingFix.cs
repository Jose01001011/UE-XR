// VRHeadTrackingFix.cs
// Ensures the camera's TrackedPoseDriver receives HMD head pose so the player
// can look around in VR. The new Input System TrackedPoseDriver needs its
// position/rotation InputActions bound to the XR HMD center eye pose; if those
// are missing the head never rotates. This sets them up at runtime, reliably.
//
// Put this on the Main Camera (same object as the TrackedPoseDriver).

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

namespace GestureThiefSystem
{
    [DefaultExecutionOrder(-100)]
    public class VRHeadTrackingFix : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        private void Awake()
        {
            var tpd = GetComponent<TrackedPoseDriver>();
            if (tpd == null)
            {
                tpd = gameObject.AddComponent<TrackedPoseDriver>();
                Debug.Log("[VRHeadTrackingFix] Added TrackedPoseDriver.");
            }

            // Build HMD pose actions
            var posAction = new InputAction("HMDPosition", InputActionType.Value, expectedControlType: "Vector3");
            posAction.AddBinding("<XRHMD>/centerEyePosition");
            posAction.AddBinding("<XRHMD>/devicePosition"); // fallback

            var rotAction = new InputAction("HMDRotation", InputActionType.Value, expectedControlType: "Quaternion");
            rotAction.AddBinding("<XRHMD>/centerEyeRotation");
            rotAction.AddBinding("<XRHMD>/deviceRotation"); // fallback

            posAction.Enable();
            rotAction.Enable();

            tpd.positionInput = new InputActionProperty(posAction);
            tpd.rotationInput = new InputActionProperty(rotAction);
            tpd.trackingType  = TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.updateType    = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            Debug.Log("[VRHeadTrackingFix] HMD head pose bound — look-around enabled.");
        }
#endif
    }
}
