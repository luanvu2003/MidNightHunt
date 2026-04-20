using UnityEngine;
using UnityEngine.InputSystem;
using Fusion; 
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
            Runner.AddCallbacks(this); 
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
    private void Update()
    {
        if (!Object.HasInputAuthority) return;
        if (SettingsUIManager.IsOpen) return;
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
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var myInput = new HunterMoveInput();
        if (SettingsUIManager.IsOpen)
        {
            myInput.moveDirection = Vector2.zero;
        }
        else
        {
            myInput.moveDirection = actions.HunterControllerS.Move.ReadValue<Vector2>();
        }
        if (Camera.main != null)
        {
            myInput.cameraYaw = Camera.main.transform.eulerAngles.y;
        }

        input.Set(myInput); 
    }
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out HunterMoveInput input))
        {
            if (interactScript != null && interactScript.IsDoingAction())
            {
                moveScript.HandleMove(Vector2.zero, input.cameraYaw);
            }
            else
            {
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
public struct HunterMoveInput : INetworkInput
{
    public Vector2 moveDirection;
    public float cameraYaw;
}