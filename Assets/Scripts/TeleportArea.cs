using UnityEngine;

/// <summary>
/// Marks a walkable teleport region inside a VR room. The box defined by this transform
/// (position, rotation, scale = box size in metres) is where the room-move teleport may land.
///
/// HOW TO TUNE (in the editor): open the VR room scene (Laboratory / Bridge), select the
/// "TeleportArea" object — it draws as a green box in the Scene view. Move and scale it until
/// it covers exactly the floor the user may reach. Add more TeleportArea objects for L-shaped
/// rooms; ANY box containing the aim point makes it valid.
///
/// If a room has no TeleportArea, teleport falls back to a rectangle around UserSpawnPoint.
/// Gizmo only — nothing is rendered in the build.
/// </summary>
public class TeleportArea : MonoBehaviour
{
    /// <summary>True if the world point lies inside this box (Y ignored — single-floor rooms).</summary>
    public bool Contains(Vector3 worldPoint)
    {
        var local = transform.InverseTransformPoint(worldPoint);
        return Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.z) <= 0.5f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.18f);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
