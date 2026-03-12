using Oculus.Interaction.Locomotion;
using UnityEngine;

public class ARTeleportRedirect : MonoBehaviour
{
    [SerializeField] private Building_TransitionCues building_TransitionCues;

    private TeleportInteractor teleportInteractor;

    /*private void Awake()
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
        if (locomotionEvent.Translation == LocomotionEvent.TranslationType.None)
            return;

        Vector3 hitPoint = locomotionEvent.Pose.position;

        building_TransitionCues.MoveVRRoomToHit(hitPoint);

        // ADD THIS
        building_TransitionCues.OnTeleportFinished();
    }

    public void SetBuildingTransitionCues(Building_TransitionCues building_TransitionCues)
    {
        this.building_TransitionCues = building_TransitionCues;
    }*/
}