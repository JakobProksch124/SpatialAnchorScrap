using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Logs when the user clicks the entry cue's "Jetzt nicht" button. The cue deliberately
/// stays visible (dismissing an entry cue is not allowed in the study) — but the ATTEMPT
/// must show up in the log files. Attached at runtime by CueController, so every cue
/// prefab gets it without prefab surgery.
/// </summary>
public class DismissButtonFunctionality : MonoBehaviour
{
    void Start()
    {
        var ray = GetComponent<RayInteractable>();
        if (ray == null) ray = GetComponentInChildren<RayInteractable>();
        if (ray == null)
        {
            Debug.LogWarning("[DismissButton] no RayInteractable found - dismiss clicks won't be logged.");
            return;
        }

        ray.WhenStateChanged += state =>
        {
            if (state.NewState == InteractableState.Select)
                CueLogger.Event("dismiss_requested_ignored", "button:jetzt_nicht");
        };
    }
}
