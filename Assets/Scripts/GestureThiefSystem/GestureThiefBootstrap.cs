// GestureThiefBootstrap.cs
// Central runtime wirer for the Gesture Thief system.
//
// PURPOSE:
//   Attaches to any persistent GameObject in SampleScene. On Awake it locates
//   every GestureThiefSystem actor via FindAnyObjectByType and injects the
//   cross-references between them, so no manual Inspector wiring is needed.
//   It also corrects the thief's scale if the FBX was imported at a bad size,
//   repositions the thief where the player can see it, enables all its
//   renderers, and ensures a ContinuousMoveProvider exists on the XR Origin
//   so the player can walk.
//
// USAGE:
//   1. Add this component to a GameObject in SampleScene (e.g. "Bootstrap").
//   2. Press Play. Check the Console for [Bootstrap] log lines.
//   3. If any actor is "not found", make sure its GameObject is in the scene
//      with the corresponding script attached.
//
// NAVMESH:
//   NavMesh must be baked once in the Editor:
//   Window > AI > Navigation > Bake
//   The thief and ostrich NavMeshAgents will log warnings until it is baked.

using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

namespace GestureThiefSystem
{
    public class GestureThiefBootstrap : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Optional manual overrides — leave empty and Awake finds them.
        // ----------------------------------------------------------------
        [Header("Override References (leave empty = auto-locate)")]
        [SerializeField] private ThiefController  thief;
        [SerializeField] private OstrichDetection ostrichDetection;
        [SerializeField] private OstrichPatrol    ostrichPatrol;
        [SerializeField] private OstrichAttack    ostrichAttack;
        [SerializeField] private ScoutNPC         scout;
        [SerializeField] private SignallerWatcher signallerWatcher;
        [SerializeField] private GameManager      gameManager;
        [SerializeField] private Transform        egg;

        [Header("Thief Positioning")]
        [Tooltip("Where to place the thief relative to the egg when the scene starts.")]
        [SerializeField] private Vector3 thiefOffsetFromEgg = new Vector3(8f, 0f, 0f);

        [Tooltip("Target world height for the thief. FBX models imported at wrong scale "
               + "are auto-corrected to this height.")]
        [SerializeField] private float targetThiefHeight = 1.8f;

        [Header("Player Locomotion")]
        [Tooltip("Walk speed added to ContinuousMoveProvider if one does not already exist.")]
        [SerializeField] private float playerWalkSpeed = 1.5f;

        // ----------------------------------------------------------------
        private void Awake()
        {
            AutoResolve();
            WireAll();
            FixThief();
            EnsurePlayerLocomotion();

            Debug.Log("[Bootstrap] Done. Press Play — use H/S/G/C/R keys to test gestures.");
        }

        // ----------------------------------------------------------------
        // 1. RESOLVE — find actors that weren't set in the Inspector
        // ----------------------------------------------------------------
        private void AutoResolve()
        {
            if (!thief)            thief            = FindAnyObjectByType<ThiefController>();
            if (!ostrichDetection) ostrichDetection = FindAnyObjectByType<OstrichDetection>();
            if (!ostrichPatrol)    ostrichPatrol    = FindAnyObjectByType<OstrichPatrol>();
            if (!ostrichAttack)    ostrichAttack    = FindAnyObjectByType<OstrichAttack>();
            if (!scout)            scout            = FindAnyObjectByType<ScoutNPC>();
            if (!signallerWatcher) signallerWatcher = FindAnyObjectByType<SignallerWatcher>();
            if (!gameManager)      gameManager      = FindAnyObjectByType<GameManager>();

            if (!egg)
            {
                var eggGO = GameObject.Find("Egg");
                if (eggGO) egg = eggGO.transform;
            }

            Debug.Log($"[Bootstrap] Resolved — Thief:{Bool(thief)}  Ostrich:{Bool(ostrichDetection)}"
                    + $"  Scout:{Bool(scout)}  GM:{Bool(gameManager)}  Egg:{Bool(egg)}");
        }

        // ----------------------------------------------------------------
        // 2. WIRE — inject references between components via reflection
        // ----------------------------------------------------------------
        private void WireAll()
        {
            // ThiefController → egg
            if (thief && egg)
                SetField(thief, "eggObjective", egg);

            // OstrichDetection → thief, scout
            if (ostrichDetection)
            {
                if (thief) SetField(ostrichDetection, "thief", thief);
                if (scout) SetField(ostrichDetection, "scout", scout);
            }

            // OstrichAttack → thief + ThiefHitReaction
            if (ostrichAttack && thief)
            {
                SetField(ostrichAttack, "thief", thief);
                var hit = thief.GetComponentInChildren<ThiefHitReaction>(true)
                       ?? FindAnyObjectByType<ThiefHitReaction>();
                if (hit) SetField(ostrichAttack, "thiefHit", hit);
            }

            // OstrichPatrol → thief transform
            if (ostrichPatrol && thief)
                ostrichPatrol.SetThiefTarget(thief.transform);

            // SignallerWatcher → ostrich, egg, thief, scout
            if (signallerWatcher)
            {
                Transform ostrichT = ostrichDetection ? ostrichDetection.transform : null;
                if (ostrichT) SetField(signallerWatcher, "ostrich", ostrichT);
                if (egg)      SetField(signallerWatcher, "nest",    egg);
                if (thief)    SetField(signallerWatcher, "thief",   thief.transform);
                if (scout)    SetField(signallerWatcher, "scout",   scout);
            }

            // GameManager → thief
            if (gameManager && thief)
                SetField(gameManager, "thief", thief);

            Debug.Log("[Bootstrap] Cross-references wired.");
        }

        // ----------------------------------------------------------------
        // 3. FIX THIEF — scale, position, renderers
        // ----------------------------------------------------------------
        private void FixThief()
        {
            if (!thief) { Debug.LogWarning("[Bootstrap] Thief not found — skipping fix."); return; }

            var go        = thief.gameObject;
            var renderers = go.GetComponentsInChildren<Renderer>(true);

            // --- Ensure NavMeshAgent exists ---
            if (!go.GetComponent<NavMeshAgent>())
                go.AddComponent<NavMeshAgent>();

            // --- Auto-correct scale ---
            if (renderers.Length > 0)
            {
                // Compute combined world-space bounds
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                float h = bounds.size.y;

                if (h < 0.5f || h > 4f)          // outside sane human range
                {
                    float correctionFactor = targetThiefHeight / Mathf.Max(h, 0.001f);
                    Vector3 s = go.transform.localScale;
                    go.transform.localScale = s * correctionFactor;
                    Debug.Log($"[Bootstrap] Thief scale corrected: was {h:F3}m tall → now ~{targetThiefHeight}m.");
                }
                else
                {
                    Debug.Log($"[Bootstrap] Thief height {h:F2}m — no scale correction needed.");
                }
            }

            // --- Enable all renderers ---
            foreach (var r in renderers) r.enabled = true;

            // --- Position in front of egg ---
            if (egg)
            {
                Vector3 spawnPos = egg.position + thiefOffsetFromEgg;
                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                    go.transform.position = hit.position;
                else
                    go.transform.position = spawnPos;

                Debug.Log($"[Bootstrap] Thief placed at {go.transform.position}.");
            }
        }

        // ----------------------------------------------------------------
        // 4. LOCOMOTION — add ContinuousMoveProvider if missing
        // ----------------------------------------------------------------
        private void EnsurePlayerLocomotion()
        {
            var xrOrigin = FindAnyObjectByType<XROrigin>();
            if (!xrOrigin)
            {
                Debug.LogWarning("[Bootstrap] XR Origin not found — player locomotion skipped.");
                return;
            }

            // XRI 3.x: ContinuousMoveProvider lives in UnityEngine.XR.Interaction.Toolkit
            // We use reflection so the script compiles even if the assembly reference
            // changes between XRI versions.
            var providerType = System.Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider, " +
                "Unity.XR.Interaction.Toolkit");

            // Fallback: older XRI 2.x namespace
            if (providerType == null)
                providerType = System.Type.GetType(
                    "UnityEngine.XR.Interaction.Toolkit.ContinuousMoveProvider, " +
                    "Unity.XR.Interaction.Toolkit");

            if (providerType == null)
            {
                Debug.LogWarning("[Bootstrap] ContinuousMoveProvider type not found. "
                               + "Install XR Interaction Toolkit and import the Starter Assets sample.");
                return;
            }

            if (!xrOrigin.GetComponent(providerType))
            {
                var provider = xrOrigin.gameObject.AddComponent(providerType) as MonoBehaviour;
                if (provider != null)
                {
                    // Set move speed via reflection (field name is consistent across XRI versions)
                    var speedField = providerType.GetField("moveSpeed",
                        BindingFlags.Public | BindingFlags.Instance);
                    speedField?.SetValue(provider, playerWalkSpeed);

                    Debug.Log($"[Bootstrap] ContinuousMoveProvider added at speed {playerWalkSpeed}.");
                }
            }
            else
            {
                Debug.Log("[Bootstrap] ContinuousMoveProvider already present — player can walk.");
            }
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        // Reflection-based private field setter — works on any SerializeField
        // without needing public setters on every component.
        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            System.Type t = target.GetType();
            FieldInfo f   = null;
            while (f == null && t != null)
            {
                f = t.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                t = t.BaseType;
            }
            if (f != null)
                f.SetValue(target, value);
            else
                Debug.LogWarning($"[Bootstrap] Field '{fieldName}' not found on {target.GetType().Name}.");
        }

        private static string Bool(object o) => o != null ? "OK" : "MISSING";
    }
}
