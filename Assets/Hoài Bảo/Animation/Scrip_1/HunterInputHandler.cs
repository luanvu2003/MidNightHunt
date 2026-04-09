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
using Fusion; // Thư viện Fusion
using System.Collections.Generic;
using System;
using Fusion.Sockets;

public class HunterInputHandler : NetworkBehaviour, INetworkRunnerCallbacks
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

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            actions.Enable();
            Runner.AddCallbacks(this); // 🚨 Đăng ký gửi mạng
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasInputAuthority)
        {
            actions.Disable();
            Runner.RemoveCallbacks(this);
        }
    }

    // =========================================================
    // UPDATE: GỬI CÁC LỆNH RỜI RẠC QUA RPC (Tấn công, Tương tác)
    // =========================================================
    private void Update()
    {
        if (!Object.HasInputAuthority) return;

        if (actions.HunterControllerS.Interact.WasPressedThisFrame())
        {
            interactScript.TryInteract();
        }

        if (attackScript != null)
        {
            attackScript.Rpc_SetAimingTrap(actions.HunterControllerS.AimTrap.IsPressed());

            if (actions.HunterControllerS.Attack.WasPressedThisFrame())
            {
                attackScript.PerformAttackLeft();
            }
            if (actions.HunterControllerS.Phibua.WasPressedThisFrame())
            {
                attackScript.PerformAttackRight();
            }
        }
    }

    // =========================================================
    // ON INPUT: ĐÓNG GÓI WASD VÀ CAMERA GỬI CHO SERVER
    // =========================================================
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var myInput = new HunterMoveInput();

        // Đọc WASD
        myInput.moveDirection = actions.HunterControllerS.Move.ReadValue<Vector2>();

        // Đọc góc quay Camera
        if (Camera.main != null)
        {
            myInput.cameraYaw = Camera.main.transform.eulerAngles.y;
        }

        input.Set(myInput); // Nạp lên Server
    }

    // =========================================================
    // FIXED UPDATE NETWORK: NHẬN DỮ LIỆU TỪ MẠNG ĐỂ DI CHUYỂN
    // =========================================================
    public override void FixedUpdateNetwork()
    {
        // 🚨 KHÔNG CÒN DÒNG if (!HasInputAuthority) return; NỮA!

        if (GetInput(out HunterMoveInput input))
        {
            if (interactScript != null && interactScript.IsDoingAction())
            {
                // Truyền số 0 và góc Camera
                moveScript.HandleMove(Vector2.zero, input.cameraYaw);
            }
            else
            {
                // Truyền phím bấm và góc Camera
                moveScript.HandleMove(input.moveDirection, input.cameraYaw);
            }
        }
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
}

// 🚨 TẠO STRUCT DỮ LIỆU Ở CUỐI FILE
public struct HunterMoveInput : INetworkInput
{
    public Vector2 moveDirection;
    public float cameraYaw;
}