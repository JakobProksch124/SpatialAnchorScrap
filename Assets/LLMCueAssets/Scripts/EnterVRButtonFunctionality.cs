using Oculus.Interaction;
using UnityEngine;

public class EnterVRButtonFunctionality : MonoBehaviour
{
    void Start()
    {
        var ray = GetComponent<RayInteractable>();
        var cueEvents = GetComponentInParent<CueEvents>();
        var log = CueLogger.For(this);

        var curr = transform.parent;
        GameObject foundParent = null;

        while (curr != null)
        {
            if (curr.CompareTag("TransitionCue"))
            {
                foundParent = curr.gameObject;
                break;
            }
            curr = curr.parent;
        }
        
        ray.WhenStateChanged += state =>
        {
            if (state.NewState == InteractableState.Select)
            {
                // Log BEFORE raising: a listener may deactivate the cue synchronously, and
                // OnDisable would then close the encounter out from under us, splitting it
                // across two files.
                log?.End("transition_entered", "button", openIfNeeded: true); // emits the event and closes the file
                cueEvents.RaiseStartTransition();
                if (foundParent != null)
                    foundParent.SetActive(false);
            }
        };
    }
}