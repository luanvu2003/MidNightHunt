using UnityEngine;

public class SurvivorStatus : MonoBehaviour
{
    // Các trạng thái cơ bản
    public bool isDowned = false; // Bị gục
    public bool isHooked = false; // Đang trên móc
    public bool isDead = false;   // Đã chết/bị loại

    // Hàm cập nhật khi bị Hunter đánh gục
    public void SetDowned(bool state)
    {
        isDowned = state;
    }

    // Hàm cập nhật khi bị treo lên móc
    public void SetHooked(bool state)
    {
        isHooked = state;
        isDowned = false; // Nếu đang gục mà bị treo thì reset trạng thái gục
    }

    // Hàm gọi khi bị loại hoàn toàn khỏi ván đấu
    public void Eliminate()
    {
        isDead = true;
        isHooked = false;
        isDowned = false;
        gameObject.SetActive(false); // Ẩn nhân vật
    }
}