using UnityEngine;

// Rotates a GameObject to face the user within a maximum angle constraint (Rotation on horizontal plane only)
// -> Creates a subtle "looking at you" effect
public class TurnTowardsUser : MonoBehaviour
{
    // === Configuration ===
    private float maxAngle = 10f;
    private float rotationSpeed = 2f;
    private float triggerDistance = 0f; // 0 = always active

    // === Components ===
    private Transform playerTransform;

    // === State ===
    private Quaternion originalRotation;
    private bool isInitialized = false;

    // Initializes the rotation effect with configuration
    //
    // maxRotationAngle: Maximum rotation angle toward user (degrees)
    // speed: Speed of rotation interpolation
    // activationDistance: Distance at which rotation gets triggered (0 = always active)
    public void Initialize(float maxRotationAngle, float speed, float activationDistance)
    {
        maxAngle = maxRotationAngle;
        rotationSpeed = speed;
        triggerDistance = activationDistance;

        // Store original rotation relative to parent
        originalRotation = transform.localRotation;
        isInitialized = true;
    }

    void Start()
    {
        if (!isInitialized)
        {
            // Default initialization if not called manually
            originalRotation = transform.localRotation;
            isInitialized = true;
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerTransform = mainCam.transform;
        }
        else
        {
            Debug.Log("[TransitionCueExpander] NO MAIN CAMERA FOUND!!!");
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null)
            return;

        // Check if within trigger distance (if set)
        if (triggerDistance > 0f)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > triggerDistance)
            {
                // Outside trigger distance = return to original rotation
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    originalRotation,
                    Time.deltaTime * rotationSpeed
                );
                return;
            }
        }

        // Calculate ideal rotation to face player
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0; // Keep rotation on horizontal plane only

        if (toPlayer.sqrMagnitude < 0.001f)
            return; // Too close, skip rotation

        // LookRotation makes the object's forward (Z+) axis point in the given direction
        // The panels have text on the Z+ face (the blue arrow in the editor)
        // This makes the panel face the player with its front (Z+) side
        Quaternion targetRotation = Quaternion.LookRotation(toPlayer, Vector3.up);

        // Convert to local space
        Quaternion targetLocalRotation = Quaternion.Inverse(transform.parent != null ? transform.parent.rotation : Quaternion.identity) * targetRotation;

        // Calculate angle difference from original rotation
        float angleDiff = Quaternion.Angle(originalRotation, targetLocalRotation);

        // Clamp rotation to maxAngle
        Quaternion clampedRotation;
        if (angleDiff > maxAngle)
        {
            // Lerp between original and target, clamped to maxAngle
            float t = maxAngle / angleDiff;
            clampedRotation = Quaternion.Slerp(originalRotation, targetLocalRotation, t);
        }
        else
        {
            clampedRotation = targetLocalRotation;
        }

        // Smoothly interpolate to target rotation
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            clampedRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    // Updates the original rotation reference (useful when parent rotation changes)
    public void UpdateOriginalRotation()
    {
        originalRotation = transform.localRotation;
    }

    // Sets the maximum rotation angle dynamically
    public void SetMaxAngle(float angle)
    {
        maxAngle = angle;
    }

    // Sets the rotation speed dynamically
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    // Enables or disables the rotation effect
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;

        // Return to original rotation when disabled
        if (!enabled)
        {
            transform.localRotation = originalRotation;
        }
    }
}
