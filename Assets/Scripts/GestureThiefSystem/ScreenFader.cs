// ScreenFader.cs
// VR-friendly fade-to-black. Creates a small black quad parented to the main
// camera that covers the view, and fades its alpha in/out. Works in VR because
// it moves with the head. Call FadeOut()/FadeIn() or FadeSequence().

using System.Collections;
using UnityEngine;

namespace GestureThiefSystem
{
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 1.0f;

        private Renderer _quad;
        private Material _mat;
        private Camera _cam;

        private void Awake()
        {
            BuildQuad();
        }

        private void BuildQuad()
        {
            _cam = Camera.main;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "FadeQuad";
            Destroy(go.GetComponent<Collider>());

            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            _mat = new Material(shader);
            SetAlpha(0f);
            _quad = go.GetComponent<Renderer>();
            _quad.sharedMaterial = _mat;
            _quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Parent to camera, place just in front, large enough to cover view.
            if (_cam != null) go.transform.SetParent(_cam.transform, false);
            go.transform.localPosition = new Vector3(0, 0, 0.3f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(2f, 2f, 1f);
            go.SetActive(false);
        }

        private void SetAlpha(float a)
        {
            if (_mat == null) return;
            var c = Color.black; c.a = a;
            if (_mat.HasProperty("_Color")) _mat.color = c;
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
            // Make the unlit material transparent-capable
            _mat.SetFloat("_Mode", 2);
            _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetInt("_ZWrite", 0);
            _mat.EnableKeyword("_ALPHABLEND_ON");
            _mat.renderQueue = 4000;
        }

        public Coroutine FadeOut() => StartCoroutine(Fade(0f, 1f)); // to black
        public Coroutine FadeIn()  => StartCoroutine(Fade(1f, 0f)); // from black

        private IEnumerator Fade(float from, float to)
        {
            if (_quad == null) yield break;
            _quad.gameObject.SetActive(true);
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                SetAlpha(Mathf.Lerp(from, to, t / fadeDuration));
                yield return null;
            }
            SetAlpha(to);
            if (to <= 0f) _quad.gameObject.SetActive(false);
        }
    }
}
