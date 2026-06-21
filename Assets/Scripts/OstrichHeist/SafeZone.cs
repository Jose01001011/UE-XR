// SafeZone.cs
// Marks the win location. When the thief (carrying the egg) enters, raises the
// win event. Works either by trigger collider OR by the ThiefAI distance check;
// this provides the collider path and a visible gizmo.

using UnityEngine;

namespace OstrichHeist
{
    [RequireComponent(typeof(Collider))]
    public class SafeZone : MonoBehaviour
    {
        [SerializeField] private float radiusGizmo = 2f;
        [SerializeField] private Color gizmoColor = new Color(0.15f, 0.45f, 1f, 0.4f);
        private bool _won;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_won) return;
            var thief = other.GetComponentInParent<ThiefAI>();
            if (thief != null && thief.HasEgg)
            {
                _won = true;
                thief.WinGame(); // ThiefAI raises OnSafeZoneReached
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.1f, radiusGizmo);
        }
    }
}
