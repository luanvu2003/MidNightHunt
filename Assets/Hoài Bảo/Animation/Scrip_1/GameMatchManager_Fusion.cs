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
    public GameObject returnRoomButton; 
    public GameObject quitButton;       

    [Header("== CÀI ĐẶT SCENE ==")]
    public int roomSceneBuildIndex = 1;

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

    public void RegisterPlayerDeath(PlayerRef deadPlayer)
    {
        if (!Runner.IsServer || IsMatchEnded) return;
        DeadSurvivors++;

        // Báo cho riêng người chết
        RPC_NotifyPlayerFinished(deadPlayer, "BẠN ĐÃ TỬ TRẬN!");
        
        CheckEndGameCondition();
    }

    public void RegisterPlayerEscape(NetworkObject playerObject)
    {
        if (!Runner.IsServer || IsMatchEnded) return;
        EscapedSurvivors++;

        // Báo cho riêng người thoát
        RPC_NotifyPlayerFinished(playerObject.InputAuthority, "BẠN ĐÃ THOÁT THÀNH CÔNG!");

        // Xóa nhân vật
        Runner.Despawn(playerObject);

        CheckEndGameCondition();
    }

    private void CheckEndGameCondition()
    {
        if (DeadSurvivors + EscapedSurvivors >= TotalSurvivors)
        {
            IsMatchEnded = true; 
        }
    }

    // Hàm này CHỈ gọi cho Survivor vừa chết/thoát (Hunter KHÔNG nhận được)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerFinished(PlayerRef player, string message)
    {
        if (Runner.LocalPlayer == player)
        {
            ShowUI(message, false); 
        }
    }

    // Hàm gọi cho TẤT CẢ mọi người (Bao gồm cả Hunter) khi game thực sự kết thúc
    void OnMatchEnded()
    {
        if (IsMatchEnded)
        {
            string finalMsg;
            if (DeadSurvivors == TotalSurvivors)
                finalMsg = "HUNTER CHIẾN THẮNG!\n<color=red>TẤT CẢ SURVIVOR ĐÃ BỊ TIÊU DIỆT</color>";
            else if (EscapedSurvivors == TotalSurvivors)
                finalMsg = "SURVIVOR CHIẾN THẮNG!\n<color=green>TẤT CẢ ĐÃ THOÁT THÀNH CÔNG</color>";
            else
                finalMsg = $"TRẬN ĐẤU KẾT THÚC!\n<color=yellow>Thoát: {EscapedSurvivors} - Bị hạ: {DeadSurvivors}</color>";

            ShowUI(finalMsg, true); 
        }
    }

    private void ShowUI(string message, bool isGlobalEnd)
    {
        if (endGameCanvas != null) endGameCanvas.SetActive(true);
        if (resultText != null) resultText.text = message;

        bool isHost = Runner.IsServer;

        if (isHost && !isGlobalEnd)
        {
            // Host đang đợi -> Ẩn nút
            resultText.text += "\n\n<size=80%><color=#A8A8A8>(Bạn là Chủ Phòng. Vui lòng làm khán giả chờ ván đấu kết thúc để không làm sập phòng của người khác!)</color></size>";
            if (returnRoomButton != null) returnRoomButton.SetActive(false);
            if (quitButton != null) quitButton.SetActive(false);
        }
        else
        {
            // Game End toàn cục HOẶC Client bình thường -> Hiện nút
            if (returnRoomButton != null) returnRoomButton.SetActive(true);
            if (quitButton != null) quitButton.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Các hàm Nút bấm giữ nguyên của bạn...
    public void Button_ReturnToRoom()
    {
        if (Runner.IsServer)
            Runner.LoadScene(SceneRef.FromIndex(roomSceneBuildIndex));
        else
            StartCoroutine(ClientReturnRoutine());
    }

    private System.Collections.IEnumerator ClientReturnRoutine()
    {
        if (Runner != null) Runner.Shutdown();
        yield return null;
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