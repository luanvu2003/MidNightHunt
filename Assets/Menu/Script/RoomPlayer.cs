using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    // Fusion 2: Sử dụng OnChangedRender thay vì OnChanged
    [Networked, OnChangedRender(nameof(OnNameChanged))] 
    public NetworkString<_32> PlayerName { get; set; }

    public override void Spawned()
    {
        // Fusion 2: Rút gọn thành HasInputAuthority
        if (HasInputAuthority)
        {
            RPC_SetPlayerName(PlayerInfo.Instance.PlayerName);
        }
        else
        {
            if (PlayerName.Length > 0 && RoomUI.Instance != null)
            {
                RoomUI.Instance.AddPlayer(PlayerName.ToString());
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerName(NetworkString<_32> name)
    {
        PlayerName = name; 
    }

    // Fusion 2: Hàm callback không cần static và không cần tham số Changed<T> nữa
    public void OnNameChanged()
    {
        string newName = PlayerName.ToString();
        if (RoomUI.Instance != null && !string.IsNullOrEmpty(newName))
        {
            RoomUI.Instance.AddPlayer(newName);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (RoomUI.Instance != null && PlayerName.Length > 0)
        {
            RoomUI.Instance.RemovePlayer(PlayerName.ToString());
        }
    }
}