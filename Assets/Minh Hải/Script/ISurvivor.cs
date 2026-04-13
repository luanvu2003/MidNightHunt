using Fusion;

public interface ISurvivor
{
    NetworkObject Object { get; }
    void TakeHit();

    // Phục vụ cho skill của Mr Bean
    float GetRepairSpeedMultiplier();
    void OnStartRepair();
    void OnStopRepair();
    void SetRepairAnimation(bool isRepairing);

    bool GetIsDowned();
    bool GetIsHooked();
    
    // 🚨 THAY ĐỔI: Dùng float từ 0 -> 1 để lấy % cứu (phục vụ Slider)
    float GetRescueProgressRatio(); 

    // 🚨 THAY ĐỔI: Đơn giản hóa tham số
    void SetBeingRescued(bool isStarting, bool isUnhookingAction);
    void CompleteRescueFromOther();
}