// HeistInput.cs
// Translates player input into the two game gestures via the new GameEvents bus.
//   Keyboard 2 = GO,  Keyboard 1 = STOP  (for the lecturer demo)
//   Also bridges the existing hand-gesture detector (PlayerGesture) if present.
//
// Put this on the XR Origin. Uses the New Input System.

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OstrichHeist
{
    public class HeistInput : MonoBehaviour
    {
        [SerializeField] private bool logPresses = true;

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            {
                if (logPresses) Debug.Log("[HeistInput] 2 -> GO");
                GameEvents.RaiseGo();
            }
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            {
                if (logPresses) Debug.Log("[HeistInput] 1 -> STOP");
                GameEvents.RaiseStop();
            }
#endif
        }

        // Public hooks so the hand-gesture detector can call into the same bus.
        public void OnGoGesture()   => GameEvents.RaiseGo();
        public void OnStopGesture() => GameEvents.RaiseStop();
    }
}
