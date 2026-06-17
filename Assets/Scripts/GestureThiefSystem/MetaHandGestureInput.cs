// MetaHandGestureInput.cs
// Detects VR hand gestures using Meta All-in-One SDK (OVRHand + OVRSkeleton).
//
// PLACE ON: each hand anchor that already has OVRHand + OVRSkeleton.
//   XR rig > OVRCameraRig > TrackingSpace > LeftHandAnchor  (or RightHandAnchor)
//   Both hands are detected — first one to trigger wins.
//
// GESTURE MAPPING:
//   Thumbs-up  (thumb extended upward, other fingers curled) → GO FORWARD
//   Open palm  (all fingers extended, hand raised)           → STOP
//
// TUNING: adjust the sliders in the Inspector.

using UnityEngine;

namespace GestureThiefSystem
{
    public class MetaHandGestureInput : MonoBehaviour
    {
        [Header("Hold / Cooldown")]
        [Tooltip("Seconds to hold pose before triggering — avoids accidental fires.")]
        [SerializeField] private float holdTime = 0.30f;

        [Tooltip("Seconds before the same gesture can fire again.")]
        [SerializeField] private float cooldown = 1.20f;

        [Header("Thumbs-Up Thresholds")]
        [Tooltip("Min world-Y gap: thumb tip above index tip. Raise if false-positives.")]
        [SerializeField] private float thumbsUpYGap = 0.05f;

        [Tooltip("Max world-Y: index/middle tip can be ABOVE the wrist before we stop treating them as curled.")]
        [SerializeField] private float curledAboveWristTolerance = 0.04f;

        [Header("Open-Palm Threshold")]
        [Tooltip("Min distance (metres) from wrist to EACH finger tip = fingers extended.")]
        [SerializeField] private float openPalmExtension = 0.075f;

        // ----------------------------------------------------------------
        private OVRHand     _hand;
        private OVRSkeleton _skel;

        private float _thumbsUpHeld;
        private float _stopHeld;
        private float _cooldownLeft;

        // ----------------------------------------------------------------
        private void Start()
        {
            _hand = GetComponent<OVRHand>();
            _skel = GetComponent<OVRSkeleton>();

            if (_hand == null || _skel == null)
                Debug.LogWarning($"[MetaGesture] {name} needs OVRHand + OVRSkeleton on the same GameObject.", this);
        }

        private void Update()
        {
            if (_hand == null || !_hand.IsTracked) { ResetTimers(); return; }
            // Bones might not be ready yet in the first few frames
            if (_skel.Bones == null || _skel.Bones.Count == 0) return;

            _cooldownLeft = Mathf.Max(0f, _cooldownLeft - Time.deltaTime);

            bool thumbUp  = CheckThumbsUp();
            bool openPalm = !thumbUp && CheckOpenPalm();

            // --- GO (thumbs-up) ---
            if (thumbUp)
            {
                _stopHeld = 0f;
                _thumbsUpHeld += Time.deltaTime;
                if (_thumbsUpHeld >= holdTime && _cooldownLeft <= 0f)
                {
                    Fire(PlayerGesture.GoForward, "thumbs-up → GO");
                    _thumbsUpHeld = 0f;
                }
            }
            // --- STOP (open palm) ---
            else if (openPalm)
            {
                _thumbsUpHeld = 0f;
                _stopHeld += Time.deltaTime;
                if (_stopHeld >= holdTime && _cooldownLeft <= 0f)
                {
                    Fire(PlayerGesture.Stop, "open palm → STOP");
                    _stopHeld = 0f;
                }
            }
            else
            {
                ResetTimers();
            }
        }

        // ----------------------------------------------------------------
        // THUMBS-UP: thumb tip clearly above index tip in world Y;
        //            index + middle tips at or below wrist height.
        // ----------------------------------------------------------------
        private bool CheckThumbsUp()
        {
            Vector3 thumbTip  = Bone(OVRSkeleton.BoneId.Hand_ThumbTip);
            Vector3 indexTip  = Bone(OVRSkeleton.BoneId.Hand_IndexTip);
            Vector3 middleTip = Bone(OVRSkeleton.BoneId.Hand_MiddleTip);
            Vector3 wrist     = Bone(OVRSkeleton.BoneId.Hand_WristRoot);

            if (thumbTip == transform.position) return false; // bone missing

            bool thumbHigher  = (thumbTip.y - indexTip.y) >= thumbsUpYGap;
            bool indexCurled  = indexTip.y  <= wrist.y + curledAboveWristTolerance;
            bool middleCurled = middleTip.y <= wrist.y + curledAboveWristTolerance;

            return thumbHigher && indexCurled && middleCurled;
        }

        // ----------------------------------------------------------------
        // OPEN PALM: all four finger tips are far enough from the wrist.
        // ----------------------------------------------------------------
        private bool CheckOpenPalm()
        {
            Vector3 wrist     = Bone(OVRSkeleton.BoneId.Hand_WristRoot);
            Vector3 indexTip  = Bone(OVRSkeleton.BoneId.Hand_IndexTip);
            Vector3 middleTip = Bone(OVRSkeleton.BoneId.Hand_MiddleTip);
            Vector3 ringTip   = Bone(OVRSkeleton.BoneId.Hand_RingTip);
            Vector3 pinkyTip  = Bone(OVRSkeleton.BoneId.Hand_PinkyTip);

            if (wrist == indexTip) return false; // bones missing

            float ext = openPalmExtension;
            return Vector3.Distance(indexTip,  wrist) > ext &&
                   Vector3.Distance(middleTip, wrist) > ext &&
                   Vector3.Distance(ringTip,   wrist) > ext &&
                   Vector3.Distance(pinkyTip,  wrist) > ext;
        }

        // ----------------------------------------------------------------
        private Vector3 Bone(OVRSkeleton.BoneId id)
        {
            foreach (var b in _skel.Bones)
                if (b.Id == id) return b.Transform.position;
            return transform.position; // sentinel = bone not found
        }

        private void Fire(PlayerGesture gesture, string label)
        {
            _cooldownLeft = cooldown;
            Debug.Log($"[MetaGesture:{name}] {label}");
            GestureEventBus.Broadcast(gesture);
        }

        private void ResetTimers()
        {
            _thumbsUpHeld = 0f;
            _stopHeld     = 0f;
        }
    }
}
