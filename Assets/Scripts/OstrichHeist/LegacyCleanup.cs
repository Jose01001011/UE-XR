// LegacyCleanup.cs
// Defensive startup guard for leftover prototype objects.
// Runs every frame for the first moment AND on a repeating check, because the old
// intro managers keep re-activating duplicate 'Thief' objects (each with its own
// Camera that renders a black-background / white view into the preview).
//
// Disables: old 'Thief' duplicates, any non-XR camera, and old GestureThiefSystem scripts.

using UnityEngine;

namespace OstrichHeist
{
    [DefaultExecutionOrder(-10000)]
    public class LegacyCleanup : MonoBehaviour
    {
        private float _recheckUntil;

        private void Awake()  { Sweep(); }
        private void Start()  { Sweep(); _recheckUntil = Time.time + 3f; }

        // Keep sweeping for the first few seconds to catch late re-activations by intro scripts.
        private void Update()
        {
            if (Time.time <= _recheckUntil) Sweep();
        }

        private void Sweep()
        {
            int thiefs = 0, cams = 0, scripts = 0;

            // 1. Disable old 'Thief' roots that lack our ThiefAI.
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root.name == "Thief" && root.GetComponent<ThiefAI>() == null && root.activeSelf)
                {
                    root.SetActive(false);
                    thiefs++;
                }
            }

            // 2. Disable EVERY camera that is not the XR Main Camera (even active ones
            //    whose parent is still on). The plumbob/headset only needs the XR camera.
            foreach (var cam in FindObjectsOfType<Camera>(true))
            {
                if (cam.CompareTag("MainCamera")) continue;
                if (cam.transform.root != null && cam.transform.root.name.Contains("XR Origin")) continue;
                if (cam.enabled || cam.gameObject.activeSelf)
                {
                    cam.enabled = false;
                    cam.gameObject.SetActive(false);
                    cams++;
                }
            }

            // 3. Disable old gameplay scripts.
            foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                if (mb.GetType().Namespace == "GestureThiefSystem" && mb.enabled)
                {
                    mb.enabled = false;
                    scripts++;
                }
            }

            if (thiefs + cams + scripts > 0)
                Debug.Log($"[LegacyCleanup] Disabled {thiefs} Thief(s), {cams} camera(s), {scripts} script(s).");
        }
    }
}
