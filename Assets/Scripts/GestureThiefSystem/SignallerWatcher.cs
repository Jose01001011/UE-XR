// SignallerWatcher.cs
// Drives the Signaller (Scout). The signaller warns the player when the ostrich
// gets close to the nest (egg) OR close to the thief. When the danger clears it
// signals all-clear. Works through ScoutNPC so animation/hint logic stays in one
// place.

using UnityEngine;

namespace GestureThiefSystem
{
    public class SignallerWatcher : MonoBehaviour
    {
        [Header("Watched Transforms")]
        [SerializeField] private Transform ostrich;
        [SerializeField] private Transform nest;   // the egg / nest
        [SerializeField] private Transform thief;

        [Header("Signaller")]
        [SerializeField] private ScoutNPC scout;

        [Header("Alert Radii")]
        [SerializeField] private float nestAlertRadius = 4f;
        [SerializeField] private float thiefAlertRadius = 4f;
        [SerializeField] private float reSignalCooldown = 2f;

        private bool _signalling = false;
        private float _cooldown = 0f;

        private void Update()
        {
            if (ostrich == null || scout == null) return;
            _cooldown -= Time.deltaTime;

            bool nearNest  = nest  != null && Vector3.Distance(ostrich.position, nest.position)  <= nestAlertRadius;
            bool nearThief = thief != null && Vector3.Distance(ostrich.position, thief.position) <= thiefAlertRadius;
            bool danger = nearNest || nearThief;

            if (danger && !_signalling && _cooldown <= 0f)
            {
                _signalling = true;
                _cooldown = reSignalCooldown;
                scout.TriggerWarning();
                Debug.Log("[Signaller] Ostrich near " + (nearNest ? "NEST" : "THIEF") + " -> signalling!");
            }
            else if (!danger && _signalling)
            {
                _signalling = false;
                scout.ClearWarning();
                Debug.Log("[Signaller] All clear.");
            }
        }
    }
}
