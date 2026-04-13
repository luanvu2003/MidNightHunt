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
    public string hunterTextGameObjectName = "HunterNameText";
    public TextMeshProUGUI[] survivorTexts;

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private int survivorSpawnIndex = 0;

    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            survivorSpawnIndex = 0;
            foreach (var player in Runner.ActivePlayers)
            {
                SpawnCharacter(player);
            }
        }

        ResetAllText();
        StartCoroutine(UpdatePlayerNamesRoutine());
    }

    private TextMeshProUGUI FindHunterTextByName()
    {
        TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var txt in allTexts)
        {
            if (txt.gameObject.name == hunterTextGameObjectName && txt.gameObject.scene.isLoaded)
            {
                return txt;
            }
        }
        return null;
    }

    private void ResetAllText()
    {
        foreach (var txt in survivorTexts) if (txt != null) { txt.text = ""; txt.gameObject.SetActive(false); }

        foreach (var kvp in spawnedCharacters)
        {
            NetworkObject spawnedObj = kvp.Value;
            if (spawnedObj != null)
            {
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

    // 🚨 THAY ĐỔI: Sử dụng Vòng lặp thử lại (Retry Loop) để đảm bảo Host/Client load xong 100%
    IEnumerator UpdatePlayerNamesRoutine()
    {
        int retries = 10; // Thử tối đa 10 lần (tương đương 5 giây) để đợi UI load xong
        
        while (retries > 0)
        {
            yield return new WaitForSeconds(0.5f);
            bool successAll = true;

            var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();

            // == CẬP NHẬT TÊN HUNTER ==
            var hunterData = allPlayers.FirstOrDefault(p => p.IsHunter);
            if (hunterData != null)
            {
                NetworkObject hunterObj = null;

                if (spawnedCharacters.TryGetValue(hunterData.Object.InputAuthority, out NetworkObject cachedObj))
                {
                    hunterObj = cachedObj;
                }
                else
                {
                    foreach (var networkObj in Runner.GetAllNetworkObjects())
                    {
                        if (networkObj.InputAuthority == hunterData.Object.InputAuthority && networkObj.GetComponent<RoomPlayer>() == null)
                        {
                            hunterObj = networkObj;
                            break;
                        }
                    }
                }

                if (hunterObj != null)
                {
                    var texts = hunterObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                    TextMeshProUGUI hunterTxt = texts.FirstOrDefault(t => t.gameObject.name == hunterTextGameObjectName);

                    if (hunterTxt != null)
                    {
                        hunterTxt.gameObject.SetActive(true);
                        hunterTxt.text = hunterData.PlayerName.ToString();
                    }
                    else
                    {
                        successAll = false; // UI chưa load xong, thử lại vòng lặp sau
                    }
                }
                else
                {
                    successAll = false; // Model chưa load xong, thử lại vòng lặp sau
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

            // Nếu mọi thứ (Cả model và Text) đã được tìm thấy và set thành công, kết thúc Coroutine
            if (successAll) break;
            
            retries--;
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
                NetworkObject charObj = Runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
                spawnedCharacters.Add(player, charObj);
            }
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer) SpawnCharacter(player);
        // 🚨 THAY ĐỔI: Gọi lại update tên để đồng bộ cho người mới vào sau
        StartCoroutine(UpdatePlayerNamesRoutine());
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Runner.IsServer)
        {
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