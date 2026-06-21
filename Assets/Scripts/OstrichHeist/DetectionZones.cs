// DetectionZones.cs
// The three proximity circles (spec section 7): Thief, Nest, Ostrich.
// Draws gizmos for debugging and evaluates intersections each frame:
//   - Thief circle ∩ Ostrich circle  -> if thief NOT hiding: ostrich attacks (handled
//     by OstrichAI's own range check); always raises OnThiefDetected for the signaler.
//   - Ostrich near Nest vicinity      -> raises OnDangerNearNest (signaler waves).
// Uses cooldown so it doesn't spam events every frame.

using UnityEngine;

namespace OstrichHeist
{
    public class DetectionZones : MonoBehaviour
    {
        [Header("Actors")]
        [SerializeField] private Transform thief;
        [SerializeField] private Transform ostrich;
        [SerializeField] private Transform nest;
        [SerializeField] private ThiefAI thiefAI;

        [Header("Circle Radii (metres)")]
        [SerializeField] private float thiefRadius   = 2.5f;
        [SerializeField] private float ostrichRadius = 3.5f;
        [SerializeField] private float nestRadius    = 4.0f;

        [Header("Event Cooldown")]
        [SerializeField] private float eventCooldown = 1.0f;

        [Header("Gizmo Colors")]
        [SerializeField] private Color thiefColor   = new Color(0f,1f,0f,0.5f);
        [SerializeField] private Color ostrichColor = new Color(1f,0f,0f,0.5f);
        [SerializeField] private Color nestColor    = new Color(0f,0.6f,1f,0.5f);

        private float _lastDetect;
        private float _lastNest;

        private void Start()
        {
            if (thiefAI == null) thiefAI = FindAnyObjectByType<ThiefAI>();
            if (thief == null && thiefAI != null) thief = thiefAI.transform;
        }

        private void Update()
        {
            if (thief == null || ostrich == null) return;

            // Circle ∩ circle = distance <= sum of radii
            float dThiefOstrich = Vector3.Distance(thief.position, ostrich.position);
            bool circlesMeet = dThiefOstrich <= (thiefRadius + ostrichRadius);

            if (circlesMeet && Time.time - _lastDetect >= eventCooldown)
            {
                _lastDetect = Time.time;
                bool hiding = thiefAI != null && thiefAI.IsHidden;
                if (!hiding)
                {
                    // Signaler should warn; ostrich's own logic decides the attack.
                    GameEvents.RaiseThiefDetected();
                }
            }

            // Ostrich in nest vicinity -> danger signal
            if (nest != null)
            {
                float dOstrichNest = Vector3.Distance(ostrich.position, nest.position);
                if (dOstrichNest <= nestRadius && Time.time - _lastNest >= eventCooldown)
                {
                    _lastNest = Time.time;
                    GameEvents.RaiseDangerNearNest();
                }
            }
        }

        private void OnDrawGizmos()
        {
            DrawCircle(thief,   thiefRadius,   thiefColor);
            DrawCircle(ostrich, ostrichRadius, ostrichColor);
            DrawCircle(nest,    nestRadius,    nestColor);
        }

        private void DrawCircle(Transform t, float radius, Color c)
        {
            if (t == null) return;
            Gizmos.color = c;
            const int seg = 48;
            Vector3 prev = t.position + new Vector3(radius, 0.05f, 0);
            for (int i = 1; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                Vector3 next = t.position + new Vector3(Mathf.Cos(a)*radius, 0.05f, Mathf.Sin(a)*radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        public void SetActors(Transform t, Transform o, Transform n, ThiefAI ai)
        { thief = t; ostrich = o; nest = n; thiefAI = ai; }
    }
}
