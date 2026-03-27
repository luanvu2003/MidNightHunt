using UnityEngine; // Khai báo thư viện Unity
using UnityEngine.InputSystem; // Sử dụng hệ thống Input System mới

public class HunterInputHandler : MonoBehaviour
{
    private HunterMovement moveScript; 
    private HunterInteraction interactScript; 
    private AttackController attackScript; 
    private HunterControllerInput actions; 

    private void Awake()
    {
        moveScript = GetComponent<HunterMovement>();
        interactScript = GetComponent<HunterInteraction>();
        attackScript = GetComponent<AttackController>();
        
        actions = new HunterControllerInput();
    }

    private void OnEnable() => actions.Enable(); 
    private void OnDisable() => actions.Disable(); 

    private void Update()
    {
        // 1. CHỐT CHẶN AN TOÀN
        if (interactScript != null && interactScript.IsDoingAction())
        {
            moveScript.HandleMove(Vector2.zero); 
        }
        else
        {
            // 2. DI CHUYỂN
            Vector2 moveInput = actions.HunterControllerS.Move.ReadValue<Vector2>();
            moveScript.HandleMove(moveInput);
        }

        // 3. TƯƠNG TÁC
        if (Keyboard.current.spaceKey.wasPressedThisFrame) 
        {
            interactScript.TryInteract(); 
        }
        
        // 4. TẤN CÔNG & NHẮM BẪY
        if (attackScript != null)
        {
            // 🚨 NÂNG CẤP: Truyền trạng thái ĐÈ phím Ctrl (Giữ thì true, Nhả ra thì false)
            attackScript.isAimingTrap = Keyboard.current.leftCtrlKey.isPressed;

            if (Mouse.current.leftButton.wasPressedThisFrame) attackScript.PerformAttackLeft(); 
            if (Mouse.current.rightButton.wasPressedThisFrame) attackScript.PerformAttackRight(); 
        }
    }
}