using UnityEngine;

public class TransitionCueTriggerForwarder : MonoBehaviour
{
    private TransitionCueTriggerReceiver receiver;

    public void Initialize(TransitionCueTriggerReceiver r)
    {
        receiver = r;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.IsChildOf(receiver.transform))
            return;

        if (!other.CompareTag("Player"))
            return;

        receiver.ReceiveTrigger(other);
    }
}