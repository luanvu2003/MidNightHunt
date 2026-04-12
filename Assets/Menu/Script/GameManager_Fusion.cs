using Fusion;
using TMPro;
using UnityEngine;

public class GameManager_Fusion : NetworkBehaviour
{
    // Tạo Singleton để các Máy phát điện dễ dàng tìm thấy GameManager
    public static GameManager_Fusion Instance;

    [Header("UI Reference")]
    // Kéo Text (TMP) của "MáyPĐ" vào đây
    public TextMeshProUGUI generatorsRemainingText;

    [Header("Game Settings")]
    // Số lượng máy tối đa cần sửa để mở cửa
    [Networked] public int GeneratorsRemaining { get; set; } = 4;

    public override void Spawned()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void Render()
    {
        // Fusion sẽ liên tục đồng bộ biến mạng này lên UI của tất cả Client
        if (generatorsRemainingText != null)
        {
            generatorsRemainingText.text = GeneratorsRemaining.ToString();
        }
    }

    // Hàm này được gọi từ Generator khi tiến trình đạt 100%
    public void OnGeneratorRepaired()
    {
        // 🚨 CHỈ SERVER mới có quyền trừ số máy
        if (!Object.HasStateAuthority) return; 

        if (GeneratorsRemaining > 0)
        {
            GeneratorsRemaining--; // Trừ đi 1

            // Kiểm tra nếu đã về 0 (Chặn không cho xuống âm)
            if (GeneratorsRemaining <= 0)
            {
                GeneratorsRemaining = 0;
                
                // Tiền đề cho cửa thoát hiểm sau này
                Debug.Log("--- ĐÃ SỬA XONG 4 MÁY! SẴN SÀNG KÍCH HOẠT CỬA THOÁT HIỂM! ---");
                // Mốt bạn sẽ viết code kích hoạt cửa ở đây
            }
        }
    }
}