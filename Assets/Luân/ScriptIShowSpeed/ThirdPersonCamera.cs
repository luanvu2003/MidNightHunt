using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public float distance = 4f;
    [Header("Camera Controls")]
    public InputActionReference lookInput;
    public float mouseSensitivity = 0.2f;
    public float pitchMin = -20f;
    public float pitchMax = 60f;
    private float yaw = 0f;
    private float pitch = 0f;
    private bool isCursorLocked = true;
    private void Start()
    {
        SetCursorState(true);
    }
    private void OnEnable()
    {
        lookInput.action.Enable();
    }
    private void OnDisable()
    {
        lookInput.action.Disable();
    }
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            isCursorLocked = !isCursorLocked; 
            SetCursorState(isCursorLocked);
        }
    }
    private void LateUpdate()
    {
        if (target == null) return;
        if (isCursorLocked)
        {
            Vector2 lookDelta = lookInput.action.ReadValue<Vector2>();
            yaw += lookDelta.x * mouseSensitivity;
            pitch -= lookDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 position = target.position - (rotation * Vector3.forward * distance) + (Vector3.up * 1.5f);
        transform.position = position;
        transform.rotation = rotation;
    }
    private void SetCursorState(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;                  
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;   
            Cursor.visible = true;                   
        }
    }
}