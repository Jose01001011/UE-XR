// OstrichAttack.cs
// When the (non-hidden) thief is within attack range, the ostrich strikes on a
// cadence. Each strike plays an optional attack animation and lands a hit on the
// thief's ThiefHitReaction. Stops attacking once the thief is down or hiding.

using UnityEngine;

namespace GestureThiefSystem
{
    public class OstrichAttack : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private ThiefController thief;
        [SerializeField] private ThiefHitReaction thiefHit;

        [Header("Attack")]
        [SerializeField] private float attackRange = 2.0f;
        [SerializeField] private float attackInterval = 1.0f;

        [Header("Animation (optional)")]
        [SerializeField] private Animator ostrichAnimator;
        [SerializeField] private string attackTrigger = "Attack";

        private float _timer;

        private void Update()
        {
            if (thief == null || thiefHit == null) return;
            if (thiefHit.IsDown) return;

            bool hidden = thief.CurrentState == ThiefState.Hidden;
            float dist = Vector3.Distance(transform.position, thief.transform.position);

            if (!hidden && dist <= attackRange)
            {
                _timer += Time.deltaTime;
                if (_timer >= attackInterval)
                {
                    _timer = 0f;
                    if (ostrichAnimator != null && !string.IsNullOrEmpty(attackTrigger))
                        ostrichAnimator.SetTrigger(attackTrigger);
                    thiefHit.TakeHit();
                }
            }
            else
            {
                _timer = 0f;
            }
        }
    }
}
