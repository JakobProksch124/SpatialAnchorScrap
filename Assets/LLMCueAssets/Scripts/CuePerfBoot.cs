using UnityEngine;

/// <summary>
/// Enables foveated rendering at startup (it was OFF). Foveation drops resolution in the
/// periphery — where you're not looking — which frees a lot of GPU, so the headset can hold a
/// higher (sharper) eye-buffer resolution where you ARE looking (e.g. reading cue text).
/// Dynamic foveation eases it off when the GPU has headroom. Global OVR setting; set once.
/// </summary>
public static class CuePerfBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.High;
        OVRManager.useDynamicFoveatedRendering = true;
        Debug.Log("[CuePerfBoot] Foveated rendering = High (dynamic).");
    }
}
