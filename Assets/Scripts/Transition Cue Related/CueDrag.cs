// using Oculus.Interaction;
// using UnityEngine;

// public class CueDrag : MonoBehaviour
// {
//     private RayInteractable _interactable;

//     private bool _dragging;

//     private RayInteractor _rayInteractor;

//     public void Initialize(RayInteractable interactable)
//     {
//         _interactable = interactable;

//         _interactable.WhenStateChanged += OnStateChanged;
//     }

//     void OnDestroy()
//     {
//         if (_interactable != null)
//             _interactable.WhenStateChanged -= OnStateChanged;
//     }

//     private void OnStateChanged(InteractableStateChangeArgs args)
//     {
//         if (args.NewState == InteractableState.Select)
//         {
//             _dragging = true;

//             _rayInteractor = args.Interactor as RayInteractor;
//         }

//         if (args.NewState == InteractableState.Normal)
//         {
//             _dragging = false;
//             _rayInteractor = null;
//         }
//     }

//     void LateUpdate()
//     {
//         if (!_dragging)
//             return;

//         if (_rayInteractor == null)
//             return;

//         if (_rayInteractor.ComputeCandidate(out RaycastHit hit))
//         {
//             transform.position = hit.point;

//             transform.rotation =
//                 Quaternion.LookRotation(
//                     transform.position -
//                     Camera.main.transform.position);
//         }
//     }
// }