using UnityEngine;
using System;

public class TransitionCueCollisionRelay : MonoBehaviour
{
    private Action<Collision> onCollide;

    public void Initialize(Action<Collision> callback)
    {
        onCollide = callback;
    }

    private void OnTriggerEnter(Collider other)
    {
        onCollide?.Invoke(null);
    }
}