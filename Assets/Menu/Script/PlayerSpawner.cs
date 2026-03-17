using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    public NetworkPrefabRef roomPlayerPrefab; // Sẽ kéo prefab RoomPlayer_Prefab vào đây

    // Lưu trữ danh sách các object đã đẻ ra để lúc họ thoát còn biết đường xóa
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    public void PlayerJoined(PlayerRef player)
    {
        // Chỉ Chủ phòng (Server/Host) mới có quyền sinh ra object
        if (Runner.IsServer)
        {
            // Sinh ra cục RoomPlayer cho người vừa vào
            var obj = Runner.Spawn(roomPlayerPrefab, Vector3.zero, Quaternion.identity, player);
            spawnedPlayers.Add(player, obj);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Khi ai đó thoát, Host sẽ tìm và hủy object của người đó
        if (Runner.IsServer && spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            Runner.Despawn(obj);
            spawnedPlayers.Remove(player);
        }
    }
}