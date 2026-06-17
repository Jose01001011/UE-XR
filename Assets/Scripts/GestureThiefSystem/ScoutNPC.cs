// ScoutNPC.cs
// The Scout NPC that watches the ostrich and performs warning gestures
// to communicate with the player.
//
// The scout's gestures mirror the correct response gesture the player should perform.
// This creates the asymmetric communication loop:
//   Scout sees danger -> Scout gestures -> Player interprets -> Player gestures -> Thief reacts
//
// ANIMATOR SETUP:
//   Create an Animator Controller with these trigger parameters:
//     GestureStop, GestureGo, GestureCrouch, GestureHide, GestureRun
//   Each trigger plays the corresponding scout arm/hand animation.
//
// CUSTOMIZE:
//   You can extend the TriggerWarning() method with AI logic that reads
//   ostrich behaviour to recommend the most appropriate gesture.

using UnityEngine;

namespace GestureThiefSystem
{
    public class ScoutNPC : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator scoutAnimator;

        [Header("Scout Behaviour")]
        [Tooltip("When thief enters warning zone, which gesture does the scout recommend?")]
        [SerializeField] private PlayerGesture defaultWarningGesture = PlayerGesture.Hide;

        [Header("UI Feedback (optional)")]
        [Tooltip("A world-space canvas or indicator that displays gesture hint to player.")]
        [SerializeField] private GameObject gestureHintUI;

        private PlayerGesture _currentRecommendation = PlayerGesture.None;

        // -- Called by OstrichDetection --

        /// <summary>
        /// Called when the thief enters the ostrich warning zone.
        /// Scout performs the recommended warning gesture.
        /// </summary>
        public void TriggerWarning()
        {
            PerformGesture(defaultWarningGesture);
        }

        /// <summary>Called when the thief leaves the warning zone. Scout relaxes.</summary>
        public void ClearWarning()
        {
            _currentRecommendation = PlayerGesture.None;
            HideGestureHint();
            PlayAnimationTrigger("GestureGo"); // Scout signals safe to move
        }

        // -- Gesture Performance --

        /// <summary>Scout performs a specific gesture to communicate with the player.</summary>
        public void PerformGesture(PlayerGesture gesture)
        {
            _currentRecommendation = gesture;
            ShowGestureHint(gesture);

            string triggerName = gesture switch
            {
                PlayerGesture.Stop      => "GestureStop",
                PlayerGesture.GoForward => "GestureGo",
                PlayerGesture.Crouch    => "GestureCrouch",
                PlayerGesture.Hide      => "GestureHide",
                PlayerGesture.Run       => "GestureRun",
                _                      => ""
            };

            if (!string.IsNullOrEmpty(triggerName))
                PlayAnimationTrigger(triggerName);

            Debug.Log($"[Scout] Performing gesture: {gesture}");
        }

        // -- Internal --
        private void PlayAnimationTrigger(string triggerName)
        {
            if (scoutAnimator == null || string.IsNullOrEmpty(triggerName)) return;
            // Guard: only fire if the controller actually has this parameter.
            foreach (var p in scoutAnimator.parameters)
                if (p.name == triggerName) { scoutAnimator.SetTrigger(triggerName); return; }
            // Parameter missing -> silently skip (no console spam).
        }

        private void ShowGestureHint(PlayerGesture gesture)
        {
            if (gestureHintUI != null)
                gestureHintUI.SetActive(true);
            // You can extend this to display the correct gesture icon/text
        }

        private void HideGestureHint()
        {
            if (gestureHintUI != null)
                gestureHintUI.SetActive(false);
        }
    }
}
