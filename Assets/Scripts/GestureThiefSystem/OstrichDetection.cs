// OstrichDetection.cs
// Handles detection logic between the Ostrich NPC and the Thief NPC.
//
// The ostrich has two sphere trigger zones:
//   WarningZone  -> thief enters warning range (scout should warn player)
//   DangerZone   -> ostrich is close; checks DetectionChance each tick
//
// HOW TO SET UP IN UNITY:
//   1. Add this script to the Ostrich GameObject.
//   2. Add two child GameObjects: "WarningZone" and "DangerZone".
//   3. Add a SphereCollider (IsTrigger = true) to each child.
//   4. Assign them in the Inspector.
//   5. The Thief must be on a Layer named "Thief".
//   6. Ensure Physics settings have Ostrich <-> Thief layer collision enabled.

using UnityEngine;

namespace GestureThiefSystem
{
    public class OstrichDetection : MonoBehaviour
    {
        [Header("Zone Colliders")]
        [Tooltip("Outer warning zone -- scout triggers here.")]
        [SerializeField] private SphereCollider warningZone;

        [Tooltip("Inner danger zone -- detection chance checked here.")]
        [SerializeField] private SphereCollider dangerZone;

        [Header("Detection")]
        [Tooltip("How often (seconds) to check for detection inside danger zone.")]
        [SerializeField] private float detectionTickRate = 0.5f;

        [Header("References")]
        [SerializeField] private ThiefController thief;
        [SerializeField] private ScoutNPC        scout;

        // -- Events --
        public UnityEngine.Events.UnityEvent OnThiefEntersWarningZone;
        public UnityEngine.Events.UnityEvent OnThiefExitsWarningZone;

        // -- Internal --
        private bool  _thiefInDanger  = false;
        private float _tickTimer      = 0f;

        private void Update()
        {
            if (!_thiefInDanger) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= detectionTickRate)
            {
                _tickTimer = 0f;
                CheckDetection();
            }
        }

        // -- Zone trigger callbacks --
        // Because zones are child GameObjects, forward their trigger callbacks
        // from TriggerForwarder components (see TriggerForwarder.cs).

        public void OnThiefEnteredWarning()
        {
            Debug.Log("[Ostrich] Thief entered WARNING zone.");
            OnThiefEntersWarningZone?.Invoke();
            if (scout != null) scout.TriggerWarning();
        }

        public void OnThiefExitedWarning()
        {
            Debug.Log("[Ostrich] Thief left warning zone.");
            OnThiefExitsWarningZone?.Invoke();
            if (scout != null) scout.ClearWarning();
        }

        public void OnThiefEnteredDanger()
        {
            Debug.Log("[Ostrich] Thief entered DANGER zone!");
            _thiefInDanger = true;
            _tickTimer     = 0f;
        }

        public void OnThiefExitedDanger()
        {
            Debug.Log("[Ostrich] Thief escaped danger zone.");
            _thiefInDanger = false;
        }

        // -- Detection logic --
        private void CheckDetection()
        {
            if (thief == null) return;

            float roll = Random.value; // 0.0 - 1.0
            float chance = thief.DetectionChance;

            Debug.Log($"[Ostrich] Detection check: roll={roll:F2} vs chance={chance:F2}");

            if (roll <= chance)
            {
                Debug.Log("[Ostrich] DETECTED the thief!");
                thief.TriggerDetected();
                _thiefInDanger = false;
            }
        }
    }
}
