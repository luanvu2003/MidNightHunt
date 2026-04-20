using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCamera : MonoBehaviour
{
    [Header("Mục tiêu đang điều khiển")]
    public Transform playerBody;  
    public Transform headBone;    
    [Header("Cài Đặt Camera")]
    public Vector3 eyeOffset = new Vector3(0f, 0.1f, 0.2f); 
    [SerializeField] private float mouseSensitivity = 15f; 
    public float maxLookAngle = 80f; 
    private float cameraPitch = 0f;
    private float cameraYaw = 0f; 
    private bool isMouseLocked = true; 
    public bool isCameraLockedForAnim = false; 
    private void Start()
    {
        SetMouseState(true);
    }
    public void SetupCameraForHunter(Transform newBody, Transform newHead, Vector3 specificOffset)
    {
        playerBody = newBody;
        headBone = newHead;
        eyeOffset = specificOffset; 
        
        cameraYaw = playerBody.eulerAngles.y;
        cameraPitch = 0f;
    }
    public void SyncCameraAngles(float newYaw)
    {
        cameraYaw = newYaw;
    }
    private void Update()
    {
        if (playerBody == null || headBone == null) return;
        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            isMouseLocked = !isMouseLocked; 
            SetMouseState(isMouseLocked);
        }
        if (!isMouseLocked || isCameraLockedForAnim) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        cameraYaw += mouseDelta.x * mouseSensitivity * Time.deltaTime;
        cameraPitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        transform.localEulerAngles = new Vector3(cameraPitch, cameraYaw, 0f);
    }
    private void LateUpdate()
    {
        if (headBone == null) return;
        transform.position = headBone.TransformPoint(eyeOffset);
    }
    private void SetMouseState(bool lockMouse)
    {
        Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockMouse;
    }
}