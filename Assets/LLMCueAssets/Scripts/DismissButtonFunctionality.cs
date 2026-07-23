using Oculus.Interaction;
using UnityEngine;

public class DismissButtonFunctionality : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var ray = GetComponent<RayInteractable>();

        ray.WhenStateChanged += state =>
        {
            if (state.NewState == InteractableState.Select)
                CueLogger.Event("dismiss_requested_ignored");
        };
    }
}
