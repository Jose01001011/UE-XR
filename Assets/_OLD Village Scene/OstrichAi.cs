using UnityEngine;
using System.Collections;

// NEUTERED: This old script ran its own ostrich wander/chase via transform moves,
// fighting the new OstrichPatrol/NavMeshAgent. All behaviour disabled. Kept only
// so existing serialized references remain intact.
public class OstrichAI : MonoBehaviour
{
    [SerializeField] private Transform thiefTransform;
    [SerializeField] private SignallerAnimator signallerAI;
    [SerializeField] private Transform eggTransform;

    // No Start/Update/coroutines — OstrichPatrol now drives the ostrich.
}
