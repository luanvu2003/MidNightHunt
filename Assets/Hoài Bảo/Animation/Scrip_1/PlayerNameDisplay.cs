using Fusion;
using TMPro;
using UnityEngine;
using System.Linq;

public class PlayerNameDisplay : NetworkBehaviour
{
    [Header("Kéo thả Text tên của nhân vật vào đây")]
    public TextMeshProUGUI nameText;

    // Biến đồng bộ mạng chứa tên người chơi (tối đa 32 ký tự)
    [Networked] public NetworkString<_32> SyncedPlayerName { get; set; }

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        // Khởi tạo công cụ theo dõi thay đổi (Fusion V2)
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // Chỉ Server (Host) mới đi tìm dữ liệu RoomPlayer để gán tên gốc
        if (Object.HasStateAuthority)
        {
            var roomPlayers = FindObjectsOfType<RoomPlayer>();
            var myData = roomPlayers.FirstOrDefault(p => p.Object.InputAuthority == Object.InputAuthority);

            if (myData != null)
            {
                // Gán tên từ RoomPlayer vào biến đồng bộ mạng
                SyncedPlayerName = myData.PlayerName.ToString();
            }
        }

        // Cập nhật UI ngay lúc vừa spawn
        UpdateNameUI();
    }

    public override void Render()
    {
        // Liên tục lắng nghe: NẾU Server cập nhật tên, Client sẽ phát hiện ra và đổi Text
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(SyncedPlayerName):
                    UpdateNameUI();
                    break;
            }
        }
    }

    private void UpdateNameUI()
    {
        // Nếu UI Text không bị trống và biến tên đã có dữ liệu
        if (nameText != null && !string.IsNullOrEmpty(SyncedPlayerName.ToString()))
        {
            nameText.text = SyncedPlayerName.ToString();
            
            // Đảm bảo Object Text đã được bật lên
            if (!nameText.gameObject.activeSelf)
            {
                nameText.gameObject.SetActive(true);
            }
        }
    }
}