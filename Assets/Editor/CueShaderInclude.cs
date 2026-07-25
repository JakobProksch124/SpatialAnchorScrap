using UnityEditor;
using UnityEngine;

/// <summary>
/// The cue creates its RoundedRect / PassthroughHole materials at runtime via Shader.Find, so
/// Unity would strip those shaders from the build. Run this once to add them to the project's
/// Always-Included Shaders so they ship in the APK.
/// </summary>
public static class CueShaderInclude
{
    [MenuItem("EntryCue/Include Cue Shaders In Build")]
    public static void Include()
    {
        Ensure("EntryCue/PassthroughHole");
        Ensure("EntryCue/RoundedRect");
        AssetDatabase.SaveAssets();
    }

    private static void Ensure(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) { Debug.LogError($"[CueShaderInclude] Shader '{shaderName}' not found."); return; }

        var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        var so = new SerializedObject(gs);
        var list = so.FindProperty("m_AlwaysIncludedShaders");
        for (var i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
            {
                Debug.Log($"[CueShaderInclude] '{shaderName}' already included.");
                return;
            }

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
        so.ApplyModifiedProperties();
        Debug.Log($"[CueShaderInclude] Added '{shaderName}' to Always-Included Shaders.");
    }
}
