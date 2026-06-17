// ThiefIndicator.cs
// Sims-style diamond (plumbob) floating above the thief's head.
//   GO / Moving   -> Green
//   STOP / Hidden -> Yellow
//   Being attacked-> RED, flashing
// [ExecuteAlways] so it is visible in editor AND play. Mesh built in OnEnable
// so it is never null.

using UnityEngine;

namespace GestureThiefSystem
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ThiefIndicator : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private ThiefController thief;
        [SerializeField] private ThiefHitReaction thiefHit;
        [SerializeField] private Transform thiefTransform;

        [Header("Position")]
        [SerializeField] private float heightAbove  = 2.6f;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed     = 1.5f;

        [Header("Size")]
        [SerializeField] private float diamondSize  = 0.6f;

        [Header("Colors")]
        [SerializeField] private Color colorGo   = new Color(0.15f, 0.9f, 0.15f, 1f);
        [SerializeField] private Color colorStop = new Color(1.0f, 0.85f, 0.0f, 1f);
        [SerializeField] private Color colorHit  = new Color(0.95f, 0.1f, 0.1f, 1f);

        [Header("Danger Flash")]
        [SerializeField] private float hitFlashDuration = 0.9f;
        [SerializeField] private float flashRate = 6f;

        private MeshRenderer _mr;
        private Material     _mat;
        private float        _hitTimer;
        private Color        _baseColor;
        private float        _bobOffset;

        private void OnEnable()
        {
            _mr = GetComponent<MeshRenderer>();
            BuildDiamondMesh();
            CreateMaterial();
            _baseColor = colorStop;
            if (!thiefTransform) ResolveThief();
            _bobOffset = Random.value * Mathf.PI * 2f;
        }

        private void ResolveThief()
        {
            if (thief == null) thief = FindAnyObjectByType<ThiefController>();
            if (thiefHit == null && thief != null) thiefHit = thief.GetComponent<ThiefHitReaction>();
            if (thief != null) thiefTransform = thief.transform;
            if (thiefTransform == null)
            {
                var go = GameObject.Find("thief");
                if (go != null) thiefTransform = go.transform;
            }
        }

        private void Update()
        {
            if (thiefTransform == null) ResolveThief();
            if (thiefTransform == null) return;

            float bob = Mathf.Sin(Time.time * bobSpeed + _bobOffset) * bobAmplitude;
            Vector3 p = thiefTransform.position;
            transform.position = new Vector3(p.x, p.y + heightAbove + bob, p.z);
            transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);

            if (_mat == null) return;

            if (thief != null)
                _baseColor = (thief.CurrentState == ThiefState.Moving ||
                              thief.CurrentState == ThiefState.Running) ? colorGo : colorStop;

            Color shown;
            if (_hitTimer > 0f && Application.isPlaying)
            {
                _hitTimer -= Time.deltaTime;
                float t = (Mathf.Sin(Time.time * flashRate * Mathf.PI * 2f) + 1f) * 0.5f;
                shown = Color.Lerp(_baseColor, colorHit, t);
            }
            else shown = _baseColor;

            _mat.color = shown;
            if (_mat.HasProperty("_EmissionColor"))
                _mat.SetColor("_EmissionColor", shown * 0.8f);
        }

        public void FlashHit() => _hitTimer = hitFlashDuration;

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

        private void CreateMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard")
                      ?? Shader.Find("Unlit/Color");
            _mat = new Material(shader);
            _mat.color = colorStop;
            if (_mat.HasProperty("_EmissionColor"))
            {
                _mat.EnableKeyword("_EMISSION");
                _mat.SetColor("_EmissionColor", colorStop * 0.8f);
            }
            _mr.sharedMaterial = _mat;
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
        }
    }
}