using Oculus.Interaction.Locomotion;
using UnityEngine;

/// <summary>
/// Redirects a VR teleport so it moves the VR ROOM to the aimed point instead of moving
/// the player rig. In this passthrough setup the AR world-mapping (occlusion walls) is
/// pinned to reality via a spatial anchor, and the rig must never move — otherwise
/// returning to AR would misalign the mapping. So teleport slides the room to the user;
/// the rig and the anchor are never touched (Y stays handled by the building's continuous
/// floor align). Auto-attached to each teleport interactor by Building_TransitionCues when
/// a VR room loads.
/// </summary>
[RequireComponent(typeof(TeleportInteractor))]
public class ARTeleportRedirect : MonoBehaviour
{
    [SerializeField] private Building_TransitionCues building_TransitionCues;

    private TeleportInteractor teleportInteractor;

    private void Awake()
    {
        teleportInteractor = GetComponent<TeleportInteractor>();
    }

    private void OnEnable()
    {
        if (teleportInteractor != null)
            teleportInteractor.WhenLocomotionPerformed += OnLocomotion;
    }

    private void OnDisable()
    {
        if (teleportInteractor != null)
            teleportInteractor.WhenLocomotionPerformed -= OnLocomotion;
    }

    private void OnLocomotion(LocomotionEvent locomotionEvent)
    {
        if (locomotionEvent.Translation == LocomotionEvent.TranslationType.None) return;
        if (building_TransitionCues == null) return;

        // Move the VR room so the aimed floor point comes under the user (XZ only).
        building_TransitionCues.MoveVRRoomToHit(locomotionEvent.Pose.position);
    }

    public void SetBuildingTransitionCues(Building_TransitionCues b) => building_TransitionCues = b;
}
