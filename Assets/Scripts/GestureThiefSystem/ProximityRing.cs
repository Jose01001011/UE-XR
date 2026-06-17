// ProximityRing.cs
// Draws a circular range indicator around the Ostrich (and optionally the Thief).
// Visible in the EDITOR so designers can tune radii; hidden at runtime so players
// never see it. The trigger zones (WarningZone / DangerZone) are separate child
// colliders and are always active regardless of this renderer.

using UnityEngine;

namespace GestureThiefSystem
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public class ProximityRing : MonoBehaviour
    {
        [SerializeField] private float radius    = 3f;
        [SerializeField] private Color color     = Color.yellow;
        [SerializeField] private int   segments  = 64;
        [SerializeField] private float yOffset   = 0.05f;
        [SerializeField] private float lineWidth = 0.05f;

        private LineRenderer _lr;

        private void OnEnable()
        {
            Build();
            // Hide the visual line when the game is actually running
            HideIfPlaying();
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled) Build();
        }

        // Called automatically when Play mode starts/stops in the Editor
        private void Start()
        {
            HideIfPlaying();
        }

        public void SetRadius(float r) { radius = r; Build(); }
        public void SetColor(Color c)  { color  = c; Build(); }

        private void HideIfPlaying()
        {
            if (_lr == null) _lr = GetComponent<LineRenderer>();
            if (_lr == null) return;

            // Application.isPlaying is true in Play mode and in a built APK
            _lr.enabled = !Application.isPlaying;
        }

        private void Build()
        {
            if (_lr == null) _lr = GetComponent<LineRenderer>();
            if (_lr == null) return;

            _lr.useWorldSpace  = false;
            _lr.loop           = true;
            _lr.positionCount  = Mathf.Max(8, segments);
            _lr.startWidth     = lineWidth;
            _lr.endWidth       = lineWidth;
            _lr.startColor     = color;
            _lr.endColor       = color;

            if (_lr.sharedMaterial == null)
            {
                Shader sh = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");
                if (sh != null) _lr.sharedMaterial = new Material(sh);
            }

            int count = _lr.positionCount;
            for (int i = 0; i < count; i++)
            {
                float a = (float)i / count * Mathf.PI * 2f;
                _lr.SetPosition(i, new Vector3(
                    Mathf.Cos(a) * radius, yOffset, Mathf.Sin(a) * radius));
            }
        }
    }
}
