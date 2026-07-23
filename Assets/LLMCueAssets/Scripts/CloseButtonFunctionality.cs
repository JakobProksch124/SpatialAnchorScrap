using Oculus.Interaction;
using UnityEngine;

public class CloseButtonFunctionality : MonoBehaviour
{
    void Start()
    {
        var ray = GetComponent<RayInteractable>();
        var cueEvents = GetComponentInParent<CueEvents>();

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
                cueEvents.RaiseCloseCue();
                CueLogger.Event("closed");
                if (foundParent != null)
                    foundParent.SetActive(false);
            }
        };
    }
}
