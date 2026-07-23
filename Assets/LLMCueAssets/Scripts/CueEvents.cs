using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The cue's integration surface for the host project. Wire your transition
/// technique onto <see cref="onStartTransition"/> in the Inspector — it fires
/// once the user has chosen to enter (Enter-VR button OR the voice intent
/// "take me in") and the cue has finished fading away.
///
/// This is the single hook a host project needs: the cue knows nothing about
/// what "enter" actually does — it just announces that the user wants in.
/// </summary>
public class CueEvents : MonoBehaviour
{
    [Tooltip("Fires AFTER the cue has faded away, when the user chose to enter. " +
             "Drop your transition-start function here.")]
    public UnityEvent onStartTransition;

    public void RaiseStartTransition() => onStartTransition?.Invoke();

    /// <summary>True if the host wired at least one listener in the Inspector.</summary>
    public bool HasTransitionListener =>
        onStartTransition != null && onStartTransition.GetPersistentEventCount() > 0;
}
