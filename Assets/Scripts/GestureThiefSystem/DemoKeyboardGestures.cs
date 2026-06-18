// DemoKeyboardGestures.cs
// Simple, reliable keyboard trigger for the two gestures, for demos/presentation.
//   Press 2  -> GO   (thief moves to egg)
//   Press 1  -> STOP (thief stops & hides)
// Uses the New Input System (this project is New-Input-System only).
// Broadcasts through the same GestureEventBus the hand gestures use, so the
// thief reacts identically whether triggered by hand or keyboard.

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GestureThiefSystem
{
    public class DemoKeyboardGestures : MonoBehaviour
    {
        [Tooltip("Log each key press to the Console.")]
        [SerializeField] private bool logPresses = true;

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;

            // GO = key 2 (or numpad 2)
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            {
                if (logPresses) Debug.Log("[DemoKeys] 2 pressed -> GO");
                GestureEventBus.Broadcast(PlayerGesture.GoForward);
            }

            // STOP = key 1 (or numpad 1)
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            {
                if (logPresses) Debug.Log("[DemoKeys] 1 pressed -> STOP");
                GestureEventBus.Broadcast(PlayerGesture.Stop);
            }
#endif
        }
    }
}
