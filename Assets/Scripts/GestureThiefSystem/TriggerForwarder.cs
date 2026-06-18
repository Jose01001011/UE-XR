// TriggerForwarder.cs
// Attach to each zone child GameObject (WarningZone, DangerZone) on the Ostrich.
// Forwards trigger events to OstrichDetection on the parent.
//
// SETUP:
//   1. Add to the WarningZone child -- set zoneType = Warning.
//   2. Add to the DangerZone child  -- set zoneType = Danger.
//   3. Both children need a SphereCollider with Is Trigger = true.
//   4. The Thief GameObject must be tagged "Thief" OR on layer "Thief".

using UnityEngine;

namespace GestureThiefSystem
{
    public class TriggerForwarder : MonoBehaviour
    {
        public enum ZoneType { Warning, Danger }

        [SerializeField] private ZoneType zoneType;
        [SerializeField] private string   thiefTag = "Thief";

        private OstrichDetection _ostrich;

        private void Awake()
        {
            // Walk up to parent to find OstrichDetection
            _ostrich = GetComponentInParent<OstrichDetection>();
            if (_ostrich == null)
                Debug.LogWarning("[TriggerForwarder] No OstrichDetection found in parent.", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(thiefTag)) return;
            if (_ostrich == null) return;

            if (zoneType == ZoneType.Warning) _ostrich.OnThiefEnteredWarning();
            else                              _ostrich.OnThiefEnteredDanger();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(thiefTag)) return;
            if (_ostrich == null) return;

            if (zoneType == ZoneType.Warning) _ostrich.OnThiefExitedWarning();
            else                              _ostrich.OnThiefExitedDanger();
        }
    }
}
