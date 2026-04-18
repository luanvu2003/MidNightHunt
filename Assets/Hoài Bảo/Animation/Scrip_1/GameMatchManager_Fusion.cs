using UnityEngine;
using Fusion;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

public class GameMatchManager_Fusion : NetworkBehaviour
{
    public static GameMatchManager_Fusion Instance;

    [Header("== UI KẾT THÚC GAME ==")]
    public GameObject endGameCanvas;
    public TextMeshProUGUI resultText;
    public GameObject returnRoomButton; // Nút "Về Phòng"
    public GameObject quitButton;       // Nút "Thoát Game"

    [Header("== CÀI ĐẶT SCENE ==")]
    public int roomSceneBuildIndex = 1;

    // Biến đồng bộ mạng
    [Networked, OnChangedRender(nameof(OnMatchEnded))]
    public NetworkBool IsMatchEnded { get; set; }

    [Networked] public int TotalSurvivors { get; set; }
    [Networked] public int DeadSurvivors { get; set; }
    [Networked] public int EscapedSurvivors { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        if (endGameCanvas != null) endGameCanvas.SetActive(false);

        if (Runner.IsServer)
        {
            StartCoroutine(CountPlayersRoutine());
        }
    }

    private System.Collections.IEnumerator CountPlayersRoutine()
    {
        yield return new WaitForSeconds(2f);
        RoomPlayer[] allPlayers = FindObjectsOfType<RoomPlayer>();
        TotalSurvivors = allPlayers.Count(p => !p.IsHunter);
    }

    // 🚨 ĐÃ SỬA: Cần truyền vào PlayerRef của người vừa chết
    public void RegisterPlayerDeath(PlayerRef deadPlayer)
    {
        if (!Runner.IsServer || IsMatchEnded) return;
        DeadSurvivors++;

        // Hiện UI riêng cho người vừa chết
        RPC_NotifyPlayerFinished(deadPlayer, "BẠN ĐÃ TỬ TRẬN!");
        
        CheckEndGameCondition();
    }

    // Server gọi khi Survivor vào vùng Win
    public void RegisterPlayerEscape(NetworkObject playerObject)
    {
        if (!Runner.IsServer || IsMatchEnded) return;
        EscapedSurvivors++;

        // Hiện UI riêng cho người vừa thoát thành công
        RPC_NotifyPlayerFinished(playerObject.InputAuthority, "BẠN ĐÃ THOÁT THÀNH CÔNG!");

        // Xóa nhân vật khỏi map
        Runner.Despawn(playerObject);

        CheckEndGameCondition();
    }

    private void CheckEndGameCondition()
    {
        // Khi tất cả Survivor đã xong (chết hoặc thoát) -> End Game toàn cục
        if (DeadSurvivors + EscapedSurvivors >= TotalSurvivors)
        {
            IsMatchEnded = true; 
        }
    }

    // Gửi UI cho cá nhân Survivor (khi họ chết hoặc thoát sớm)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerFinished(PlayerRef player, string message)
    {
        if (Runner.LocalPlayer == player)
        {
            ShowUI(message, false); // false = Game chưa kết thúc toàn cục
        }
    }

    // Hàm tự động gọi trên MỌI MÁY khi game thực sự kết thúc
    void OnMatchEnded()
    {
        if (IsMatchEnded)
        {
            string finalMsg = (DeadSurvivors == TotalSurvivors) ? "HUNTER CHIẾN THẮNG\nTOÀN BỘ SURVIVOR ĐÃ BỊ TIÊU DIỆT!" : "TRẬN ĐẤU KẾT THÚC!";
            ShowUI(finalMsg, true); // true = Game đã kết thúc toàn cục
        }
    }

    // Hàm xử lý hiển thị UI dựa trên vai trò
    private void ShowUI(string message, bool isGlobalEnd)
    {
        if (endGameCanvas != null) endGameCanvas.SetActive(true);
        if (resultText != null) resultText.text = message;

        bool isHost = Runner.IsServer;

        if (isHost && !isGlobalEnd)
        {
            // 🚨 TRƯỜNG HỢP: Host (Survivor) thoát/chết sớm.
            // Phải ẩn nút để Host không tắt nhầm làm sập phòng của những người đang chơi.
            resultText.text += "\n\n<size=80%>(Bạn là Chủ Phòng. Vui lòng làm khán giả chờ ván đấu kết thúc để không làm sập phòng của người khác!)</size>";
            if (returnRoomButton != null) returnRoomButton.SetActive(false);
            if (quitButton != null) quitButton.SetActive(false);
        }
        else
        {
            // TRƯỜNG HỢP: 
            // 1. Client thoát/chết sớm -> Bấm Rời đi bình thường.
            // 2. Game đã kết thúc (isGlobalEnd = true) -> Ai cũng hiện nút, kể cả Hunter và Host.
            if (returnRoomButton != null) returnRoomButton.SetActive(true);
            if (quitButton != null) quitButton.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // =====================================
    // NÚT BẤM UI
    // =====================================

    public void Button_ReturnToRoom()
    {
        if (Runner.IsServer)
        {
            // Nếu là End Game thực sự, Host bấm nút này sẽ Lôi toàn bộ những người còn lại về Lobby
            Runner.LoadScene(SceneRef.FromIndex(roomSceneBuildIndex));
        }
        else
        {
            // Client bấm -> Tự cắt kết nối mạng và Load lại giao diện Lobby
            StartCoroutine(ClientReturnRoutine());
        }
    }

    private System.Collections.IEnumerator ClientReturnRoutine()
    {
        if (Runner != null)
        {
            Runner.Shutdown(); // Ngắt kết nối khỏi phòng
        }

        // Đợi 1 frame để ngắt kết nối hoàn toàn
        yield return null;

        // Về lại màn hình sảnh
        SceneManager.LoadScene(roomSceneBuildIndex);
    }

    public void Button_QuitGame()
    {
        if (Runner != null) Runner.Shutdown();
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}