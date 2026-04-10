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
    [Tooltip("Nhập chính xác tên GameObject text của Hunter vào đây")]
    public string hunterTextGameObjectName = "HunterNameText"; // <-- SỬA TẠI ĐÂY
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

    // HÀM MỚI: Tìm Text Hunter theo tên (Tìm được cả khi GameObject bị ẩn / SetActive = false)
    private TextMeshProUGUI FindHunterTextByName()
    {
        TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var txt in allTexts)
        {
            // Đảm bảo đúng tên và object đang thực sự có trong scene (tránh dính nhầm prefab chưa spawn trong folder)
            if (txt.gameObject.name == hunterTextGameObjectName && txt.gameObject.scene.isLoaded)
            {
                return txt;
            }
        }
        return null;
    }

    private void ResetAllText()
    {
        // 1. Reset bảng tên Survivor (nếu Survivor vẫn dùng UI gắn cứng trên Scene)
        foreach (var txt in survivorTexts) if (txt != null) { txt.text = ""; txt.gameObject.SetActive(false); }

        // 2. Reset bảng tên Hunter (Tìm trực tiếp bên trong các nhân vật đã được spawn)
        foreach (var kvp in spawnedCharacters)
        {
            NetworkObject spawnedObj = kvp.Value;
            if (spawnedObj != null)
            {
                // Tìm tất cả Text bên trong nhân vật này (bao gồm cả các object đang bị ẩn)
                var texts = spawnedObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                var hunterTxt = texts.FirstOrDefault(t => t.gameObject.name == hunterTextGameObjectName);
                
                if (hunterTxt != null)
                {
                    hunterTxt.text = "";
                    hunterTxt.gameObject.SetActive(false);
                }
            }
        }
    }

    IEnumerator UpdatePlayerNamesRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();

        // == CẬP NHẬT TÊN HUNTER ==
        var hunterData = allPlayers.FirstOrDefault(p => p.IsHunter);
        // Kiểm tra xem đã tìm thấy Data của Hunter chưa, VÀ Hunter đó đã được spawn ra map chưa
        if (hunterData != null && spawnedCharacters.TryGetValue(hunterData.Object.InputAuthority, out NetworkObject hunterObj))
        {
            // Tìm TextMeshProUGUI con bên trong Prefab Hunter đã spawn
            var texts = hunterObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI hunterTxt = texts.FirstOrDefault(t => t.gameObject.name == hunterTextGameObjectName);

            if (hunterTxt != null)
            {
                hunterTxt.gameObject.SetActive(true);
                hunterTxt.text = hunterData.PlayerName.ToString();
            }
            else
            {
                Debug.LogWarning($"[MapSpawner] Không tìm thấy GameObject nào tên '{hunterTextGameObjectName}' bên trong Prefab Hunter!");
            }
        }

        // == CẬP NHẬT TÊN SURVIVOR ==
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