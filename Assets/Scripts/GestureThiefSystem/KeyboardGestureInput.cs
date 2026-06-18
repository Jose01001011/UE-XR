// KeyboardGestureInput.cs (new Input System)
// DEMO KEYS for showing the lecturer without a headset:
//   Key [1] = STOP gesture  (thief stops & hides)
//   Key [2] = GO gesture    (thief crawls toward the egg)
// Only these two are active, matching the two real hand gestures.

using UnityEngine;
using UnityEngine.InputSystem;

namespace GestureThiefSystem
{
    public class KeyboardGestureInput : MonoBehaviour
    {
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // 1 = STOP, 2 = GO  (both main-row and numpad)
            if (kb.digit1Key.wasPressedThisFrame || (kb.numpad1Key != null && kb.numpad1Key.wasPressedThisFrame))
                Broadcast(PlayerGesture.Stop);

            if (kb.digit2Key.wasPressedThisFrame || (kb.numpad2Key != null && kb.numpad2Key.wasPressedThisFrame))
                Broadcast(PlayerGesture.GoForward);
        }

        private void Broadcast(PlayerGesture gesture)
        {
            Debug.Log("[KeyboardInput] DEMO key -> " + gesture);
            GestureEventBus.Broadcast(gesture);
        }
    }
}