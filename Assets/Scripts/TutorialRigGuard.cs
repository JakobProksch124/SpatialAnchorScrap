using UnityEngine;

/// <summary>
/// Tutorial-only guard. The interaction rig's Locomotor applies gravity and slowly sinks the
/// camera rig (nothing catches it). In the study this is invisible (all content is anchored to
/// the real world), but the tutorial's world-fixed cue then appears to "fly up into the sky".
///
/// This component 1) disables every self-movement source at runtime and 2) HARD-LOCKS the rig
/// root at its start position every frame — in this app the rig must never move (the user walks
/// physically; the white-room teleport moves the ROOM, not the rig). Corrections are logged so
/// the fix is verifiable via adb logcat.
/// </summary>
public class TutorialRigGuard : MonoBehaviour
{
    private Transform _rig;
    private Vector3 _home;
    private float _logBudget = 25f;

    private void Start()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "Locomotor" || t.name.Contains("LocomotionSlideActions"))
            {
                t.gameObject.SetActive(false);
                Debug.Log($"[TutorialRigGuard] disabled {t.name}");
            }

        var rig = FindAnyObjectByType<OVRCameraRig>();
        if (rig != null)
        {
            _rig = rig.transform;
            _home = _rig.position;
            Debug.Log($"[TutorialRigGuard] rig locked at {_home}");
        }
        else Debug.LogWarning("[TutorialRigGuard] no OVRCameraRig found");
    }

    private void LateUpdate()
    {
        if (_rig == null) return;
        var drift = _rig.position - _home;
        if (drift.sqrMagnitude > 0.0001f)
        {
            if (_logBudget > 0f)
            {
                _logBudget -= 1f;
                Debug.Log($"[TutorialRigGuard] corrected rig drift {drift} (something tried to move the rig)");
            }
            _rig.position = _home;
        }
    }
}
