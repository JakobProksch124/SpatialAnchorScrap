using UnityEngine;

public class TransitionCueTriggerForwarder : MonoBehaviour
{
    private TransitionCueTriggerReceiver receiver;

    public void Initialize(TransitionCueTriggerReceiver target)
    {
        receiver = target;
    }

    private void OnTriggerEnter(Collider other)
    {
        receiver?.ReceiveTrigger(other);
    }
}