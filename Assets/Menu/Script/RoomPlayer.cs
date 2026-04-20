using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public NetworkBool IsHunter { get; set; } 

    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public int CharacterID { get; set; } 

    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public NetworkBool IsReady { get; set; } 
    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public int RoomIndex { get; set; } = -1; 
    public static RoomPlayer Local;
    public override void Spawned()
    {
        DontDestroyOnLoad(gameObject);
        if (HasInputAuthority)
        {
            Local = this;
            RPC_InitPlayer(PlayerInfo.Instance.PlayerName);
        }
        else
        {
            UpdateLobbyUI();
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_InitPlayer(NetworkString<_32> name)
    {
        PlayerName = name;
        CharacterID = -1; 
        IsReady = false;
        IsHunter = false; 
    }
    public void SetRoleByServer(bool isHunterRole)
    {
        if (Runner.IsServer)
        {
            IsHunter = isHunterRole;
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacter(int wantedCharID)
    {
        if (IsHunter)
        {
            CharacterID = wantedCharID;
        }
        else 
        {
            bool isTaken = false;
            var allPlayers = FindObjectsOfType<RoomPlayer>();
            foreach (var p in allPlayers)
            {
                if (p != this && !p.IsHunter && p.CharacterID == wantedCharID)
                {
                    isTaken = true;
                    break;
                }
            }
            if (!isTaken)
            {
                CharacterID = wantedCharID;
            }
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ToggleReady()
    {
        IsReady = !IsReady; 
    }
    public void OnDataChanged()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 1) 
        {
            UpdateLobbyUI();
        }
        else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 3) 
        {
        }
    }
    private void UpdateLobbyUI()
    {
        if (RoomUI.Instance != null && PlayerName.Length > 0)
        {
            RoomUI.Instance.AddPlayer(PlayerName.ToString());
        }
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (RoomUI.Instance != null && PlayerName.Length > 0)
        {
            RoomUI.Instance.RemovePlayer(PlayerName.ToString());
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowLoadingAndTransition()
    {
        // 1. Máy nào cũng bật Loading lên
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.ShowLoading();
        }
        if (Runner.IsServer)
        {
            StartCoroutine(DelayLoadRoutine());
        }
    }
    private System.Collections.IEnumerator DelayLoadRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        Runner.LoadScene(SceneRef.FromIndex(2));
    }
}