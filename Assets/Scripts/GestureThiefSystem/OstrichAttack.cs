// OstrichAttack.cs
// When the EXPOSED thief is within attack range, the ostrich strikes on a
// cadence. Each strike plays an attack animation (if present) and lands a hit
// via ThiefHitReaction. Stops if the thief hides or is already down.
//
// BALANCE: attackInterval is spaced so the player has a fair chance to issue a
// STOP before the next hit lands. Hidden thief = no attacks (ostrich gives up).

using UnityEngine;

namespace GestureThiefSystem
{
    public class OstrichAttack : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private ThiefController thief;
        [SerializeField] private ThiefHitReaction thiefHit;

        [Header("Attack")]
        [Tooltip("Range at which the ostrich can land a peck.")]
        [SerializeField] private float attackRange = 2.5f;
        [Tooltip("Seconds between pecks — spaced so the player can react with STOP.")]
        [SerializeField] private float attackInterval = 1.4f;
        [Tooltip("Delay before the FIRST peck once in range (telegraph window).")]
        [SerializeField] private float windUpTime = 0.7f;

        [Header("Animation (optional)")]
        [SerializeField] private Animator ostrichAnimator;
        [SerializeField] private string attackTrigger = "Attack";

        private float _timer;
        private bool  _engaged;

        private void Update()
        {
            if (thief == null || thiefHit == null) return;
            if (thiefHit.IsDown) return;

            bool hidden = thief.CurrentState == ThiefState.Hidden;
            float dist = Vector3.Distance(transform.position, thief.transform.position);

            if (!hidden && dist <= attackRange)
            {
                if (!_engaged)
                {
                    // Just got in range — wind up before the first hit.
                    _engaged = true;
                    _timer   = attackInterval - windUpTime;
                }

                _timer += Time.deltaTime;
                if (_timer >= attackInterval)
                {
                    _timer = 0f;
                    if (ostrichAnimator != null && !string.IsNullOrEmpty(attackTrigger))
                        ostrichAnimator.SetTrigger(attackTrigger);
                    thiefHit.TakeHit();
                    Debug.Log("[Ostrich] PECK! hit landed.");
                }
            }
            else
            {
                _engaged = false;
                _timer   = 0f;
            }
        }
    }
}