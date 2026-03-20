using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class RoomSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("Room Prefab (Phòng chờ)")]
    public NetworkPrefabRef roomPlayerPrefab; // Kéo prefab RoomPlayer vào đây

    private Dictionary<PlayerRef, NetworkObject> spawnedRoomPlayers = new Dictionary<PlayerRef, NetworkObject>();

    public void PlayerJoined(PlayerRef player)
    {
        // Chỉ Host mới đẻ ra object quản lý trong phòng chờ
        if (Runner.IsServer && roomPlayerPrefab.IsValid)
        {
            var roomObj = Runner.Spawn(roomPlayerPrefab, Vector3.zero, Quaternion.identity, player);
            spawnedRoomPlayers.Add(player, roomObj);
            Debug.Log($"🏠 Đã spawn RoomPlayer cho Player {player.PlayerId} tại phòng chờ.");
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Runner.IsServer && spawnedRoomPlayers.TryGetValue(player, out NetworkObject roomObj))
        {
            Runner.Despawn(roomObj);
            spawnedRoomPlayers.Remove(player);
        }
    }
}