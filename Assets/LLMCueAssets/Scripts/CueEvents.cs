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
    public UnityEvent onStartTransition;
    public UnityEvent onCloseCue;

    public void RaiseStartTransition() => onStartTransition?.Invoke();
    public void RaiseCloseCue() => onCloseCue?.Invoke();
}
