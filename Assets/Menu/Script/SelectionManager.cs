using Fusion;
using UnityEngine;
using TMPro;
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

    [Header("Mô Hình 3D Hiện Thị (Gắn Object ngoài Scene)")]
    public GameObject[] hunter3DModels;   // Kéo 3 con Hunter 3D đang tắt vào đây
    public GameObject[] survivor3DModels; // Chuẩn bị sẵn cho Survivor sau này

    // 🚨 THÊM MỚI: Mảng chứa các Bệ đứng 3D
    [Header("Bệ Đứng 3D (Tương ứng với từng tướng)")]
    public GameObject[] hunterPlatforms;   // Kéo 3 cái bệ của Hunter vào đây
    public GameObject[] survivorPlatforms; // Kéo các bệ của Survivor vào đây

    [Header("Hiệu Ứng Sẵn Sàng")]
    public ParticleSystem readyParticle; // Kéo 1 cái hiệu ứng hạt vào đây (Nếu có)
    public AudioSource readyAudio;       // Kéo âm thanh chốt tướng vào đây (Nếu có)

    [Header("UI Trạng Thái")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI readyButtonText;

    [Networked] public TickTimer SelectionTimer { get; set; }

    public override void Spawned()
    {
        if (hunterPanel != null) hunterPanel.SetActive(false);
        if (survivorPanel != null) survivorPanel.SetActive(false);

        // Đảm bảo tất cả 3D Model VÀ Bệ đứng đều tắt lúc mới vào
        TurnOffAll3DModels();

        if (Runner.IsServer)
        {
            SelectionTimer = TickTimer.CreateFromSeconds(Runner, 60f);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (RoomPlayer.Local == null) return;

        // 1. HIỂN THỊ UI ĐÚNG PHE
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

        // 2. CẬP NHẬT TRẠNG THÁI NÚT VÀ MÔ HÌNH 3D
        UpdateCharacterButtons();
        Update3DModelsDisplay(); // 🚨 Gọi hàm bật tắt 3D và Bệ đứng

        // 3. ĐỔI CHỮ NÚT SẴN SÀNG
        if (readyButtonText != null)
        {
            readyButtonText.text = RoomPlayer.Local.IsReady ? "ĐÃ CHỐT" : "SẴN SÀNG";
            readyButtonText.color = RoomPlayer.Local.IsReady ? Color.green : Color.white;
        }

        CheckStartGameCondition();
    }

    private void UpdateCharacterButtons()
    {
        var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();

        // --- SURVIVOR ---
        if (!RoomPlayer.Local.IsHunter && survivorButtons.Length > 0)
        {
            for (int i = 0; i < survivorButtons.Length; i++)
            {
                int charID = i; 
                bool isTakenByOther = allPlayers.Any(p => p != RoomPlayer.Local && !p.IsHunter && p.CharacterID == charID);
                survivorButtons[i].interactable = !isTakenByOther;
                
                // Đổi màu
                survivorButtons[i].image.color = (RoomPlayer.Local.CharacterID == charID) ? Color.white : (isTakenByOther ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.7f, 0.7f, 0.7f));
            }
        }
        // --- HUNTER ---
        else if (hunterButtons.Length > 0)
        {
            for (int i = 0; i < hunterButtons.Length; i++)
            {
                int charID = i; 
                bool isSelected = (RoomPlayer.Local.CharacterID == charID);
                
                // LÀM XÁM NÚT NẾU KHÔNG ĐƯỢC CHỌN, SÁNG MÀU GỐC NẾU ĐƯỢC CHỌN
                hunterButtons[i].image.color = isSelected ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f); 
            }
        }
    }

    // 🚨 HÀM CẬP NHẬT: Bật mô hình 3D VÀ Bệ đứng tương ứng với ID đã chọn
    private void Update3DModelsDisplay()
    {
        int myCharID = RoomPlayer.Local.CharacterID;

        if (RoomPlayer.Local.IsHunter)
        {
            for (int i = 0; i < hunter3DModels.Length; i++)
            {
                bool isSelected = (myCharID == i);

                // Bật/tắt mô hình nhân vật
                if (hunter3DModels[i] != null) 
                    hunter3DModels[i].SetActive(isSelected);

                // Bật/tắt bệ đứng tương ứng
                if (i < hunterPlatforms.Length && hunterPlatforms[i] != null) 
                    hunterPlatforms[i].SetActive(isSelected);
            }
        }
        else
        {
            for (int i = 0; i < survivor3DModels.Length; i++)
            {
                bool isSelected = (myCharID == i);

                // Bật/tắt mô hình nhân vật
                if (survivor3DModels[i] != null) 
                    survivor3DModels[i].SetActive(isSelected);

                // Bật/tắt bệ đứng tương ứng
                if (i < survivorPlatforms.Length && survivorPlatforms[i] != null) 
                    survivorPlatforms[i].SetActive(isSelected);
            }
        }
    }

    private void TurnOffAll3DModels()
    {
        // Tắt nhân vật
        foreach (var model in hunter3DModels) if (model != null) model.SetActive(false);
        foreach (var model in survivor3DModels) if (model != null) model.SetActive(false);
        
        // Tắt luôn bệ đứng
        foreach (var platform in hunterPlatforms) if (platform != null) platform.SetActive(false);
        foreach (var platform in survivorPlatforms) if (platform != null) platform.SetActive(false);
    }

    public void OnClickSelectCharacter(int id)
    {
        if (RoomPlayer.Local != null && !RoomPlayer.Local.IsReady) // Nếu đã Ready thì khóa không cho đổi nữa
        {
            RoomPlayer.Local.RPC_RequestCharacter(id);
        }
    }

    public void OnClickToggleReady()
    {
        if (RoomPlayer.Local != null)
        {
            if (RoomPlayer.Local.CharacterID >= 0)
            {
                RoomPlayer.Local.RPC_ToggleReady();

                // PHÁT HIỆU ỨNG KHI BẤM CHỐT TƯỚNG (Chỉ chạy trên máy mình)
                if (!RoomPlayer.Local.IsReady) 
                {
                    if (readyParticle != null) readyParticle.Play();
                    if (readyAudio != null) readyAudio.Play();
                }
            }
        }
    }

    private void CheckStartGameCondition()
    {
        if (SelectionTimer.IsRunning && timerText != null)
        {
            int timeLeft = Mathf.CeilToInt(SelectionTimer.RemainingTime(Runner) ?? 0);
            timerText.text = $"Vào game sau: {timeLeft}s";
        }

        if (!Runner.IsServer) return; 

        var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();
        bool isAllReady = allPlayers.Count > 0 && allPlayers.All(p => p.IsReady);

        if (isAllReady || SelectionTimer.Expired(Runner))
        {
            SelectionTimer = TickTimer.None; 
            Runner.LoadScene(SceneRef.FromIndex(4)); 
        }
    }
}