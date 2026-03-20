using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class MapSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("Nhân Vật 3D (Vào Game Mới Đẻ)")]
    public GameObject player3DPrefab; // Kéo Prefab nhân vật 3D của bạn vào đây

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    // CHẠY NGAY KHI MAP 2 LOAD XONG: Quét đẻ nhân vật cho những người đi từ phòng chờ sang
    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            foreach (var player in Runner.ActivePlayers)
            {
                SpawnCharacter(player);
            }
        }
    }

    // DÀNH CHO NHỮNG NGƯỜI VÀO SAU (LATE JOIN): Bỏ qua phòng chờ chui thẳng vào Map
    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            SpawnCharacter(player);
        }
    }

    // Tách riêng hàm đẻ nhân vật cho gọn code
    private void SpawnCharacter(PlayerRef player)
    {
        if (!spawnedCharacters.ContainsKey(player) && player3DPrefab != null)
        {
            NetworkObject charObj = Runner.Spawn(player3DPrefab, new Vector3(448, 2, 156), Quaternion.identity, player);
            spawnedCharacters.Add(player, charObj);
            Debug.Log($"⚔️ Đã thả nhân vật 3D của Player {player.PlayerId} xuống bản đồ!");
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Ai thoát game thì thu hồi xác nhân vật 3D của người đó
        if (Runner.IsServer && spawnedCharacters.TryGetValue(player, out NetworkObject charObj))
        {
            Runner.Despawn(charObj);
            spawnedCharacters.Remove(player);
        }
    }
}