using Fusion;
using UnityEngine;
using System.Collections; // Thêm để dùng Coroutine
using System.Collections.Generic;
using System.Linq;

public class MapSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("== PREFAB NHÂN VẬT ==")]
    public NetworkObject[] hunterPrefabs; 
    public NetworkObject[] survivorPrefabs; 

    [Header("== VỊ TRÍ XUẤT HIỆN ==")]
    public Transform hunterSpawnPoint; 
    public Transform[] survivorSpawnPoints; 

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private int survivorSpawnIndex = 0; 

    // ... (Hàm Spawned và PlayerJoined giữ nguyên) ...
    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            foreach (var player in Runner.ActivePlayers) SpawnCharacter(player);
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer) SpawnCharacter(player);
    }

    private void SpawnCharacter(PlayerRef player)
    {
        if (spawnedCharacters.ContainsKey(player)) return;

        RoomPlayer roomData = FindObjectsOfType<RoomPlayer>().FirstOrDefault(p => p.Object.InputAuthority == player);

        if (roomData != null)
        {
            NetworkObject prefabToSpawn = null;
            Vector3 spawnPos = Vector3.zero;
            int charID = roomData.CharacterID;

            if (roomData.IsHunter) 
            {
                if (charID >= 0 && charID < hunterPrefabs.Length) prefabToSpawn = hunterPrefabs[charID];
                spawnPos = hunterSpawnPoint != null ? hunterSpawnPoint.position : new Vector3(448, 2, 156);
            }
            else 
            {
                if (charID >= 0 && charID < survivorPrefabs.Length) prefabToSpawn = survivorPrefabs[charID];
                if (survivorSpawnPoints != null && survivorSpawnPoints.Length > 0)
                {
                    spawnPos = survivorSpawnPoints[survivorSpawnIndex % survivorSpawnPoints.Length].position;
                    survivorSpawnIndex++;
                }
                else spawnPos = new Vector3(440, 2, 156);
            }

            if (prefabToSpawn != null)
            {
                NetworkObject charObj = Runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
                spawnedCharacters.Add(player, charObj);

                // 🚨 MẸO: Gọi Coroutine để fix lỗi dựt dựt bằng cách tắt CC tạm thời
                StartCoroutine(SafeTeleportRoutine(charObj, spawnPos));
            }
        }
    }

    // Hàm xử lý tắt/mở CharacterController
    IEnumerator SafeTeleportRoutine(NetworkObject obj, Vector3 targetPos)
    {
        yield return new WaitForSeconds(0.1f); // Chờ một nhịp cực ngắn để Fusion khởi tạo xong

        if (obj != null)
        {
            CharacterController cc = obj.GetComponent<CharacterController>();
            NetworkTransform nt = obj.GetComponent<NetworkTransform>();

            if (cc != null) cc.enabled = false; // Tắt vật lý

            obj.transform.position = targetPos;
            if (nt != null) nt.Teleport(targetPos); // Ép NetworkTransform đồng bộ ngay

            yield return new WaitForFixedUpdate(); // Chờ nhịp vật lý tiếp theo

            if (cc != null) cc.enabled = true; // Bật lại vật lý
            Debug.Log($"✅ Đã Reset CharacterController cho {obj.name}");
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Runner.IsServer && spawnedCharacters.TryGetValue(player, out NetworkObject charObj))
        {
            Runner.Despawn(charObj);
            spawnedCharacters.Remove(player);
        }
    }
}