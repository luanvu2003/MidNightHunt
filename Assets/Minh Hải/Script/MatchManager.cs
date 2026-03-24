using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum GameState { Playing, HunterWin, SurvivorWin }

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;

    public GameState currentState = GameState.Playing;
    
    // Danh sách lưu trữ tất cả Survivor trong ván
    private List<SurvivorStatus> allSurvivors = new List<SurvivorStatus>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Tìm tất cả Survivor khi bắt đầu ván đấu
        allSurvivors = Object.FindObjectsByType<SurvivorStatus>(FindObjectsSortMode.None).ToList();
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            CheckHunterWinCondition();
        }
    }

    // Logic kiểm tra: Hunter thắng khi không còn Survivor nào có khả năng di chuyển
    void CheckHunterWinCondition()
    {
        if (allSurvivors.Count == 0) return;

        // Kiểm tra xem có phải TẤT CẢ đều đang bị gục, bị treo hoặc đã chết không
        bool isEveryoneFinished = allSurvivors.All(s => s.isDowned || s.isHooked || s.isDead);

        if (isEveryoneFinished)
        {
            OnHunterWin();
        }
    }

    void OnHunterWin()
    {
        currentState = GameState.HunterWin;
        Debug.Log("Hunter đã thắng ván đấu!");
        // Hiển thị UI kết quả hoặc chuyển Scene tại đây
    }

    // Hunter thắng ngay lập tức nếu tất cả Survivor đã chết (isDead)
    void CheckFinalDeath()
    {
        if (allSurvivors.All(s => s.isDead))
        {
            OnHunterWin();
        }
    }
}