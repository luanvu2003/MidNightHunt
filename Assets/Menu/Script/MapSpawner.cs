using Fusion;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class MapSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("== PREFAB NHÂN VẬT ==")]
    public NetworkObject[] hunterPrefabs; 
    public NetworkObject[] survivorPrefabs; 

    [Header("== VỊ TRÍ XUẤT HIỆN ==")]
    public Transform hunterSpawnPoint; 
    public Transform[] survivorSpawnPoints; 

    [Header("== UI TEXT HUNTER (3 Ô) ==")]
    [Tooltip("Kéo 3 ô Text của 3 Hunter vào đây theo thứ tự ID 0, 1, 2")]
    public TextMeshProUGUI[] hunterTexts;

    [Header("== UI TEXT SURVIVOR (4 Ô) ==")]
    [Tooltip("Kéo 4 ô Text của 4 Survivor vào đây")]
    public TextMeshProUGUI[] survivorTexts;

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private int survivorSpawnIndex = 0; 

    public override void Spawned()
    {
        // 1. Server đẻ nhân vật
        if (Runner.IsServer)
        {
            foreach (var player in Runner.ActivePlayers) SpawnCharacter(player);
        }

        // 2. Dọn dẹp Text cũ
        ResetAllText();

        // 3. Cập nhật bảng tên
        StartCoroutine(UpdatePlayerNamesRoutine());
    }

    private void ResetAllText()
    {
        // Xóa sạch chữ và ẩn các Object Text đi để tránh hiện rác
        foreach (var txt in hunterTexts) if (txt != null) { txt.text = ""; txt.gameObject.SetActive(false); }
        foreach (var txt in survivorTexts) if (txt != null) { txt.text = ""; txt.gameObject.SetActive(false); }
    }

    IEnumerator UpdatePlayerNamesRoutine()
    {
        // Đợi đồng bộ mạng
        yield return new WaitForSeconds(1.0f);

        var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();

        // --- 🏹 XỬ LÝ TÊN HUNTER ---
        var hunterData = allPlayers.FirstOrDefault(p => p.IsHunter);
        if (hunterData != null)
        {
            int id = hunterData.CharacterID; // 0, 1 hoặc 2
            if (id >= 0 && id < hunterTexts.Length && hunterTexts[id] != null)
            {
                hunterTexts[id].gameObject.SetActive(true);
                hunterTexts[id].text = hunterData.PlayerName.ToString();
            }
        }

        // --- 🏃 XỬ LÝ TÊN SURVIVOR ---
        var survivorsData = allPlayers.Where(p => !p.IsHunter).ToList();
        for (int i = 0; i < survivorsData.Count; i++)
        {
            if (i < survivorTexts.Length && survivorTexts[i] != null)
            {
                survivorTexts[i].gameObject.SetActive(true);
                survivorTexts[i].text = survivorsData[i].PlayerName.ToString();
            }
        }
    }

    // --- CÁC HÀM SPAWN VÀ ĐIỀU KIỆN MẠNG GIỮ NGUYÊN ---
    private void SpawnCharacter(PlayerRef player)
    {
        if (spawnedCharacters.ContainsKey(player)) return;
        RoomPlayer roomData = FindObjectsOfType<RoomPlayer>().FirstOrDefault(p => p.Object.InputAuthority == player);

        if (roomData != null)
        {
            NetworkObject prefabToSpawn = null;
            Vector3 spawnPos = Vector3.zero;
            int charID = roomData.CharacterID;

            if (roomData.IsHunter) {
                if (charID >= 0 && charID < hunterPrefabs.Length) prefabToSpawn = hunterPrefabs[charID];
                spawnPos = hunterSpawnPoint != null ? hunterSpawnPoint.position : new Vector3(448, 2, 156);
            } else {
                if (charID >= 0 && charID < survivorPrefabs.Length) prefabToSpawn = survivorPrefabs[charID];
                if (survivorSpawnPoints != null && survivorSpawnPoints.Length > 0) {
                    spawnPos = survivorSpawnPoints[survivorSpawnIndex % survivorSpawnPoints.Length].position;
                    survivorSpawnIndex++;
                } else spawnPos = new Vector3(440, 2, 156);
            }

            if (prefabToSpawn != null) {
                NetworkObject charObj = Runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
                spawnedCharacters.Add(player, charObj);
                StartCoroutine(SafeTeleportRoutine(charObj, spawnPos));
            }
        }
    }

    IEnumerator SafeTeleportRoutine(NetworkObject obj, Vector3 targetPos) {
        yield return new WaitForSeconds(0.1f);
        if (obj != null) {
            CharacterController cc = obj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; 
            obj.transform.position = targetPos;
            yield return new WaitForFixedUpdate();
            if (cc != null) cc.enabled = true; 
        }
    }

    public void PlayerJoined(PlayerRef player) { if (Runner.IsServer) SpawnCharacter(player); }
    public void PlayerLeft(PlayerRef player)
    {
        if (Runner.IsServer && spawnedCharacters.TryGetValue(player, out NetworkObject charObj))
        {
            Runner.Despawn(charObj);
            spawnedCharacters.Remove(player);
        }
        ResetAllText();
        StartCoroutine(UpdatePlayerNamesRoutine());
    }
}