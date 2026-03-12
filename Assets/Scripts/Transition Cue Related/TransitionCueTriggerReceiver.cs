using UnityEngine;
using System;

public class TransitionCueTriggerReceiver : MonoBehaviour
{
    private Action<Collider> onTrigger;
    private bool hasTriggered = false;

    public void Initialize(Action<Collider> callback)
    {
        onTrigger = callback;
    }

    public void ReceiveTrigger(Collider other)
    {
        if (hasTriggered)
            return;

        hasTriggered = true;
        onTrigger?.Invoke(other);
    }
}