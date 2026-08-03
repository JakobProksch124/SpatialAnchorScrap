using Oculus.Interaction;
using UnityEngine;

public class CloseButtonFunctionality : MonoBehaviour
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
                // closing IS the end of the encounter — without End() the file stayed open
                // until the object was destroyed and every arrival cue ended as app_quit.
                // Logged before the event is raised, for the same reason as the enter button.
                log?.End("closed", "button", openIfNeeded: true);
                cueEvents.RaiseCloseCue();
                if (foundParent != null)
                    foundParent.SetActive(false);
            }
        };
    }
}
