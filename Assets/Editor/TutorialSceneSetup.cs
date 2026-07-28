using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click builder for the TUTORIAL experience (its own APK). Run:
/// EntryCue > Build Tutorial (Host + White Room).
///
/// Design: the tutorial reuses the exact chem-lab machinery (Building_TransitionCues drives the
/// cue flow, teleport, fades and the reality window) — only the content is tutorial-ish:
///   AR (passthrough)  -> TutorialCue_Entry  ("Betreten")
///   white room (VR)   -> TutorialCue        (practice: talking, panels, teleport)
///   close arrival     -> TutorialCue_Exit   (Live-Fenster, "Verlassen")
///   back to AR        -> tutorial done.
///
/// The command (idempotent, safe to re-run):
///   1. Generates Assets/Scenes/TutorialRoom.unity — a plain white room (floor, walls, ceiling,
///      light) with UserSpawnPoint, entryArrivalAnchor, exitAnchor and a fitted TeleportArea.
///   2. Copies TeleportationScene.unity -> TutorialHost.unity (only if missing), disables the
///      AR building placement (PositioningManager), and adds a TutorialController
///      (Building_TransitionCues) wired to the tutorial room + tutorial cues.
///   3. Adds both scenes to Build Settings.
/// Then: run "EntryCue > Apply Lecture-Navigation Context (T1-T13)" once (fills the tutorial
/// cues too) and build with only TutorialHost + TutorialRoom enabled.
/// </summary>
public static class TutorialSceneSetup
{
    const string RoomPath = "Assets/Scenes/TutorialRoom.unity";
    const string HostPath = "Assets/Scenes/TutorialHost.unity";
    const string SourceHost = "Assets/Scenes/TeleportationScene.unity";
    const string MatDir = "Assets/LLMCueAssets/Tutorial";
    const string MatPath = MatDir + "/TutorialWhite.mat";
    const string FloorMatPath = MatDir + "/TutorialFloor.mat";

    [MenuItem("EntryCue/Build Tutorial (Host + White Room)")]
    public static void Build()
    {
        BuildWhiteRoom();
        BuildHost();
        EnsureBuildSettings(HostPath, RoomPath);
        AssetDatabase.SaveAssets();

        // Leave the user LOOKING at the room: open it, select the floor and frame the Scene view
        // on it — so there is no way to end up staring at a blank Game view (the room has no
        // camera by design; the camera comes from TutorialHost at runtime).
        EditorSceneManager.OpenScene(RoomPath, OpenSceneMode.Single);
        var floor = GameObject.Find("Floor");
        if (floor != null) Selection.activeGameObject = floor;
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.Focus();
            sv.in2DMode = false;
            sv.Frame(new Bounds(new Vector3(0, 1.5f, 0), new Vector3(11, 6, 11)), false);
        }

        EditorUtility.DisplayDialog("Tutorial bereit",
            "TutorialRoom und TutorialHost sind erstellt und verkabelt.

" +
            "Du siehst den Übungsraum jetzt im SCENE-Tab. Der GAME-Tab bleibt hier immer schwarz " +
            "— der Raum hat absichtlich keine Kamera, sie kommt zur Laufzeit aus TutorialHost.

" +
            "Testen geht nur per Build & Run auf der Quest (nur TutorialHost + TutorialRoom aktiv). " +
            "Editor-Play ist bei dieser App nicht aussagekräftig.",
            "OK");
        Debug.Log("[TutorialSetup] Done. If not done yet, run 'EntryCue > Apply Lecture-Navigation Context (T1-T13)' once.");
    }

    // ---------- the white practice room (regenerated on every run) ----------

    static void BuildWhiteRoom()
    {
        if (!AssetDatabase.IsValidFolder(MatDir))
            AssetDatabase.CreateFolder("Assets/LLMCueAssets", "Tutorial");

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.color = new Color(0.92f, 0.92f, 0.94f); // off-white walls

        var floorMat = AssetDatabase.LoadAssetAtPath<Material>(FloorMatPath);
        if (floorMat == null)
        {
            floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(floorMat, FloorMatPath);
        }
        floorMat.color = new Color(0.55f, 0.57f, 0.60f); // grey floor — contrast so the room reads as a room

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // IMPORTANT: the room root must be the FIRST root object — LoadVRRoom uses
        // GetRootGameObjects()[0] and looks up UserSpawnPoint as its direct child.
        var root = new GameObject("TutorialRoom");

        Child(root, "UserSpawnPoint", new Vector3(0, 0, 0));
        Child(root, "entryArrivalAnchor", new Vector3(0.9f, 1.25f, 1.6f), yaw: 200f);
        Child(root, "exitAnchor", new Vector3(-0.9f, 1.25f, 1.6f), yaw: 160f);

        var area = Child(root, "TeleportArea", new Vector3(0, 1f, 0));
        area.transform.localScale = new Vector3(6.5f, 2f, 6.5f);
        area.AddComponent<TeleportArea>();

        // white box room: 8x8 m floor, 3 m walls, closed ceiling (passthrough must not leak in)
        Cube(root, floorMat, "Floor", new Vector3(0, -0.05f, 0), new Vector3(8, 0.1f, 8));
        Cube(root, mat, "Ceiling", new Vector3(0, 3.05f, 0),   new Vector3(8, 0.1f, 8));
        Cube(root, mat, "Wall_N",  new Vector3(0, 1.5f, 4.05f), new Vector3(8, 3, 0.1f));
        Cube(root, mat, "Wall_S",  new Vector3(0, 1.5f, -4.05f), new Vector3(8, 3, 0.1f));
        Cube(root, mat, "Wall_E",  new Vector3(4.05f, 1.5f, 0), new Vector3(0.1f, 3, 8));
        Cube(root, mat, "Wall_W",  new Vector3(-4.05f, 1.5f, 0), new Vector3(0.1f, 3, 8));

        // even, shadow-free light so the closed room is bright (directional ignores geometry)
        var sun = Child(root, "Sun", new Vector3(0, 2.8f, 0));
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(60f, -35f, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);

        EditorSceneManager.SaveScene(scene, RoomPath);
        Debug.Log($"[TutorialSetup] white room saved: {RoomPath}");
    }

    // ---------- the AR host (copy of the working TeleportationScene) ----------

    static void BuildHost()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HostPath) == null)
        {
            if (!AssetDatabase.CopyAsset(SourceHost, HostPath))
            { Debug.LogError($"[TutorialSetup] could not copy {SourceHost}"); return; }
        }

        var scene = EditorSceneManager.OpenScene(HostPath, OpenSceneMode.Single);

        // no AR building / spatial-anchor placement in the tutorial
        foreach (var pos in Object.FindObjectsByType<Positioner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (pos.gameObject.activeSelf) pos.gameObject.SetActive(false);

        var existing = GameObject.Find("TutorialController");
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject("TutorialController");
        var btc = go.AddComponent<Building_TransitionCues>();

        // the entry cue floats ~1.8 m in front of where the user faces at app start
        var anchor = new GameObject("entryAnchor");
        anchor.transform.SetParent(go.transform, false);
        anchor.transform.localPosition = new Vector3(0, 1.2f, 1.8f);
        anchor.transform.localRotation = Quaternion.Euler(0, 180f, 0); // toward the user

        var so = new SerializedObject(btc);
        Set(so, "vrSceneName", "TutorialRoom");
        Set(so, "entryCuePath", "LLMCues/TutorialCue_Entry");
        Set(so, "entryArrivalCuePath", "LLMCues/TutorialCue");
        Set(so, "exitCuePath", "LLMCues/TutorialCue_Exit");
        Set(so, "startArrivalCuePath", "");
        Set(so, "exitArrivalCuePath", "");
        Set(so, "navigationDestination", "Tutorial");
        Set(so, "vrRoomTitle", "Übungsraum");
        var hx = so.FindProperty("teleportWalkableHalfX"); if (hx != null) hx.floatValue = 3.2f;
        var hz = so.FindProperty("teleportWalkableHalfZ"); if (hz != null) hz.floatValue = 3.2f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[TutorialSetup] host ready: {HostPath} (PositioningManager off, TutorialController wired)");
    }

    // ---------- helpers ----------

    static GameObject Child(GameObject parent, string name, Vector3 localPos, float yaw = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        if (yaw != 0f) go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        return go;
    }

    static void Cube(GameObject parent, Material mat, string name, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void Set(SerializedObject so, string prop, string value)
    {
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning($"[TutorialSetup] missing field '{prop}'"); return; }
        p.stringValue = value;
    }

    static void EnsureBuildSettings(params string[] paths)
    {
        var list = EditorBuildSettings.scenes.ToList();
        foreach (var p in paths)
        {
            var idx = list.FindIndex(s => s.path == p);
            if (idx >= 0) list[idx] = new EditorBuildSettingsScene(p, true);
            else list.Add(new EditorBuildSettingsScene(p, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }
}
