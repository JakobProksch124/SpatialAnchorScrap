using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Positioner : MonoBehaviour
{
    // Offset Text Element
    [SerializeField] TMP_Text alignmentText;

    [SerializeField] float positionSpeed = 0.1f;

    // Used for changing y-level
    [SerializeField] InputActionReference ascendButton;
    [SerializeField] InputActionReference descendButton;

    // Used to change rotation mode (between x, y, and z rotation)
    [SerializeField] InputActionReference triggerLeft;
    private bool _triggerLeftWasPressed = false;
    private enum RotAxis { X, Y, Z }
    private RotAxis _currentAxis = RotAxis.Y;
    [SerializeField] public Transform cameraTransform;

    [SerializeField] InputActionReference _moveAction;
    [SerializeField] InputActionReference _turnAction;

    [SerializeField] public GameObject _objectToPosition;

    void OnEnable()
    {
        _moveAction.action.Enable();
        _turnAction.action.Enable();
        ascendButton.action.Enable();
        descendButton.action.Enable();
        triggerLeft.action.Enable();
    }

    void OnDisable()
    {
        _moveAction.action.Disable();
        _turnAction.action.Disable();
        ascendButton.action.Disable();
        descendButton.action.Disable();
        triggerLeft.action.Disable();
    }

    void Update()
    {
        if (_objectToPosition == null)
            return;

        if (_moveAction == null || _turnAction == null)
            return;
        ChooseAxisMode();

            // Movement in x and z direction (horizontal / left and right)
            var moveValue = _moveAction.action.ReadValue<Vector2>();
        Debug.Log("Move: " + moveValue);
        _objectToPosition.transform.localPosition += new Vector3(moveValue.x * positionSpeed, 0, moveValue.y * positionSpeed);

            // Rotation
            var turnValue = _turnAction.action.ReadValue<Vector2>();
        Debug.Log("Turn: " + turnValue);
        float rotAmount = turnValue.x * positionSpeed * 10f;
            Quaternion deltaRot = Quaternion.identity;

            switch (_currentAxis)
            {
                case RotAxis.X:
                    deltaRot = Quaternion.Euler(rotAmount, 0f, 0f);
                    break;

                case RotAxis.Y:
                    deltaRot = Quaternion.Euler(0f, rotAmount, 0f);
                    break;

                case RotAxis.Z:
                    deltaRot = Quaternion.Euler(0f, 0f, rotAmount);
                    break;
            }

            _objectToPosition.transform.localRotation *= deltaRot;


            // Movement in y direction (vertical)
            if (ascendButton.action.IsPressed())
                _objectToPosition.transform.localPosition += new Vector3(0, positionSpeed, 0);

            if (descendButton.action.IsPressed())
                _objectToPosition.transform.localPosition += new Vector3(0, -positionSpeed, 0);

            // Update Offset Text
            Vector3 p = _objectToPosition.transform.localPosition;
            Vector3 r = _objectToPosition.transform.localEulerAngles;
            alignmentText.text =
                $"X: {Mathf.Round(p.x * 100) / 100}; " +
                $"Y: {Mathf.Round(p.y * 100) / 100}; " +
                $"Z: {Mathf.Round(p.z * 100) / 100}\n" +
                $"RotX: {Mathf.Round(r.x * 100) / 100}; " +
                $"RotY: {Mathf.Round(r.y * 100) / 100}; " +
                $"RotZ: {Mathf.Round(r.z * 100) / 100}\n" +
                $"Aktive Rot-Achse: {_currentAxis}";
        
    }

    void ChooseAxisMode()
    {
        // This logic makes it a IsReleased instead of a IsPressed
        bool isPressed = triggerLeft.action.IsPressed(); 
        if (_triggerLeftWasPressed && !isPressed)
        {
            _currentAxis = (RotAxis)(((int)_currentAxis + 1) % 3);
        }
        _triggerLeftWasPressed = isPressed;
    }

    public void SetObjectToPosition(GameObject obj)
    {
        _objectToPosition = obj;
        Debug.Log("[Positioner] _objectToPostion set");
    }
}
