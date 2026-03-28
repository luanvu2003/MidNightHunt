using UnityEngine;
using UnityEngine.InputSystem; 

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

        // 3. TƯƠNG TÁC (Đã gộp chung thành nút Interact)
        if (actions.HunterControllerS.Interact.WasPressedThisFrame()) 
        {
            interactScript.TryInteract(); 
        }
        
        // 4. TẤN CÔNG & SKILL CỤ THỂ
        if (attackScript != null)
        {
            // Đọc Action "AimTrap" (Đè Ctrl)
            attackScript.isAimingTrap = actions.HunterControllerS.AimTrap.IsPressed();

            // Đọc Action "Attack" (Chuột trái)
            if (actions.HunterControllerS.Attack.WasPressedThisFrame()) 
            {
                attackScript.PerformAttackLeft(); 
            }
            
            // Đọc Action "Phibua" (Chuột phải - Xài chung cho Đặt bẫy)
            if (actions.HunterControllerS.Phibua.WasPressedThisFrame()) 
            {
                attackScript.PerformAttackRight(); 
            }
        }
    }
}