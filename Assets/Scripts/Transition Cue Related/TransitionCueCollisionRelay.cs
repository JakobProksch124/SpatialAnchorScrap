using UnityEngine;
using System;

public class TransitionCueCollisionRelay : MonoBehaviour
{
    private Action<Collider> onTrigger;
    private bool hasTriggered = false;

    public void Initialize(Action<Collider> callback)
    {
        onTrigger = callback;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        hasTriggered = true;
        onTrigger?.Invoke(other);
    }
}