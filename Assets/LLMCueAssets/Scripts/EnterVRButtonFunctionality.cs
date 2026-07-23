using Oculus.Interaction;
using UnityEngine;

public class EnterVRButtonFunctionality : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var ray = GetComponent<RayInteractable>();
        var cueEvents = GetComponentInParent<CueEvents>();

        ray.WhenStateChanged += state =>
        {
            if (state.NewState == InteractableState.Select)
            {
                cueEvents.onStartTransition.Invoke();
                CueLogger.Event("transition_entered");
                CueLogger.End("transition_entered"); // entering finishes the encounter file
            }
        };
    }
}