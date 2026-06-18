// GestureDebugUI.cs
// On-screen debug overlay showing:
//   - Last player gesture performed
//   - Current thief NPC state
//   - Detection chance %
//   - Keyboard shortcut reminder
//
// Attach to a Canvas > Panel GameObject.
// Assign the Text components in the Inspector.
// Disable or hide in final build.

using UnityEngine;
using UnityEngine.UI;

namespace GestureThiefSystem
{
    public class GestureDebugUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ThiefController thief;

        [Header("Text Fields")]
        [SerializeField] private Text lastGestureText;
        [SerializeField] private Text thiefStateText;
        [SerializeField] private Text detectionChanceText;

        [Header("Settings")]
        [SerializeField] private float flashDuration = 0.8f;

        private float _flashTimer = 0f;

        private void OnEnable()
        {
            GestureEventBus.OnGesturePerformed += OnGesture;
        }

        private void OnDisable()
        {
            GestureEventBus.OnGesturePerformed -= OnGesture;
        }

        private void Update()
        {
            if (thief == null) return;

            // Update thief state and detection
            if (thiefStateText      != null)
                thiefStateText.text      = $"Thief: {thief.CurrentState}";
            if (detectionChanceText != null)
                detectionChanceText.text = $"Detection: {thief.DetectionChance * 100:0}%";

            // Flash last gesture label
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && lastGestureText != null)
                    lastGestureText.text = "Last Gesture: --";
            }
        }

        private void OnGesture(PlayerGesture gesture)
        {
            if (lastGestureText == null) return;
            lastGestureText.text = $"Last Gesture: {gesture}";
            _flashTimer = flashDuration;
        }
    }
}
