using UnityEngine;
using UnityEngine.InputSystem;

public class GameCameraController : MonoBehaviour
{
    public enum CameraMode { None, FPS_Hunter, TPS_Survivor }
    public CameraMode currentMode = CameraMode.None;
    [Header("== MỤC TIÊU ==")]
    public Transform targetBody;
    public Transform targetHead;
    [Header("== SETTINGS FPS (HUNTER) ==")]
    public Vector3 fpsEyeOffset = new Vector3(0, 0.1f, 0.2f);
    public float maxLookAngle = 80f;
    [Header("== SETTINGS TPS (SURVIVOR) ==")]
    public Vector3 tpsOffset = new Vector3(0.5f, 1.5f, -3f); 
    public float tpsSmoothSpeed = 10f;
    [Header("== CHUNG ==")]
    public float mouseSensitivity = 15f;
    private float cameraPitch = 0f;
    private float cameraYaw = 0f;
    private bool isMouseLocked = true;
    private void Start() => SetMouseState(true);
    public void SetupFPS(Transform body, Transform head, Vector3 offset)
    {
        targetBody = body;
        targetHead = head;
        fpsEyeOffset = offset;
        cameraYaw = body.eulerAngles.y;
        currentMode = CameraMode.FPS_Hunter;
    }
    public void SetupTPS(Transform body, Vector3 offset)
    {
        targetBody = body;
        tpsOffset = offset;
        cameraYaw = body.eulerAngles.y;
        currentMode = CameraMode.TPS_Survivor;
    }
    private void Update()
    {
        if (targetBody == null || currentMode == CameraMode.None) return;
        if (SettingsUIManager.IsOpen) return;
        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            isMouseLocked = !isMouseLocked;
            SetMouseState(isMouseLocked);
        }
        if (!isMouseLocked) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        cameraYaw += mouseDelta.x * mouseSensitivity * Time.deltaTime;
        cameraPitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        transform.localEulerAngles = new Vector3(cameraPitch, cameraYaw, 0f);
    }
    private void LateUpdate()
    {
        if (targetBody == null || currentMode == CameraMode.None) return;
        if (currentMode == CameraMode.FPS_Hunter && targetHead != null)
        {
            transform.position = targetHead.TransformPoint(fpsEyeOffset);
        }
        else if (currentMode == CameraMode.TPS_Survivor)
        {
            Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
            Vector3 desiredPosition = targetBody.position + rotation * tpsOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * tpsSmoothSpeed);
        }
    }
    private void SetMouseState(bool lockMouse)
    {
        Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockMouse;
    }
}