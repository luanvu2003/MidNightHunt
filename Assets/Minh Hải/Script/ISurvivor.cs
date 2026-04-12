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
    TickTimer GetRescueTimer();
    void SetBeingRescued(bool isStarting, float requiredTime);
    void CompleteRescueFromOther();
}