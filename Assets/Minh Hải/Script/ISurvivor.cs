using Fusion;

public interface ISurvivor
{
    NetworkObject Object { get; }
    void TakeHit();
    float GetRepairSpeedMultiplier();
    void OnStartRepair();
    void OnStopRepair();
    void SetRepairAnimation(bool isRepairing);
    bool GetIsDowned();
    bool GetIsHooked();
    float GetRescueProgressRatio(); 
    void SetBeingRescued(bool isStarting, bool isUnhooking, float rescuerSpeed);
    void CompleteRescueFromOther();
    bool GetIsBeingUnhooked(); 
    float GetSacrificeTimer();
}