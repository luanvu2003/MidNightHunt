using Fusion;
using UnityEngine;
using TMPro;
using System.Collections;
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

    [Header("== UI TEXT (BẢNG TÊN) ==")]
    public TextMeshProUGUI[] hunterTexts;
    public TextMeshProUGUI[] survivorTexts;

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    // Server dùng biến này để đếm vị trí spawn cho Survivor
    private int survivorSpawnIndex = 0;

    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            // Reset index khi bắt đầu map
            survivorSpawnIndex = 0;
            foreach (var player in Runner.ActivePlayers)
            {
                SpawnCharacter(player);
            }
        }

        ResetAllText();
        StartCoroutine(UpdatePlayerNamesRoutine());
    }

    private void ResetAllText()
    {
        foreach (var txt in hunterTexts) if (txt != null) { txt.text = ""; txt.gameObject.SetActive(false); }
        foreach (var txt in survivorTexts) if (txt != null) { txt.text = ""; txt.gameObject.SetActive(false); }
    }

    IEnumerator UpdatePlayerNamesRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();

        // Cập nhật tên Hunter
        var hunterData = allPlayers.FirstOrDefault(p => p.IsHunter);
        if (hunterData != null)
        {
            int id = hunterData.CharacterID;
            if (id >= 0 && id < hunterTexts.Length && hunterTexts[id] != null)
            {
                hunterTexts[id].gameObject.SetActive(true);
                hunterTexts[id].text = hunterData.PlayerName.ToString();
            }
        }

        // Cập nhật tên Survivor
        var survivorsData = allPlayers.Where(p => !p.IsHunter).OrderBy(p => p.Object.InputAuthority.PlayerId).ToList();
        for (int i = 0; i < survivorsData.Count; i++)
        {
            if (i < survivorTexts.Length && survivorTexts[i] != null)
            {
                survivorTexts[i].gameObject.SetActive(true);
                survivorTexts[i].text = survivorsData[i].PlayerName.ToString();
            }
        }
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
                    int index = survivorSpawnIndex % survivorSpawnPoints.Length;
                    spawnPos = survivorSpawnPoints[index].position;
                    survivorSpawnIndex++;
                }
                else spawnPos = new Vector3(440, 2, 156);
            }

            if (prefabToSpawn != null)
            {
                // 🚨 ĐÃ SỬA: Bỏ thao tác với CharacterController ở đây. Để IShowSpeedController tự lo.
                NetworkObject charObj = Runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
                spawnedCharacters.Add(player, charObj);
            }
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer) SpawnCharacter(player);
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Xử lý dọn dẹp khi người chơi thoát
        if (Runner.IsServer)
        {
            // Tìm tất cả NetworkObject thuộc quyền của người chơi vừa thoát để Despawn
            foreach (var obj in Runner.GetAllNetworkObjects())
            {
                if (obj.InputAuthority == player)
                {
                    Runner.Despawn(obj);
                }
            }
        }
        ResetAllText();
        StartCoroutine(UpdatePlayerNamesRoutine());
    }
}