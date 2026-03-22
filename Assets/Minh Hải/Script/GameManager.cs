using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Tạo một biến static (Singleton) để bất kỳ script nào cũng có thể gọi nó dễ dàng
    public static GameManager instance;
    [Header("Cài đặt Cửa Thoát Hiểm")]
    [Tooltip("Kéo thả các Cửa thoát hiểm (hoặc vùng tẩu thoát) vào đây")]
    public GameObject[] exitGates; // Dùng mảng để hỗ trợ nhiều cửa (VD: cửa Bắc, cửa Nam)

    [Header("Cài đặt Nhiệm vụ")]
    [Tooltip("Số lượng máy phát điện cần sửa để mở cửa thoát hiểm")]
    public int generatorsRemaining = 4;

    [Header("Cài đặt Nhân vật")]
    [Tooltip("Tổng số người chơi phe Survivor")]
    public int totalSurvivors = 4; 
    
    private int deadSurvivors = 0; // Số người đã chết/bị treo
    private bool isGameOver = false; // Khóa trạng thái khi game đã kết thúc

    void Awake()
    {
        // Thiết lập Singleton: Đảm bảo chỉ có duy nhất 1 GameManager tồn tại trong cảnh
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Khi mới vào game, giấu tất cả các cửa thoát hiểm đi
        foreach (GameObject gate in exitGates)
        {
            if (gate != null)
            {
                gate.SetActive(false);
            }
        }
    }

    // ================== LOGIC MÁY PHÁT ĐIỆN ==================

    // Hàm này sẽ được các máy phát điện gọi khi chúng được sửa xong 100%
    public void GeneratorCompleted()
    {
        if (isGameOver) return; // Nếu game đã kết thúc thì không tính nữa

        generatorsRemaining--; // Trừ đi 1 máy

        Debug.Log("Đã sửa xong 1 máy! Số máy cần sửa còn lại: " + generatorsRemaining);

        // Kiểm tra xem đã sửa hết máy chưa
        if (generatorsRemaining <= 0)
        {
            // Tránh việc bị số âm nếu người chơi cố tình sửa dư máy
            generatorsRemaining = 0; 
            AllGeneratorsDone();
        }
    }

   void AllGeneratorsDone()
    {
        Debug.Log("===============================================");
        Debug.Log("TẤT CẢ MÁY PHÁT ĐIỆN ĐÃ HOẠT ĐỘNG! CẤP ĐIỆN CHO CỔNG THOÁT HIỂM!");
        Debug.Log("===============================================");
        
        // Bật tất cả các cửa thoát hiểm lên để người chơi có thể chạy trốn
        foreach (GameObject gate in exitGates)
        {
            if (gate != null)
            {
                gate.SetActive(true);
            }
        }
    }

    // ================== LOGIC THẮNG / THUA ==================

    // 1. Hàm này được gọi khi có 1 Survivor bị Sát nhân giết hoặc treo chết
    public void SurvivorDied()
    {
        if (isGameOver) return; // Nếu game đã kết thúc thì bỏ qua

        deadSurvivors++;
        Debug.Log("Một Survivor đã gục ngã! Số người chết: " + deadSurvivors + "/" + totalSurvivors);

        // CHECK ĐIỀU KIỆN HUNTER THẮNG: Tất cả đều chết
        if (deadSurvivors >= totalSurvivors)
        {
            HunterWins();
        }
    }

    // 2. Hàm này được gọi khi có 1 Survivor chạm vào Vùng tẩu thoát
    public void SurvivorEscaped()
    {
        if (isGameOver) return;

        Debug.Log("Một Survivor đã thoát khỏi khu vực!");

        // CHECK ĐIỀU KIỆN SURVIVOR THẮNG: Chỉ cần ít nhất 1 người thoát
        SurvivorWins();
    }

    // ================== KẾT QUẢ ==================

    void HunterWins()
    {
        isGameOver = true;
        Debug.Log("============== GAME OVER ==============");
        Debug.Log("HUNTER WIN! Không một ai sống sót thoát khỏi đây...");
        
        // TODO: Xử lý UI hiện chữ Hunter Win
    }

    void SurvivorWins()
    {
        isGameOver = true;
        Debug.Log("============== GAME OVER ==============");
        Debug.Log("SURVIVOR WIN! Đã có người trốn thoát thành công!");
        
        // TODO: Xử lý UI hiện chữ Survivor Win
    }
}