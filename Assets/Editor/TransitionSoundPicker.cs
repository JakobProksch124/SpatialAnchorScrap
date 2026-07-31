using UnityEditor;
using UnityEngine;

/// <summary>
/// Switches which of Daniel's three transition sounds is active. TransitionSound.Play() loads
/// Resources/TransitionSound, so this simply copies the chosen file over that slot.
/// Menu: EntryCue > Transition Sound > Use One / Two / Three.
/// </summary>
public static class TransitionSoundPicker
{
    const string Dir = "Assets/LLMCueAssets/Sounds/TransitionSound_";
    const string Active = "Assets/Resources/TransitionSound.mp3";

    [MenuItem("EntryCue/Transition Sound/Use One")]   static void One()   => Use("one");
    [MenuItem("EntryCue/Transition Sound/Use Two")]   static void Two()   => Use("two");
    [MenuItem("EntryCue/Transition Sound/Use Three")] static void Three() => Use("three");

    static void Use(string which)
    {
        var src = Dir + which + ".mp3";
        if (AssetDatabase.LoadAssetAtPath<AudioClip>(src) == null)
        { Debug.LogError($"[TransitionSound] missing {src}"); return; }

        AssetDatabase.DeleteAsset(Active);
        if (!AssetDatabase.CopyAsset(src, Active))
        { Debug.LogError($"[TransitionSound] could not copy {src} -> {Active}"); return; }

        AssetDatabase.Refresh();
        Debug.Log($"[TransitionSound] active transition sound = {which}. Rebuild to hear it.");
    }
}
