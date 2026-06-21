// ThiefPlumbob.cs
// Sims-style floating diamond above the thief that shows his STATE via color.
// This is the ONLY thing that changes color — the thief body keeps its texture.
//
// Colors (per spec):
//   Idle    -> Yellow
//   Moving  -> Green   (crawl / run)
//   Hidden  -> Purple
//   Attacked-> Red (brief flash, then back to underlying state)
//   Victory -> Blue
//
// Follows the thief every frame, bobs, and spins. Listens to
// GameEvents.OnThiefStateChanged so it never touches the body material.

using UnityEngine;

namespace OstrichHeist
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ThiefPlumbob : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform thief;            // who to float above
        [SerializeField] private float heightAbove = 2.6f;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 1.5f;
        [SerializeField] private float spinSpeed = 60f;

        [Header("Size")]
        [SerializeField] private float diamondSize = 0.45f;

        [Header("State Colors")]
        [SerializeField] private Color colorIdle     = new Color(1f, 0.85f, 0f);      // yellow
        [SerializeField] private Color colorMoving   = new Color(0.15f, 0.9f, 0.15f); // green
        [SerializeField] private Color colorHidden   = new Color(0.6f, 0.1f, 0.85f);  // purple
        [SerializeField] private Color colorAttacked = new Color(0.95f, 0.1f, 0.1f);  // red
        [SerializeField] private Color colorVictory  = new Color(0.15f, 0.45f, 1f);   // blue

        [Header("Attack Flash")]
        [SerializeField] private float flashDuration = 0.5f;
        [SerializeField] private float flashRate = 6f;

        private MeshRenderer _mr;
        private Material _mat;
        private Color _baseColor;     // color of the current underlying state
        private float _flashUntil;
        private float _bobOffset;

        private void Awake()
        {
            _mr = GetComponent<MeshRenderer>();
            BuildDiamondMesh();
            CreateMaterial();
            _baseColor = colorIdle;
            ApplyColor(colorIdle);
            _bobOffset = Random.value * Mathf.PI * 2f;
            if (thief == null)
            {
                var ai = FindObjectOfType<ThiefAI>();
                if (ai != null) thief = ai.transform;
            }
        }

        private void OnEnable()  { GameEvents.OnThiefStateChanged += OnStateChanged; }
        private void OnDisable() { GameEvents.OnThiefStateChanged -= OnStateChanged; }

        private void OnStateChanged(ThiefState s)
        {
            switch (s)
            {
                case ThiefState.Idle:         _baseColor = colorIdle;    break;
                case ThiefState.Moving:       _baseColor = colorMoving;  break;
                case ThiefState.Running:      _baseColor = colorMoving;  break;
                case ThiefState.PickingUpEgg: _baseColor = colorMoving;  break;
                case ThiefState.Hidden:       _baseColor = colorHidden;  break;
                case ThiefState.Victory:      _baseColor = colorVictory; break;
                case ThiefState.Attacked:
                    // Attacked is a transient flash; don't overwrite the base state color.
                    _flashUntil = Time.time + flashDuration;
                    return;
            }
            if (Time.time >= _flashUntil) ApplyColor(_baseColor);
        }

        private void Update()
        {
            if (thief != null)
            {
                float bob = Mathf.Sin(Time.time * bobSpeed + _bobOffset) * bobAmplitude;
                Vector3 p = thief.position;
                transform.position = new Vector3(p.x, p.y + heightAbove + bob, p.z);
                transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            }

            // Handle the red attack flash overlay
            if (_flashUntil > Time.time)
            {
                float t = (Mathf.Sin(Time.time * flashRate * Mathf.PI * 2f) + 1f) * 0.5f;
                ApplyColor(Color.Lerp(_baseColor, colorAttacked, t));
            }
            else
            {
                ApplyColor(_baseColor);
            }
        }

        private void ApplyColor(Color c)
        {
            if (_mat == null) return;
            _mat.color = c;
            if (_mat.HasProperty("_EmissionColor"))
                _mat.SetColor("_EmissionColor", c * 0.8f);
        }

        private void CreateMaterial()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var m = new Material(shader);
            m.color = colorIdle;
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", colorIdle * 0.8f);
            }
            _mr.material = m;                 // assign
            _mat = _mr.material;              // read back the LIVE instance (critical)
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
        }

        private void BuildDiamondMesh()
        {
            var mesh = new Mesh { name = "Plumbob" };
            float h = diamondSize, r = diamondSize * 0.55f;
            int seg = 6;
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris  = new System.Collections.Generic.List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                verts.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
            verts.Add(new Vector3(0, h, 0));         int topIdx = verts.Count - 1;
            verts.Add(new Vector3(0, -h * 1.4f, 0)); int botIdx = verts.Count - 1;
            for (int i = 0; i < seg; i++)
            {
                int a = i, b = (i + 1) % seg;
                tris.Add(topIdx); tris.Add(a); tris.Add(b);
                tris.Add(botIdx); tris.Add(b); tris.Add(a);
            }
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        public void SetThief(Transform t) { thief = t; }
    }
}
