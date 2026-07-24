using Oculus.Interaction;
using UnityEngine;

public class DragHandle : MonoBehaviour
{
    private RayInteractable _interactable;
    private Transform _root;
    private Transform _controller;

    private bool _dragging;
    private Vector3 _grabOffset;

    [SerializeField]
    private float dragFollowSpeed = 20f;

    public void Initialize(
        RayInteractable interactable,
        Transform root,
        Transform controller)
    {
        _interactable = interactable;
        _root = root;
        _controller = controller;

        _interactable.WhenStateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.WhenStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(InteractableStateChangeArgs args)
    {
        if (args.NewState == InteractableState.Select)
        {
            BeginDrag();
        }
        else if (args.NewState == InteractableState.Normal)
        {
            EndDrag();
        }
    }

    private void BeginDrag()
    {
        if (_controller == null)
            return;

        _dragging = true;

        _root.SetParent(null, true);

        _grabOffset = _root.position - _controller.position;
    }

    private void EndDrag()
    {
        _dragging = false;
    }

    private void Update()
    {
        if (!_dragging)
            return;

        Vector3 target = _controller.position + _grabOffset;

        _root.position = Vector3.Lerp(
            _root.position,
            target,
            Time.deltaTime * dragFollowSpeed);
    }
}