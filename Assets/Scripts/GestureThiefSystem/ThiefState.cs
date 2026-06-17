// ThiefState.cs
// Shared enum for all thief NPC states.
// Used by ThiefController, GestureInputHandler, and any UI/debug systems.

namespace GestureThiefSystem
{
    public enum ThiefState
    {
        Idle,       // Default standing state
        Moving,     // Walking toward egg objective
        Crouching,  // Reduced visibility, slow movement
        Hidden,     // Fully hidden, no movement
        Running,    // Sprint / emergency escape
        Alert       // Frozen alert idle (after STOP command)
    }
}
