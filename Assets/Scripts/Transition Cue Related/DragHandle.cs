using Oculus.Interaction;
using UnityEngine;

public class DragHandle : MonoBehaviour
{
    private RayInteractable _interactable;
    private Transform _root;
    private Transform _controller;

    private bool _dragging;

    [SerializeField]
    private float dragFollowSpeed = 20f;

    [SerializeField]
    private float rotationFollowSpeed = 20f;

    // State captured when dragging begins
    private Vector3 _initialRootPosition;
    private Quaternion _initialRootRotation;

    private Vector3 _initialControllerPosition;
    private Quaternion _initialControllerRotation;

    private TurnTowardsUser _turnTowardsUser;


    public void Initialize(
        RayInteractable interactable,
        Transform root,
        Transform controller)
    {
        _interactable = interactable;
        _root = root;
        _controller = controller;


        // Find the rotation component on the cue root
        if (_root != null)
        {
            _turnTowardsUser = _root.GetComponent<TurnTowardsUser>();
        }

        if (_interactable != null)
        {
            _interactable.WhenStateChanged += OnStateChanged;
        }
    }


    private void OnDestroy()
    {
        if (_interactable != null)
        {
            _interactable.WhenStateChanged -= OnStateChanged;
        }
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
        if (_controller == null || _root == null)
            return;

        _dragging = true;

        // Detach the cue from its original anchor.
        // 'true' preserves its current world position and rotation.
        _root.SetParent(null, true);
        //_turnTowardsUser.UpdateOriginalRotation();

        // ---------------------------------------------------------
        // Store the exact state at the moment the user grabs the cue
        // ---------------------------------------------------------

        _initialRootPosition = _root.position;
        _initialRootRotation = _root.rotation;

        _initialControllerPosition = _controller.position;
        _initialControllerRotation = _controller.rotation;
    }


    private void EndDrag()
    {
        _turnTowardsUser.UpdateOriginalRotation();
        _dragging = false;
    }


    private void Update()
    {
        if (!_dragging || _controller == null || _root == null)
            return;

        // ---------------------------------------------------------
        // Calculate how much the controller has moved/rotated
        // since the moment dragging started.
        // ---------------------------------------------------------

        Quaternion rotationDelta =
            _controller.rotation *
            Quaternion.Inverse(_initialControllerRotation);


        // ---------------------------------------------------------
        // POSITION
        //
        // Rotate the original controller -> cue offset together
        // with the controller.
        // ---------------------------------------------------------

        Vector3 initialOffset =
            _initialRootPosition -
            _initialControllerPosition;

        Vector3 rotatedOffset =
            rotationDelta * initialOffset;

        Vector3 targetPosition =
            _controller.position +
            rotatedOffset;


        // ---------------------------------------------------------
        // ROTATION
        //
        // Apply exactly the same rotation change that the controller
        // experienced to the cue's original rotation.
        // ---------------------------------------------------------

        Quaternion targetRotation =
            rotationDelta *
            _initialRootRotation;


        // ---------------------------------------------------------
        // Smooth movement
        // ---------------------------------------------------------

        float positionLerp =
            1f - Mathf.Exp(-dragFollowSpeed * Time.deltaTime);

        float rotationLerp =
            1f - Mathf.Exp(-rotationFollowSpeed * Time.deltaTime);

        _root.position = Vector3.Lerp(
            _root.position,
            targetPosition,
            positionLerp
        );

        _root.rotation = Quaternion.Slerp(
            _root.rotation,
            targetRotation,
            rotationLerp
        );
    }
}