// PlayerGesture.cs
// Enum representing every gesture the player can perform.
// Maps 1:1 to keyboard test keys and future VR poses.

namespace GestureThiefSystem
{
    public enum PlayerGesture
    {
        None,
        Stop,       // H key / open palm facing outward
        GoForward,  // G key / pointing / forward directional
        Crouch,     // C key / downward hand motion
        Hide,       // H key / hand lowered / stay low
        Run         // R key / fast repeated forward wave
    }
}
