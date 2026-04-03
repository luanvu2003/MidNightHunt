using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    // =========================================================
    // 1. CÁC BIẾN ĐỒNG BỘ MẠNG (Tất cả mọi người đều thấy)
    // =========================================================
    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public NetworkBool IsHunter { get; set; } // Phân loại phe: True = Hunter, False = Survivor

    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public int CharacterID { get; set; } // ID của tướng (Mặc định -1 là chưa chọn)

    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public NetworkBool IsReady { get; set; } // Cờ báo hiệu: Đã bấm nút Sẵn Sàng chưa?

    // Biến tĩnh để tìm ra chính bản thân mình trên máy tính của mình
    public static RoomPlayer Local;

    public override void Spawned()
    {
        // 🚨 BÙA BẤT TỬ: Đảm bảo cục dữ liệu này không bị xóa khi chuyển Scene (1 -> 2 -> 3 -> 4)
        DontDestroyOnLoad(gameObject);

        if (HasInputAuthority)
        {
            Local = this;
            // Vừa vào phòng: Gửi tên lên Server, ID tướng = -1 (Chưa chọn), Chưa Ready
            RPC_InitPlayer(PlayerInfo.Instance.PlayerName);
        }
        else
        {
            // Cập nhật UI cho những người khác thấy mình vào phòng
            UpdateLobbyUI();
        }
    }

    // =========================================================
    // 2. CÁC HÀM XIN PHÉP SERVER (RPC)
    // =========================================================

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_InitPlayer(NetworkString<_32> name)
    {
        PlayerName = name;
        CharacterID = -1; // Chưa chọn
        IsReady = false;
        IsHunter = false; // Mặc định ai cũng là Survivor cho đến lúc quay xổ số
    }

    // Server dùng hàm này để "Chỉ định" ai làm Hunter ở Scene 2
    public void SetRoleByServer(bool isHunterRole)
    {
        if (Runner.IsServer)
        {
            IsHunter = isHunterRole;
        }
    }

    // Hàm xin phép đổi tướng ở Scene 3
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacter(int wantedCharID)
    {
        // Nếu là Hunter thì cho chọn thoải mái
        if (IsHunter)
        {
            CharacterID = wantedCharID;
        }
        else // Nếu là Survivor thì phải kiểm tra chống trùng lặp
        {
            bool isTaken = false;
            var allPlayers = FindObjectsOfType<RoomPlayer>();

            foreach (var p in allPlayers)
            {
                // Nếu có đứa KHÁC, CÙNG PHE Survivor, mà ĐÃ LẤY ID này rồi -> Bị trùng!
                if (p != this && !p.IsHunter && p.CharacterID == wantedCharID)
                {
                    isTaken = true;
                    break;
                }
            }

            // Nếu chưa ai lấy thì Server mới duyệt cho phép đổi
            if (!isTaken)
            {
                CharacterID = wantedCharID;
            }
        }
    }

    // Hàm xin phép bấm nút Sẵn Sàng ở Scene 3
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ToggleReady()
    {
        IsReady = !IsReady; // Đảo trạng thái (Bấm 1 lần là Ready, bấm lần nữa là Hủy)
    }

    // =========================================================
    // 3. CẬP NHẬT GIAO DIỆN KHI CÓ THAY ĐỔI
    // =========================================================

    // Mỗi khi Tên, Role, Tướng, hoặc nút Ready thay đổi, hàm này sẽ tự động chạy
    public void OnDataChanged()
    {
        // Tùy vào việc đang đứng ở Scene nào mà ta cập nhật UI của Scene đó
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 1) // Giả sử Index 1 là Lobby
        {
            UpdateLobbyUI();
        }
        else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 3) // Giả sử Index 3 là Chọn Tướng
        {
            // Cập nhật UI chọn tướng (Sẽ viết ở Bước 3)
            // SelectionUI.Instance.UpdatePlayerSlot(this);
        }
    }

    private void UpdateLobbyUI()
    {
        if (RoomUI.Instance != null && PlayerName.Length > 0)
        {
            RoomUI.Instance.AddPlayer(PlayerName.ToString());
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (RoomUI.Instance != null && PlayerName.Length > 0)
        {
            RoomUI.Instance.RemovePlayer(PlayerName.ToString());
        }
    }
}