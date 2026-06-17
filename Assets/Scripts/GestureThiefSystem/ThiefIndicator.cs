// ThiefIndicator.cs
// Sims-style diamond (plumbob) above the thief's head.
// Color reflects state:
//   GO / Moving   -> Green  (heading to egg)
//   STOP / Hidden -> Yellow (hiding)
//   Being attacked-> RED, actively FLASHING (danger)
// Procedural mesh, billboards to camera, bobs gently.

using UnityEngine;

namespace GestureThiefSystem
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ThiefIndicator : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private ThiefController thief;
        [SerializeField] private ThiefHitReaction thiefHit;

        [Header("Position")]
        [SerializeField] private float heightAbove  = 2.6f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed     = 1.5f;

        [Header("Size")]
        [SerializeField] private float diamondSize  = 0.45f;

        [Header("Colors")]
        [SerializeField] private Color colorGo   = new Color(0.18f, 0.85f, 0.18f, 1f); // green
        [SerializeField] private Color colorStop = new Color(1.00f, 0.85f, 0.00f, 1f); // yellow
        [SerializeField] private Color colorHit  = new Color(0.95f, 0.10f, 0.10f, 1f); // red

        [Header("Danger Flash")]
        [Tooltip("How long the red danger flash lasts after each peck.")]
        [SerializeField] private float hitFlashDuration = 0.9f;
        [Tooltip("Flashes per second while in danger.")]
        [SerializeField] private float flashRate = 6f;

        private MeshRenderer _mr;
        private Material     _mat;
        private Camera       _cam;
        private float        _hitTimer;
        private Color        _baseColor;
        private float        _bobOffset;

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

            // Follow thief
            Vector3 pos = thief.transform.position;
            float   bob = Mathf.Sin(Time.time * bobSpeed + _bobOffset) * bobAmplitude;
            transform.position = new Vector3(pos.x, pos.y + heightAbove + bob, pos.z);

            // Billboard
            if (!_cam) _cam = Camera.main;
            if (_cam)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);

            // Base color from state
            _baseColor = (thief.CurrentState == ThiefState.Moving ||
                          thief.CurrentState == ThiefState.Running)
                         ? colorGo : colorStop;

            Color shown;
            if (_hitTimer > 0f)
            {
                _hitTimer -= Time.deltaTime;
                // Pulse between red and the base color so it visibly FLASHES.
                float t = (Mathf.Sin(Time.time * flashRate * Mathf.PI * 2f) + 1f) * 0.5f;
                shown = Color.Lerp(_baseColor, colorHit, t);
            }
            else
            {
                shown = Color.Lerp(_mat.color, _baseColor, Time.deltaTime * 12f);
            }

            _mat.color = shown;
            _mat.SetColor("_EmissionColor", shown * 0.7f);
        }

        // Called by ThiefHitReaction on each peck.
        public void FlashHit() => _hitTimer = hitFlashDuration;

        private void BuildDiamondMesh()
        {
            var mesh = new Mesh { name = "Plumbob" };
            float h = diamondSize, r = diamondSize * 0.6f;
            int seg = 6;
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris  = new System.Collections.Generic.List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                verts.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
            verts.Add(new Vector3(0, h, 0));        int topIdx = verts.Count - 1;
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

        private void CreateMaterial()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            _mat = new Material(shader);
            _mat.color = colorStop;
            _mat.EnableKeyword("_EMISSION");
            _mat.SetColor("_EmissionColor", colorStop * 0.7f);
            _mr.sharedMaterial = _mat;
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
        }
    }
}