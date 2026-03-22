using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCamera : MonoBehaviour
{
    [Header("Mục tiêu đang điều khiển")]
    public Transform playerBody;  
    public Transform headBone;    

    [Header("Cai Dat Camera")]
    public Vector3 eyeOffset = new Vector3(0f, 0.1f, 0.2f); 
    [SerializeField] private float mouseSensitivity = 15f; 
    public float maxLookAngle = 80f; 
    public float bodyRotationSpeed = 8f; 

    private float cameraPitch = 0f;
    private float cameraYaw = 0f; 
    private bool isMouseLocked = true; 

    // ĐÃ THÊM: Biến khóa Camera khi đang làm Animation
    public bool isCameraLockedForAnim = false; 

    private void Start()
    {
        cameraYaw = transform.eulerAngles.y;
        cameraPitch = transform.eulerAngles.x;
        SetMouseState(true);
    }

    public void SetTarget(Transform newBody, Transform newHead)
    {
        playerBody = newBody;
        headBone = newHead;
        cameraYaw = playerBody.eulerAngles.y;
        cameraPitch = 0f;
    }

    // ĐÃ THÊM: Hàm này để đồng bộ lại góc chuột sau khi nhân vật bị ép xoay mặt vào cái máy
    public void SyncCameraAngles(float newYaw)
    {
        cameraYaw = newYaw;
    }

    private void Update()
    {
        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            isMouseLocked = !isMouseLocked; 
            SetMouseState(isMouseLocked);
        }

        // ĐÃ THÊM: Nếu Camera bị khóa vì đang múa Animation -> Cấm xoay chuột!
        if (!isMouseLocked || playerBody == null || headBone == null || isCameraLockedForAnim) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        cameraYaw += mouseDelta.x * mouseSensitivity * Time.deltaTime;
        cameraPitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        transform.localEulerAngles = new Vector3(cameraPitch, cameraYaw, 0f);

        float targetBodyYaw = cameraYaw; 
        float currentBodyYaw = playerBody.eulerAngles.y; 
        float newBodyYaw = Mathf.LerpAngle(currentBodyYaw, targetBodyYaw, Time.deltaTime * bodyRotationSpeed);
        playerBody.eulerAngles = new Vector3(0f, newBodyYaw, 0f);
    }

    private void LateUpdate()
    {
        if (headBone == null) return;
        transform.position = headBone.TransformPoint(eyeOffset);
    }

    private void SetMouseState(bool lockMouse)
    {
        if (lockMouse)
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

    public void SetMouseSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }
}