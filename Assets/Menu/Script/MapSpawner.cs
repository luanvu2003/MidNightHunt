using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Cần cái này để tìm RoomPlayer

public class MapSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("== PREFAB NHÂN VẬT ==")]
    [Tooltip("Kéo 3 Prefab Hunter (Búa, Bẫy, Ói Độc) vào đây theo thứ tự nút (0, 1, 2)")]
    public NetworkObject[] hunterPrefabs; 
    
    [Tooltip("Kéo 4 Prefab Survivor vào đây theo thứ tự nút (0, 1, 2, 3)")]
    public NetworkObject[] survivorPrefabs; 

    [Header("== VỊ TRÍ XUẤT HIỆN (SPAWN POINTS) ==")]
    public Transform hunterSpawnPoint; 
    public Transform[] survivorSpawnPoints; 

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private int survivorSpawnIndex = 0; 

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

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            SpawnCharacter(player);
        }
    }

    private void SpawnCharacter(PlayerRef player)
    {
        if (spawnedCharacters.ContainsKey(player)) return;

        // 1. TÌM DỮ LIỆU CỦA NGƯỜI CHƠI TỪ PHÒNG CHỜ (RoomPlayer)
        RoomPlayer roomData = FindObjectsOfType<RoomPlayer>().FirstOrDefault(p => p.Object.InputAuthority == player);

        if (roomData != null)
        {
            NetworkObject prefabToSpawn = null;
            Vector3 spawnPos = Vector3.zero;
            
            // Lấy ID mà người chơi đã chọn
            int charID = roomData.CharacterID;

            // 2. PHÂN LOẠI HUNTER VÀ SURVIVOR ĐỂ LẤY ĐÚNG PREFAB
            if (roomData.IsHunter) 
            {
                // Lấy Prefab Hunter
                if (charID >= 0 && charID < hunterPrefabs.Length)
                {
                    prefabToSpawn = hunterPrefabs[charID];
                }
                
                spawnPos = hunterSpawnPoint != null ? hunterSpawnPoint.position : new Vector3(448, 5, 142);
                Debug.Log($"🔪 Đã thả HUNTER (Loại {charID}) của Player {player.PlayerId} xuống bản đồ!");
            }
            else 
            {
                // Lấy Prefab Survivor
                if (charID >= 0 && charID < survivorPrefabs.Length)
                {
                    prefabToSpawn = survivorPrefabs[charID];
                }
                
                // Tránh việc Survivor đẻ đè lên nhau
                if (survivorSpawnPoints != null && survivorSpawnPoints.Length > 0)
                {
                    spawnPos = survivorSpawnPoints[survivorSpawnIndex % survivorSpawnPoints.Length].position;
                    survivorSpawnIndex++;
                }
                else
                {
                    spawnPos = new Vector3(440 + (survivorSpawnIndex * 2), 2, 156); 
                }
                Debug.Log($"🏃 Đã thả SURVIVOR (Loại {charID}) của Player {player.PlayerId} xuống bản đồ!");
            }

            // 3. THỰC HIỆN ĐẺ NHÂN VẬT VÀ CẤP QUYỀN
            if (prefabToSpawn != null)
            {
                NetworkObject charObj = Runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
                spawnedCharacters.Add(player, charObj);
            }
            else
            {
                Debug.LogError($"❌ LỖI: Không tìm thấy Prefab cho ID {charID}! Vui lòng kiểm tra lại mảng Prefabs trong MapSpawner ngoài Inspector.");
            }
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Thu hồi xác khi thoát game
        if (Runner.IsServer && spawnedCharacters.TryGetValue(player, out NetworkObject charObj))
        {
            Runner.Despawn(charObj);
            spawnedCharacters.Remove(player);
        }
    }
}