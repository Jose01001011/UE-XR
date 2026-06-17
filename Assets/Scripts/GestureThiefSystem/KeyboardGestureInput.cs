// KeyboardGestureInput.cs (new Input System)
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
            if (kb.hKey.wasPressedThisFrame) Broadcast(PlayerGesture.Hide);
            if (kb.sKey.wasPressedThisFrame) Broadcast(PlayerGesture.Stop);
            if (kb.gKey.wasPressedThisFrame) Broadcast(PlayerGesture.GoForward);
            if (kb.cKey.wasPressedThisFrame) Broadcast(PlayerGesture.Crouch);
            if (kb.rKey.wasPressedThisFrame) Broadcast(PlayerGesture.Run);
        }
        private void Broadcast(PlayerGesture gesture)
        {
            Debug.Log("[KeyboardInput] Gesture broadcast: " + gesture);
            GestureEventBus.Broadcast(gesture);
        }
    }
}
