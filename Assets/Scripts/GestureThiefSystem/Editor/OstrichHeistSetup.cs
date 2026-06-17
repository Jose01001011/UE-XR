// OstrichHeistSetup.cs  (Editor only)
// ---------------------------------------------------
// Does the full "adoption surgery" on SampleScene automatically
// whenever you press Play (via the [InitializeOnLoad] hook below),
// and is also available as a manual menu: Ostrich Heist > Setup Scene.
//
// What it creates / attaches:
//   - Thief     : thief.fbx + NavMeshAgent + ThiefController + ThiefHitReaction
//   - Ostrich   : adds OstrichDetection + OstrichAttack + OstrichPatrol + ProximityRings
//                 to the existing Ostrich GameObject (keeps its mesh / old scripts)
//   - Signaller : adds ScoutNPC + SignallerWatcher to existing Signaller GameObject
//   - Egg       : small cream sphere at nest position
//   - GameManager / Bootstrap: auto-wiring at runtime
//   - GestureInput: KeyboardGestureInput on XR Origin (VR gestures via MetaHandGestureInput)
//
// Safe to re-run — existing objects/components are reused, never duplicated.

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using GestureThiefSystem;

// ── Auto-run on Play ───────────────────────────────────────────────────────
// Registers a callback so SetupScene() fires automatically when the user
// presses Play. No menu click required.
[InitializeOnLoad]
public class OstrichHeistAutoSetup
{
    static OstrichHeistAutoSetup()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // ExitingEditMode = still in edit mode, all editor APIs available
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name == "SampleScene")
                OstrichHeistSetup.SetupScene(showDialog: false); // silent — no popup
        }
    }
}

public class OstrichHeistSetup : EditorWindow
{
    // Asset paths (relative to Assets/)
    const string THIEF_FBX      = "Assets/_OLD Village Scene/thief.fbx";
    const string OSTRICH_FBX    = "Assets/_OLD Village Scene/Ostrich.fbx";
    const string SIGNALLER_FBX  = "Assets/_OLD Village Scene/Signaller.fbx";
    const string THIEF_ANIM     = "Assets/_OLD Village Scene/ThiefAnimator.controller";
    const string SIG_ANIM       = "Assets/_OLD Village Scene/SigIdleAnimator.controller";
    const string THIEF_TEX      = "Assets/_OLD Village Scene/defaultMat_Base_Color.png";

    // Scene-space positions
    static readonly Vector3 THIEF_POS     = new Vector3(8f, 0f, 0f);
    static readonly Vector3 OSTRICH_POS   = new Vector3(0f, 0f, 0f);
    static readonly Vector3 SIGNALLER_POS = new Vector3(4f, 0f, 4f);
    static readonly Vector3 EGG_POS       = new Vector3(-5f, 0.3f, 0f);

    // -------------------------------------------------------------------------
    [MenuItem("Ostrich Heist/Setup Scene")]
    public static void SetupScene() => SetupScene(showDialog: true);

    // Called by auto-hook (no popup) and by the menu item (with popup).
    public static void SetupScene(bool showDialog)
    {
        Debug.Log("=== Ostrich Heist: Begin Scene Setup ===");

        GameObject thief     = SetupThief();
        GameObject ostrich   = SetupOstrich();
        GameObject signaller = SetupSignaller();
        GameObject egg       = SetupEgg();
        GameObject gm        = SetupGameManager(thief);
        SetupGestureInput();
        SetupBootstrap();

        // Wire cross-references
        WireOstrichDetection(ostrich, thief, signaller);
        WireSignallerWatcher(signaller, ostrich, egg, thief);
        WireThiefController(thief, egg);
        WireGameManager(gm, thief);

        EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            .GetRootGameObjects().FirstOrDefault());
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("=== Ostrich Heist: Setup Complete! ===");
        Debug.Log("  Bake NavMesh: Window > AI > Navigation > Bake");
        Debug.Log("  Keys: H=Hide  S=Stop  G=Go  C=Crouch  R=Run");

        if (showDialog)
            EditorUtility.DisplayDialog("Setup Complete!",
                "Scene wired!\n\nBake NavMesh: Window > AI > Navigation > Bake\n\nKeys: H=Hide  S=Stop  G=Go  C=Crouch  R=Run", "OK");
    }

    // -------------------------------------------------------------------------
    // THIEF
    // -------------------------------------------------------------------------
    static GameObject SetupThief()
    {
        // Reuse existing
        var existing = FindByComponent<ThiefController>();
        if (existing != null)
        {
            Debug.Log("[Setup] Thief already in scene: " + existing.name);
            EnsureThiefComponents(existing);
            return existing;
        }

        // Instantiate from FBX
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(THIEF_FBX);
        GameObject thief;
        if (fbx != null)
        {
            thief = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            thief.name = "Thief";
        }
        else
        {
            Debug.LogWarning("[Setup] thief.fbx not found — using capsule placeholder.");
            thief = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            thief.name = "Thief";
            thief.transform.localScale = new Vector3(0.4f, 0.9f, 0.4f);
        }
        thief.transform.position = THIEF_POS;
        thief.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // FBX often huge

        // Texture
        ApplyThiefTexture(thief);

        EnsureThiefComponents(thief);

        // Tag & Layer
        SetTagSafe(thief, "Thief");

        Undo.RegisterCreatedObjectUndo(thief, "Create Thief");
        Debug.Log("[Setup] Thief created.");
        return thief;
    }

    static void EnsureThiefComponents(GameObject thief)
    {
        // Scale guard — FBX default import scale is 0.01
        if (thief.transform.localScale == Vector3.one)
            thief.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // NavMeshAgent
        if (!thief.GetComponent<NavMeshAgent>())
            thief.AddComponent<NavMeshAgent>();

        // Animator
        var anim = thief.GetComponent<Animator>();
        if (anim == null) anim = thief.AddComponent<Animator>();
        var thiefCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(THIEF_ANIM);
        if (thiefCtrl != null && anim.runtimeAnimatorController == null)
            anim.runtimeAnimatorController = thiefCtrl;

        // ThiefController
        if (!thief.GetComponent<ThiefController>())
            thief.AddComponent<ThiefController>();

        // ThiefHitReaction
        if (!thief.GetComponent<ThiefHitReaction>())
            thief.AddComponent<ThiefHitReaction>();

        // Capsule collider so trigger zones can detect it
        if (!thief.GetComponent<Collider>())
        {
            var col = thief.AddComponent<CapsuleCollider>();
            col.height = 180f; // FBX units = cm, scale 0.01 => 1.8m
            col.radius = 20f;
            col.center = new Vector3(0, 90f, 0);
        }
    }

    static void ApplyThiefTexture(GameObject thief)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(THIEF_TEX);
        if (tex == null) { Debug.LogWarning("[Setup] Thief texture not found: " + THIEF_TEX); return; }

        foreach (var r in thief.GetComponentsInChildren<Renderer>(true))
        {
            if (r.sharedMaterial == null) continue;
            // Don't create a new material instance if texture is already assigned
            if (r.sharedMaterial.mainTexture == tex) continue;

            var mat = new Material(r.sharedMaterial);
            // mainTexture works for Standard; _BaseMap is the URP Lit slot
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            r.sharedMaterial = mat;
        }
    }

    // -------------------------------------------------------------------------
    // OSTRICH
    // -------------------------------------------------------------------------
    static GameObject SetupOstrich()
    {
        // 1. Already has the new component — nothing to do
        var existing = FindByComponent<OstrichDetection>();
        if (existing != null)
        {
            Debug.Log("[Setup] Ostrich (new system) already in scene: " + existing.name);
            EnsureOstrichComponents(existing);
            return existing;
        }

        // 2. Existing scene Ostrich (old OstrichAI script) — add new components to it
        var byName = GameObject.Find("Ostrich");
        if (byName != null)
        {
            Debug.Log("[Setup] Found existing Ostrich — adding GestureThiefSystem components.");
            EnsureOstrichComponents(byName);
            return byName;
        }

        // 3. Nothing exists — instantiate from FBX
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(OSTRICH_FBX);
        GameObject ostrich;
        if (fbx != null)
        {
            ostrich = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            ostrich.name = "Ostrich";
        }
        else
        {
            Debug.LogWarning("[Setup] Ostrich.fbx not found — using sphere placeholder.");
            ostrich = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ostrich.name = "Ostrich";
        }
        ostrich.transform.position = OSTRICH_POS;
        ostrich.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        EnsureOstrichComponents(ostrich);
        Undo.RegisterCreatedObjectUndo(ostrich, "Create Ostrich");
        Debug.Log("[Setup] Ostrich created.");
        return ostrich;
    }

    static void EnsureOstrichComponents(GameObject ostrich)
    {
        // Main detection script
        if (!ostrich.GetComponent<OstrichDetection>())
            ostrich.AddComponent<OstrichDetection>();

        // Proximity ring (warning, yellow)
        if (!ostrich.GetComponent<ProximityRing>())
        {
            var ring = ostrich.AddComponent<ProximityRing>();
            var so = new SerializedObject(ring);
            so.FindProperty("radius").floatValue = 6f;
            so.FindProperty("color").colorValue = Color.yellow;
            so.ApplyModifiedProperties();
        }

        // WarningZone child
        GameObject warnGO = GetOrCreateChild(ostrich, "WarningZone");
        var warnCol = GetOrAdd<SphereCollider>(warnGO);
        warnCol.isTrigger = true;
        warnCol.radius = 600f; // FBX scale 0.01 -> 6m
        var warnFwd = GetOrAdd<TriggerForwarder>(warnGO);
        SetEnumField(warnFwd, "zoneType", 0); // 0 = Warning

        // DangerZone child
        GameObject dangerGO = GetOrCreateChild(ostrich, "DangerZone");
        var dangerCol = GetOrAdd<SphereCollider>(dangerGO);
        dangerCol.isTrigger = true;
        dangerCol.radius = 250f; // ~2.5m
        var dangerFwd = GetOrAdd<TriggerForwarder>(dangerGO);
        SetEnumField(dangerFwd, "zoneType", 1); // 1 = Danger

        // Wire zone colliders to OstrichDetection
        var detection = ostrich.GetComponent<OstrichDetection>();
        var dso = new SerializedObject(detection);
        dso.FindProperty("warningZone").objectReferenceValue = warnCol;
        dso.FindProperty("dangerZone").objectReferenceValue  = dangerCol;
        dso.ApplyModifiedProperties();
    }

    // -------------------------------------------------------------------------
    // SIGNALLER
    // -------------------------------------------------------------------------
    static GameObject SetupSignaller()
    {
        // 1. Already has the new component
        var existing = FindByComponent<ScoutNPC>();
        if (existing != null)
        {
            Debug.Log("[Setup] Signaller (new system) already in scene: " + existing.name);
            return existing;
        }

        // 2. Existing scene Signaller (old SignallerAnimator) — add new components
        var byName = GameObject.Find("Signaller");
        GameObject sig;
        if (byName != null)
        {
            Debug.Log("[Setup] Found existing Signaller — adding GestureThiefSystem components.");
            sig = byName;
        }
        else
        {
            // 3. Instantiate from FBX
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(SIGNALLER_FBX);
            if (fbx != null)
            {
                sig = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                sig.name = "Signaller";
            }
            else
            {
                sig = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                sig.name = "Signaller";
            }
            sig.transform.position = SIGNALLER_POS;
            sig.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            Undo.RegisterCreatedObjectUndo(sig, "Create Signaller");
        }

        // Ensure Animator (may already exist from old setup)
        var anim = GetOrAdd<Animator>(sig);
        var sigCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SIG_ANIM);
        if (sigCtrl != null && anim.runtimeAnimatorController == null)
            anim.runtimeAnimatorController = sigCtrl;

        // ScoutNPC + SignallerWatcher
        var scout = GetOrAdd<ScoutNPC>(sig);
        var sso = new SerializedObject(scout);
        sso.FindProperty("scoutAnimator").objectReferenceValue = anim;
        sso.ApplyModifiedProperties();
        GetOrAdd<SignallerWatcher>(sig);

        Debug.Log("[Setup] Signaller configured.");
        return sig;
    }

    // -------------------------------------------------------------------------
    // EGG
    // -------------------------------------------------------------------------
    static GameObject SetupEgg()
    {
        // Look for existing egg by name only (FindWithTag throws if tag is undefined)
        var existing = GameObject.Find("Egg");
        if (existing != null)
        {
            Debug.Log("[Setup] Egg already in scene: " + existing.name);
            return existing;
        }

        var egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        egg.name = "Egg";
        egg.transform.position = EGG_POS;
        egg.transform.localScale = new Vector3(0.15f, 0.2f, 0.15f);

        // Creamy white material — try URP names used by Unity 6, fall back to Standard
        Shader eggShader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("URP/Lit")
                        ?? Shader.Find("Standard")
                        ?? Shader.Find("Diffuse");
        if (eggShader != null)
        {
            var mat = new Material(eggShader);
            mat.color = new Color(0.96f, 0.93f, 0.82f);
            egg.GetComponent<Renderer>().sharedMaterial = mat;
        }

        SetTagSafe(egg, "Egg");

        Undo.RegisterCreatedObjectUndo(egg, "Create Egg");
        Debug.Log("[Setup] Egg created at " + EGG_POS);
        return egg;
    }

    // -------------------------------------------------------------------------
    // GAME MANAGER
    // -------------------------------------------------------------------------
    static GameObject SetupGameManager(GameObject thief)
    {
        var existing = FindByComponent<GameManager>();
        if (existing != null)
        {
            Debug.Log("[Setup] GameManager already in scene.");
            return existing;
        }

        var gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();

        // --- Build minimal Canvas UI ---
        // We create world-space canvas so it works in VR
        var canvas = new GameObject("GameCanvas");
        var canvasComp = canvas.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.WorldSpace;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        canvas.transform.localScale = Vector3.one * 0.005f;
        canvas.transform.position = new Vector3(0f, 1.6f, 2f);

        // LoseScreen
        var loseScreen = CreateTextPanel(canvas.transform, "LoseScreen",
            "YOU GOT CAUGHT\nTry Again", Color.red);

        // WinScreen
        var winScreen = CreateTextPanel(canvas.transform, "WinScreen",
            "EGG STOLEN!\nYou Win!", Color.green);

        loseScreen.SetActive(false);
        winScreen.SetActive(false);

        // Wire to GameManager
        var gmComp = gm.GetComponent<GameManager>();
        var so = new SerializedObject(gmComp);
        so.FindProperty("winScreen").objectReferenceValue  = winScreen;
        so.FindProperty("loseScreen").objectReferenceValue = loseScreen;
        so.FindProperty("thief").objectReferenceValue      = thief?.GetComponent<ThiefController>();
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(gm, "Create GameManager");
        Debug.Log("[Setup] GameManager created.");
        return gm;
    }

    static GameObject CreateTextPanel(Transform parent, string name, string text, Color color)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 200);
        rect.anchoredPosition = Vector2.zero;

        var bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(panel.transform, false);
        var trect = textGO.AddComponent<RectTransform>();
        trect.anchorMin = Vector2.zero;
        trect.anchorMax = Vector2.one;
        trect.offsetMin = trect.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 48;
        tmp.color = color;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        return panel;
    }

    // -------------------------------------------------------------------------
    // GESTURE INPUT
    // -------------------------------------------------------------------------
    static void SetupGestureInput()
    {
        // Find XR Origin / XR Rig
        // Search by component first so name doesn't matter
        var xrOrigin = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>()?.gameObject
                    ?? GameObject.Find("XR Origin (VR)")
                    ?? GameObject.Find("XR Origin")
                    ?? GameObject.Find("XR Origin (XR Rig)")
                    ?? GameObject.Find("XRRig");

        GameObject inputHost;
        if (xrOrigin != null)
        {
            inputHost = xrOrigin;
            Debug.Log("[Setup] Attaching gesture input to " + xrOrigin.name);
        }
        else
        {
            inputHost = new GameObject("GestureInputHost");
            inputHost.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(inputHost, "Create GestureInputHost");
            Debug.LogWarning("[Setup] XR Origin not found — created GestureInputHost instead.");
        }

        GetOrAdd<VRGestureInput>(inputHost);

        // Keyboard fallback always present (VRGestureInput disables it at runtime)
        GetOrAdd<KeyboardGestureInput>(inputHost);
    }

    // -------------------------------------------------------------------------
    // BOOTSTRAP
    // -------------------------------------------------------------------------
    static void SetupBootstrap()
    {
        if (FindByComponent<GestureThiefBootstrap>() != null)
        {
            Debug.Log("[Setup] Bootstrap already in scene.");
            return;
        }
        var go = new GameObject("Bootstrap");
        go.AddComponent<GestureThiefBootstrap>();
        Undo.RegisterCreatedObjectUndo(go, "Create Bootstrap");
        Debug.Log("[Setup] Bootstrap created.");
    }

    // -------------------------------------------------------------------------
    // WIRING
    // -------------------------------------------------------------------------
    static void WireOstrichDetection(GameObject ostrich, GameObject thief, GameObject signaller)
    {
        var detection = ostrich?.GetComponent<OstrichDetection>();
        if (detection == null) return;
        var so = new SerializedObject(detection);
        if (thief != null)
            so.FindProperty("thief").objectReferenceValue = thief.GetComponent<ThiefController>();
        if (signaller != null)
            so.FindProperty("scout").objectReferenceValue = signaller.GetComponent<ScoutNPC>();
        so.ApplyModifiedProperties();
        Debug.Log("[Setup] OstrichDetection wired.");
    }

    static void WireSignallerWatcher(GameObject signaller, GameObject ostrich, GameObject egg, GameObject thief)
    {
        var watcher = signaller?.GetComponent<SignallerWatcher>();
        if (watcher == null) return;
        var so = new SerializedObject(watcher);
        if (ostrich  != null) so.FindProperty("ostrich").objectReferenceValue = ostrich.transform;
        if (egg      != null) so.FindProperty("nest").objectReferenceValue    = egg.transform;
        if (thief    != null) so.FindProperty("thief").objectReferenceValue   = thief.transform;
        var scout = signaller.GetComponent<ScoutNPC>();
        if (scout != null) so.FindProperty("scout").objectReferenceValue      = scout;
        so.ApplyModifiedProperties();
        Debug.Log("[Setup] SignallerWatcher wired.");
    }

    static void WireThiefController(GameObject thief, GameObject egg)
    {
        var tc = thief?.GetComponent<ThiefController>();
        if (tc == null || egg == null) return;
        var so = new SerializedObject(tc);
        so.FindProperty("eggObjective").objectReferenceValue = egg.transform;
        so.ApplyModifiedProperties();

        // Wire ThiefController events -> GameManager
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            // UnityEvent wiring via SerializedObject
            var gmSO = new SerializedObject(gm);
            gmSO.FindProperty("thief").objectReferenceValue = tc;
            gmSO.ApplyModifiedProperties();
        }
        Debug.Log("[Setup] ThiefController wired to egg.");
    }

    static void WireGameManager(GameObject gm, GameObject thief)
    {
        var gmComp = gm?.GetComponent<GameManager>();
        if (gmComp == null || thief == null) return;
        var so = new SerializedObject(gmComp);
        so.FindProperty("thief").objectReferenceValue = thief.GetComponent<ThiefController>();
        so.ApplyModifiedProperties();
    }

    // -------------------------------------------------------------------------
    // BAKE NAV MESH (separate menu item)
    // -------------------------------------------------------------------------
    [MenuItem("Ostrich Heist/Bake NavMesh")]
    public static void BakeNavMesh()
    {
#pragma warning disable CS0618
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore CS0618
        Debug.Log("[Setup] NavMesh baked.");
    }

    // -------------------------------------------------------------------------
    // UTILITIES
    // -------------------------------------------------------------------------
    static GameObject FindByComponent<T>() where T : Component
    {
        var c = Object.FindAnyObjectByType<T>();
        return c != null ? c.gameObject : null;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static GameObject GetOrCreateChild(GameObject parent, string childName)
    {
        var existing = parent.transform.Find(childName);
        if (existing != null) return existing.gameObject;
        var child = new GameObject(childName);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    static void SetTagSafe(GameObject go, string tag)
    {
        try { go.tag = tag; }
        catch { Debug.LogWarning("[Setup] Tag '" + tag + "' not defined. Add it in Edit > Project Settings > Tags & Layers."); }
    }

    static void SetEnumField(Component comp, string fieldName, int value)
    {
        var so = new SerializedObject(comp);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.enumValueIndex = value; so.ApplyModifiedProperties(); }
    }
}
