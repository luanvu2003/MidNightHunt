using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class RoomSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("Room Prefab (Phòng chờ)")]
    public NetworkPrefabRef roomPlayerPrefab;

    // 🚨 SỬA LỖI MẤT TÊN: Xóa chữ "static" đi để nó không bị "nhớ dai" rác của ván trước
    private Dictionary<PlayerRef, NetworkObject> SpawnedRoomPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private static RoomSpawner _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer && roomPlayerPrefab.IsValid)
        {
            var roomObj = Runner.Spawn(roomPlayerPrefab, Vector3.zero, Quaternion.identity, player);

            // 🚨 BÙA CHỐNG RÁC: Thay vì dùng .Add, ta dùng dấu [ ] = để GHI ĐÈ. 
            // Nếu có rác của ván trước thì xóa luôn đè cái mới vào!
            SpawnedRoomPlayers[player] = roomObj;

            Debug.Log($"🏠 Đã spawn RoomPlayer cho Player {player.PlayerId} tại phòng chờ.");
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Runner.IsServer && SpawnedRoomPlayers.TryGetValue(player, out NetworkObject roomObj))
        {
            if (roomObj != null) Runner.Despawn(roomObj);
            SpawnedRoomPlayers.Remove(player);
        }
    }
   
}