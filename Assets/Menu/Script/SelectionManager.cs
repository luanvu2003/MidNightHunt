using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class SelectionManager : NetworkBehaviour
{
    [Header("Các Bảng UI (Kéo thả vào đây)")]
    public GameObject hunterPanel;
    public GameObject survivorPanel;
    [Header("Danh sách Nút Chọn Tướng")]
    public Button[] survivorButtons; 
    public Button[] hunterButtons;   
    [Header("Mô Hình 3D Hiện Thị")]
    public GameObject[] hunter3DModels;   
    public GameObject[] survivor3DModels; 

    [Header("Bệ Đứng 3D")]
    public GameObject[] hunterPlatforms;   
    public GameObject[] survivorPlatforms; 
    public override void Spawned()
    {
        if (hunterPanel != null) hunterPanel.SetActive(false);
        if (survivorPanel != null) survivorPanel.SetActive(false);
        TurnOffAll3DModels();
    }
    private void Update()
    {
        if (RoomPlayer.Local == null) return;
        if (RoomPlayer.Local.IsHunter && !hunterPanel.activeSelf)
        {
            hunterPanel.SetActive(true);
            survivorPanel.SetActive(false);
        }
        else if (!RoomPlayer.Local.IsHunter && !survivorPanel.activeSelf)
        {
            survivorPanel.SetActive(true);
            hunterPanel.SetActive(false);
        }
        UpdateCharacterButtons();
        Update3DModelsDisplay();
    }
    private void UpdateCharacterButtons()
    {
        var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();
        if (!RoomPlayer.Local.IsHunter && survivorButtons.Length > 0)
        {
            for (int i = 0; i < survivorButtons.Length; i++)
            {
                int charID = i; 
                bool isTakenByOther = allPlayers.Any(p => p != RoomPlayer.Local && !p.IsHunter && p.CharacterID == charID);
                survivorButtons[i].interactable = !isTakenByOther;
                survivorButtons[i].image.color = (RoomPlayer.Local.CharacterID == charID) ? Color.white : (isTakenByOther ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.7f, 0.7f, 0.7f));
            }
        }
        else if (hunterButtons.Length > 0)
        {
            for (int i = 0; i < hunterButtons.Length; i++)
            {
                int charID = i; 
                bool isSelected = (RoomPlayer.Local.CharacterID == charID);
                hunterButtons[i].image.color = isSelected ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f); 
            }
        }
    }
    private void Update3DModelsDisplay()
    {
        int myCharID = RoomPlayer.Local.CharacterID;
        if (RoomPlayer.Local.IsHunter)
        {
            for (int i = 0; i < hunter3DModels.Length; i++)
            {
                bool isSelected = (myCharID == i);
                if (hunter3DModels[i] != null) hunter3DModels[i].SetActive(isSelected);
                if (i < hunterPlatforms.Length && hunterPlatforms[i] != null) hunterPlatforms[i].SetActive(isSelected);
            }
        }
        else
        {
            for (int i = 0; i < survivor3DModels.Length; i++)
            {
                bool isSelected = (myCharID == i);
                if (survivor3DModels[i] != null) survivor3DModels[i].SetActive(isSelected);
                if (i < survivorPlatforms.Length && survivorPlatforms[i] != null) survivorPlatforms[i].SetActive(isSelected);
            }
        }
    }
    private void TurnOffAll3DModels()
    {
        foreach (var model in hunter3DModels) if (model != null) model.SetActive(false);
        foreach (var model in survivor3DModels) if (model != null) model.SetActive(false);
        foreach (var platform in hunterPlatforms) if (platform != null) platform.SetActive(false);
        foreach (var platform in survivorPlatforms) if (platform != null) platform.SetActive(false);
    }
    public void OnClickSelectCharacter(int id)
    {
        if (RoomPlayer.Local != null && !RoomPlayer.Local.IsReady)
        {
            RoomPlayer.Local.RPC_RequestCharacter(id);
        }
    }
}