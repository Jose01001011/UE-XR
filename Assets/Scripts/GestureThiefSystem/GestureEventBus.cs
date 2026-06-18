// GestureEventBus.cs
// Lightweight static event bus.
// Any system can broadcast a gesture; any system can listen.
// Keeps GestureInputHandler, ThiefController, and UI fully decoupled.
//
// Usage:
//   GestureEventBus.OnGesturePerformed += HandleGesture;   // subscribe
//   GestureEventBus.Broadcast(PlayerGesture.Hide);          // fire

using System;

namespace GestureThiefSystem
{
    public static class GestureEventBus
    {
        /// <summary>Fired whenever the player performs a recognised gesture.</summary>
        public static event Action<PlayerGesture> OnGesturePerformed;

        /// <summary>Broadcast a gesture to all listeners.</summary>
        public static void Broadcast(PlayerGesture gesture)
        {
            OnGesturePerformed?.Invoke(gesture);
        }
    }
}
