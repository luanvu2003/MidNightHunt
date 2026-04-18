using UnityEngine;
using Fusion;
using System.Linq;
using TMPro;

public class GameMatchManager_Fusion : NetworkBehaviour
{
    public static GameMatchManager_Fusion Instance;

    [Header("== UI KẾT THÚC GAME ==")]
    public GameObject endGameCanvas; // Kéo Canvas Win/Lose vào đây
    public TextMeshProUGUI resultText; // Chữ hiển thị "BẠN ĐÃ THOÁT" hoặc "ĐỘI SURVIVOR ĐÃ BỊ TIÊU DIỆT"
    public GameObject returnRoomButton; // Nút về sảnh

    [Header("== CÀI ĐẶT SCENE ==")]
    public int roomSceneBuildIndex = 1; // 🚨 Sửa số này thành Build Index của Scene Room (Lobby) trong Build Settings

    // --- Biến đồng bộ mạng ---
    [Networked] public int TotalSurvivors { get; set; }
    [Networked] public int DeadSurvivors { get; set; }
    [Networked] public int EscapedSurvivors { get; set; }
    [Networked] public NetworkBool IsMatchEnded { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        if (endGameCanvas != null) endGameCanvas.SetActive(false);
        
        // Đếm xem ván này có tổng cộng bao nhiêu Survivor
        if (Runner.IsServer)
        {
            StartCoroutine(CountPlayersRoutine());
        }
    }

    private System.Collections.IEnumerator CountPlayersRoutine()
    {
        // Đợi 2 giây để MapSpawner spawn xong hết nhân vật
        yield return new WaitForSeconds(2f);
        RoomPlayer[] allPlayers = FindObjectsOfType<RoomPlayer>();
        TotalSurvivors = allPlayers.Count(p => !p.IsHunter);
    }

    // Hàm gọi khi có người bay màu (Chết hẳn)
    public void RegisterPlayerDeath()
    {
        if (!Runner.IsServer || IsMatchEnded) return;

        DeadSurvivors++;
        CheckEndGameCondition();
    }

    // Hàm gọi khi có người chạy vào vùng Win
    public void RegisterPlayerEscape(NetworkObject playerObject)
    {
        if (!Runner.IsServer || IsMatchEnded) return;

        EscapedSurvivors++;

        // Gửi lệnh cho riêng người vừa thoát hiện chữ WIN
        RPC_ShowEndScreen(playerObject.InputAuthority, true, "BẠN ĐÃ THOÁT THÀNH CÔNG!");

        // Xóa nhân vật đó khỏi bản đồ
        Runner.Despawn(playerObject);

        CheckEndGameCondition();
    }

    // Kiểm tra xem game đã kết thúc chưa
    private void CheckEndGameCondition()
    {
        // NẾU TẤT CẢ SURVIVOR ĐỀU ĐÃ CHẾT (Hoặc thoát)
        if (DeadSurvivors + EscapedSurvivors >= TotalSurvivors)
        {
            IsMatchEnded = true;

            if (DeadSurvivors == TotalSurvivors)
            {
                // Hunter giết sạch -> Hunter Win, Survivor Lose toàn tập
                RPC_ShowEndScreenToAll("TOÀN BỘ SURVIVOR ĐÃ BỊ TIÊU DIỆT!");
            }
            else
            {
                // Có người thoát được
                RPC_ShowEndScreenToAll("TRẬN ĐẤU KẾT THÚC!");
            }
        }
    }

    // Hiện UI cho 1 người cụ thể (dành cho người vừa chạy thoát)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowEndScreen(PlayerRef player, bool isWin, string message)
    {
        // Chỉ hiện UI nếu đây là máy của người đó
        if (Runner.LocalPlayer == player)
        {
            ShowUI(message);
        }
    }

    // Hiện UI cho tất cả mọi người (khi game kết thúc hẳn)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowEndScreenToAll(string message)
    {
        ShowUI(message);
    }

    private void ShowUI(string message)
    {
        if (endGameCanvas != null)
        {
            endGameCanvas.SetActive(true);
            if (resultText != null) resultText.text = message;

            // Nút "Về Sảnh" chỉ để Host (Server) bấm để kéo cả phòng về
            // Nếu bạn muốn ai bấm cũng tự về, xem giải thích ở Bước 4
            if (returnRoomButton != null)
            {
                returnRoomButton.SetActive(Runner.IsServer); 
            }
        }
        
        // Mở khóa chuột để bấm UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ==========================================
    // CÁC HÀM GÁN VÀO NÚT (BUTTON ONCLICK)
    // ==========================================
    
    // Gắn vào nút "Về Sảnh"
   public void Button_ReturnToRoom()
    {
        if (Runner.IsServer)
        {
            // Dùng LoadScene của Fusion để đồng bộ chuyển Scene cho toàn bộ người chơi trong phòng
            Runner.LoadScene(SceneRef.FromIndex(roomSceneBuildIndex));
        }
    }

    // Gắn vào nút "Thoát Game"
    public void Button_QuitGame()
    {
        if (Runner != null) Runner.Shutdown(); // Ngắt kết nối mạng trước khi thoát
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}