// ThiefIndicator.cs
// Sims-style diamond (plumbob) above the thief's head.
// Color reflects current thief state:
//   GO / Moving   -> Green  (thief is active, heading to egg)
//   STOP / Hidden -> Yellow (thief is hiding)
//   Being hit     -> Red    (flash red on each hit, then back to previous color)
//
// The diamond mesh is built procedurally from 8 triangles so no external asset
// is needed. It always faces the camera (billboard) and bobs gently up/down.

using UnityEngine;

namespace GestureThiefSystem
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ThiefIndicator : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Inspector
        // ----------------------------------------------------------------
        [Header("Target")]
        [Tooltip("The thief GameObject. Auto-found if left empty.")]
        [SerializeField] private ThiefController thief;
        [SerializeField] private ThiefHitReaction thiefHit;

        [Header("Position")]
        [Tooltip("Height above the thief's world origin.")]
        [SerializeField] private float heightAbove  = 2.3f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed     = 1.5f;

        [Header("Size")]
        [SerializeField] private float diamondSize  = 0.22f;

        [Header("Colors")]
        [SerializeField] private Color colorGo      = new Color(0.18f, 0.85f, 0.18f, 1f); // bright green
        [SerializeField] private Color colorStop    = new Color(1.00f, 0.85f, 0.00f, 1f); // yellow
        [SerializeField] private Color colorHit     = new Color(0.95f, 0.10f, 0.10f, 1f); // red
        [SerializeField] private float hitFlashDuration = 0.5f;

        // ----------------------------------------------------------------
        private MeshRenderer _mr;
        private Material     _mat;
        private Camera       _cam;
        private float        _hitTimer;
        private Color        _baseColor;
        private float        _bobOffset;

        // ----------------------------------------------------------------
        private void Awake()
        {
            _mr = GetComponent<MeshRenderer>();
            BuildDiamondMesh();
            CreateMaterial();
            _baseColor = colorStop;
        }

        private void Start()
        {
            if (!thief)    thief    = FindAnyObjectByType<ThiefController>();
            if (!thiefHit) thiefHit = FindAnyObjectByType<ThiefHitReaction>();
            if (!_cam)     _cam     = Camera.main;
            _bobOffset = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (!thief) return;

            // --- Follow thief ---
            Vector3 pos = thief.transform.position;
            float   bob = Mathf.Sin(Time.time * bobSpeed + _bobOffset) * bobAmplitude;
            transform.position = new Vector3(pos.x, pos.y + heightAbove + bob, pos.z);

            // --- Billboard: always face camera ---
            if (_cam)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);

            // --- Color based on state ---
            Color target = _baseColor;

            if (thief.CurrentState == ThiefState.Moving ||
                thief.CurrentState == ThiefState.Running)
                _baseColor = colorGo;
            else
                _baseColor = colorStop;

            // Hit flash overrides
            if (_hitTimer > 0f)
            {
                _hitTimer -= Time.deltaTime;
                target = colorHit;
            }
            else
            {
                target = _baseColor;
            }

            _mat.color = UnityEngine.Color.Lerp(_mat.color, target, Time.deltaTime * 12f);
        }

        // Called by ThiefHitReaction when a hit lands
        public void FlashHit() => _hitTimer = hitFlashDuration;

        // ----------------------------------------------------------------
        // Procedural diamond mesh (8-sided bicone)
        // ----------------------------------------------------------------
        private void BuildDiamondMesh()
        {
            var mesh = new Mesh();
            mesh.name = "Plumbob";

            float h  = diamondSize;       // half-height
            float r  = diamondSize * 0.6f; // equator radius
            int   seg = 6;                // sides

            var verts = new System.Collections.Generic.List<Vector3>();
            var tris  = new System.Collections.Generic.List<int>();

            Vector3 top = new Vector3(0,  h, 0);
            Vector3 bot = new Vector3(0, -h * 1.4f, 0); // slightly longer bottom

            // Equator ring
            int eqStart = 0;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                verts.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
            verts.Add(top);   int topIdx = verts.Count - 1;
            verts.Add(bot);   int botIdx = verts.Count - 1;

            for (int i = 0; i < seg; i++)
            {
                int a = eqStart + i;
                int b = eqStart + (i + 1) % seg;
                // Upper face
                tris.Add(topIdx); tris.Add(a); tris.Add(b);
                // Lower face
                tris.Add(botIdx); tris.Add(b); tris.Add(a);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private void CreateMaterial()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            _mat = new Material(shader);
            _mat.color = colorStop;
            // Emissive so it glows
            _mat.EnableKeyword("_EMISSION");
            _mat.SetColor("_EmissionColor", colorStop * 0.6f);
            _mr.sharedMaterial = _mat;
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
        }
    }
}