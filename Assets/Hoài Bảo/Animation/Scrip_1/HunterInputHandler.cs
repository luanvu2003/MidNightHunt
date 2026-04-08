// using UnityEngine;
// using UnityEngine.InputSystem; 

// public class HunterInputHandler : MonoBehaviour
// {
//     private HunterMovement moveScript; 
//     private HunterInteraction interactScript; 
//     private AttackController attackScript; 
//     private HunterControllerInput actions; 

//     private void Awake()
//     {
//         moveScript = GetComponent<HunterMovement>();
//         interactScript = GetComponent<HunterInteraction>();
//         attackScript = GetComponent<AttackController>();
        
//         actions = new HunterControllerInput();
//     }

//     private void OnEnable() => actions.Enable(); 
//     private void OnDisable() => actions.Disable(); 

//     private void Update()
//     {
//         // 1. CHỐT CHẶN AN TOÀN
//         if (interactScript != null && interactScript.IsDoingAction())
//         {
//             moveScript.HandleMove(Vector2.zero); 
//         }
//         else
//         {
//             // 2. DI CHUYỂN
//             Vector2 moveInput = actions.HunterControllerS.Move.ReadValue<Vector2>();
//             moveScript.HandleMove(moveInput);
//         }

//         // 3. TƯƠNG TÁC (Đã gộp chung thành nút Interact)
//         if (actions.HunterControllerS.Interact.WasPressedThisFrame()) 
//         {
//             interactScript.TryInteract(); 
//         }
        
//         // 4. TẤN CÔNG & SKILL CỤ THỂ
//         if (attackScript != null)
//         {
//             // Đọc Action "AimTrap" (Đè Ctrl)
//             attackScript.isAimingTrap = actions.HunterControllerS.AimTrap.IsPressed();

//             // Đọc Action "Attack" (Chuột trái)
//             if (actions.HunterControllerS.Attack.WasPressedThisFrame()) 
//             {
//                 attackScript.PerformAttackLeft(); 
//             }
            
//             // Đọc Action "Phibua" (Chuột phải - Xài chung cho Đặt bẫy)
//             if (actions.HunterControllerS.Phibua.WasPressedThisFrame()) 
//             {
//                 attackScript.PerformAttackRight(); 
//             }
//         }
//     }
// }
using UnityEngine;
using UnityEngine.InputSystem; 
using Fusion; // 1. Thêm thư viện Fusion

public class HunterInputHandler : NetworkBehaviour // 2. Đổi thành NetworkBehaviour
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

    // 3. Thay OnEnable/OnDisable bằng Spawned/Despawned để đảm bảo Mạng đã sẵn sàng
    public override void Spawned()
    {
        // CHỈ BẬT NHẬN PHÍM NẾU ĐÂY LÀ NHÂN VẬT CỦA MÌNH
        if (Object.HasInputAuthority)
        {
            actions.Enable(); 
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        actions.Disable(); 
    }

    // =========================================================
    // UPDATE: XỬ LÝ NHỮNG NÚT "BẤM 1 LẦN" ĐỂ TRÁNH BỊ MISS NHỊP
    // =========================================================
    private void Update()
    {
        // 🚨 CHỐT CHẶN MẠNG: Chỉ người cầm chuột mới được gửi lệnh
        if (!Object.HasInputAuthority) return;

        // TƯƠNG TÁC
        if (actions.HunterControllerS.Interact.WasPressedThisFrame()) 
        {
            interactScript.TryInteract(); 
        }
        
        // TẤN CÔNG & SKILL CỤ THỂ
        if (attackScript != null)
        {
            // Báo lên cho Host biết mình có đang đè phím ngắm bẫy không (Dùng RPC thay vì gán trực tiếp)
            attackScript.Rpc_SetAimingTrap(actions.HunterControllerS.AimTrap.IsPressed());

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

    // =========================================================
    // FIXED UPDATE NETWORK: XỬ LÝ DI CHUYỂN VẬT LÝ VÀ ĐỒNG BỘ
    // =========================================================
    public override void FixedUpdateNetwork()
    {
        // 🚨 CHỐT CHẶN MẠNG
        if (!Object.HasInputAuthority) return;

        // DI CHUYỂN CÓ CHỐT CHẶN AN TOÀN
        if (interactScript != null && interactScript.IsDoingAction())
        {
            moveScript.HandleMove(Vector2.zero); 
        }
        else
        {
            // Đọc Action "Move" (WASD)
            Vector2 moveInput = actions.HunterControllerS.Move.ReadValue<Vector2>();
            moveScript.HandleMove(moveInput);
        }
    }
}