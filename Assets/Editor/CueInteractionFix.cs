using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Oculus.Interaction;

/// <summary>
/// One-shot repair for the "KeyNotFoundException: '...(PointableCanvas)' not present in the
/// dictionary" storm that jams interaction (incl. teleport). Meta's PointableCanvasModule must
/// be a single scene-wide singleton (it asserts "at most one in the scene"), but our cue prefabs
/// each baked one in — so every spawned cue installs another module, they fight over the static
/// instance, and registrations/lookups desync.
///
/// Fix: remove the module from the cue prefabs, and guarantee exactly ONE module (on an
/// EventSystem) in each scene that shows cues. Run via the menu, then rebuild.
/// </summary>
public static class CueInteractionFix
{
    static readonly string[] CueScenes =
    {
        "Assets/Scenes/TeleportationScene.unity",
        "Assets/Scenes/Laboratory.unity",
        "Assets/Scenes/Bridge.unity",
        "Assets/Scenes/Lecture.unity",
        "Assets/Scenes/TCTestScene.unity",
    };

    [MenuItem("EntryCue/Fix Duplicate PointableCanvasModules")]
    public static void Fix()
    {
        // 1) strip the baked-in module from every cue prefab
        int cleanedPrefabs = 0, removed = 0;
        foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/LLMCues" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var root = PrefabUtility.LoadPrefabContents(path);
            var changed = false;
            foreach (var m in root.GetComponentsInChildren<PointableCanvasModule>(true))
            {
                Object.DestroyImmediate(m, true);
                removed++; changed = true;
            }
            if (changed) { PrefabUtility.SaveAsPrefabAsset(root, path); cleanedPrefabs++; }
            PrefabUtility.UnloadPrefabContents(root);
        }

        // 2) ensure exactly one module (on an EventSystem) in each cue-bearing scene
        var prev = EditorSceneManager.GetActiveScene().path;
        foreach (var sp in CueScenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(sp) == null) continue; // scene doesn't exist
            var scene = EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);

            var modules = Object.FindObjectsByType<PointableCanvasModule>(FindObjectsSortMode.None);
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            // the module we keep lives on the EventSystem
            PointableCanvasModule keep = es.GetComponent<PointableCanvasModule>();
            if (keep == null) keep = es.gameObject.AddComponent<PointableCanvasModule>();

            foreach (var m in modules) if (m != keep) Object.DestroyImmediate(m);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (!string.IsNullOrEmpty(prev)) EditorSceneManager.OpenScene(prev, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        Debug.Log($"[CueInteractionFix] Removed PointableCanvasModule from {cleanedPrefabs} cue prefabs " +
                  $"({removed} components); ensured one module per cue scene. Rebuild to apply.");
    }
}
