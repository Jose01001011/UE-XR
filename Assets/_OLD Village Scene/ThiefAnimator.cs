using System.Collections;
using UnityEngine;

// NEUTERED: This old script previously ran its own thief AI (movement + caught
// detection + game over), which fought the new ThiefController/NavMeshAgent and
// caused instant GAME OVER. All behaviour is disabled. Kept only so existing
// serialized references and the Animator component remain intact.
public class ThiefAnimator : MonoBehaviour
{
    [SerializeField] private SignallerAnimator signallerAI;
    [SerializeField] private Transform ostrichTransform;
    [SerializeField] private Transform eggTarget;
    [SerializeField] private Transform escapeTarget;
    [SerializeField] private StealthLevelManager levelManager;

    // No Start/Update/coroutines — this script intentionally does nothing now.
    // The new GestureThiefSystem.ThiefController drives the thief.
}
