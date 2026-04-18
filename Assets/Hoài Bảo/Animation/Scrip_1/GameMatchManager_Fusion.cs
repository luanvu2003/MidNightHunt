using UnityEngine;
using Fusion;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement; // Thêm để load scene local khi Quit

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

    // Biến đồng bộ mạng: Khi IsMatchEnded thay đổi, hàm OnMatchEnded sẽ chạy trên TẤT CẢ máy
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

    // Server gọi khi Survivor chết
    public void RegisterPlayerDeath()
    {
        if (!Runner.IsServer || IsMatchEnded) return;
        DeadSurvivors++;
        CheckEndGameCondition();
    }

    // Server gọi khi Survivor vào vùng Win
    public void RegisterPlayerEscape(NetworkObject playerObject)
    {
        if (!Runner.IsServer || IsMatchEnded) return;
        EscapedSurvivors++;

        // Hiện UI riêng cho người vừa thoát thành công
        RPC_ShowEndScreenIndividual(playerObject.InputAuthority, "BẠN ĐÃ THOÁT THÀNH CÔNG!");
        
        // Despawn nhân vật để họ không còn trên map
        Runner.Despawn(playerObject);

        CheckEndGameCondition();
    }

    private void CheckEndGameCondition()
    {
        if (DeadSurvivors + EscapedSurvivors >= TotalSurvivors)
        {
            IsMatchEnded = true; // Kích hoạt callback OnMatchEnded cho mọi người
        }
    }

    // Callback này tự chạy trên mọi máy (Host & Client) khi trận đấu kết thúc
    void OnMatchEnded()
    {
        if (IsMatchEnded)
        {
            string finalMsg = (DeadSurvivors == TotalSurvivors) ? "TOÀN BỘ SURVIVOR ĐÃ BỊ TIÊU DIỆT!" : "TRẬN ĐẤU KẾT THÚC!";
            ShowUI(finalMsg);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowEndScreenIndividual(PlayerRef player, string message)
    {
        if (Runner.LocalPlayer == player)
        {
            ShowUI(message);
        }
    }

    private void ShowUI(string message)
    {
        if (endGameCanvas != null)
        {
            endGameCanvas.SetActive(true);
            if (resultText != null) resultText.text = message;

            // ĐỒNG BỘ NÚT: Cả Host và Client đều thấy nút này
            if (returnRoomButton != null) returnRoomButton.SetActive(true);
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Gắn vào nút "Về Sảnh" (Back)
    public void Button_ReturnToRoom()
    {
        if (Runner.IsServer)
        {
            // Host bấm: Kéo cả phòng cùng về (giữ nguyên session)
            Runner.LoadScene(SceneRef.FromIndex(roomSceneBuildIndex));
        }
        else
        {
            // Client bấm: Thoát khỏi session hiện tại và quay về màn hình phòng
            // (Nếu Host chưa bấm về, client này sẽ rời nhóm)
            StartCoroutine(ClientReturnRoutine());
        }
    }

    private System.Collections.IEnumerator ClientReturnRoutine()
    {
        if (Runner != null)
        {
            Runner.Shutdown(); // Ngắt kết nối
        }
        
        // Đợi 1 frame để Shutdown hoàn tất
        yield return null; 
        
        // Load lại Scene Room bằng Unity thuần để quay về màn hình nhập ID/Lobby
        SceneManager.LoadScene(roomSceneBuildIndex);
    }

    // Gắn vào nút "Thoát Game" (Quit)
    public void Button_QuitGame()
    {
        if (Runner != null) Runner.Shutdown();
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}