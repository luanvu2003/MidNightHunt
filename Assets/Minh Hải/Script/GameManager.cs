using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Tạo một biến static (Singleton) để bất kỳ script nào cũng có thể gọi nó dễ dàng
    public static GameManager instance;

    [Header("Cài đặt Nhiệm vụ")]
    [Tooltip("Số lượng máy phát điện cần sửa để mở cửa thoát hiểm")]
    public int generatorsRemaining = 4;

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

    // Hàm này sẽ được các máy phát điện gọi khi chúng được sửa xong 100%
    public void GeneratorCompleted()
    {
        generatorsRemaining--; // Trừ đi 1 máy

        Debug.Log("Đã sửa xong 1 máy! Số máy cần sửa còn lại: " + generatorsRemaining);

        // TODO: Cập nhật UI hiển thị số máy còn lại lên màn hình ở đây (nếu có)

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
        
        // Bạn có thể viết thêm code bật đèn báo động, phát âm thanh hú còi, 
        // hoặc gọi script mở cửa thoát hiểm (Exit Gates) ở đây.
    }
}