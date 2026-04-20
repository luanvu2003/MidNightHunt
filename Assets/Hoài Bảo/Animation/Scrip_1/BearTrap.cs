using UnityEngine;
using Fusion;
public class BearTrap : NetworkBehaviour
{
    [Header("Thông tin chủ nhân")]
    public AttackController ownerHunter;
    [Header("Cài đặt Bẫy")]
    [Tooltip("Thời gian tự động hủy bẫy nếu không ai dẫm (giây)")]
    public float autoDestroyTime = 100f;
    [Networked] private NetworkBool isSprung { get; set; }
    [Networked] private TickTimer lifeTimer { get; set; }
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, autoDestroyTime);
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!isSprung && lifeTimer.Expired(Runner))
        {
            Debug.Log("⏳ Bẫy đã hết thời gian tồn tại (100s) và tự phân hủy.");
            Runner.Despawn(Object);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (!isSprung && other.CompareTag("Player"))
        {
            isSprung = true;
            Debug.Log("PHẬP! Đã bắt được Survivor: " + other.name);

            if (ownerHunter != null)
            {
                ownerHunter.RecoverTrap();
            }
            Runner.Despawn(Object);
        }
    }
    public void OnTrapDestroyedBySurvivor()
    {
        if (!Object.HasStateAuthority) return;

        if (!isSprung)
        {
            isSprung = true;
            Debug.Log(" Bẫy đã bị Survivor tháo gỡ!");

            if (ownerHunter != null)
            {
                ownerHunter.RecoverTrap();
            }

            Runner.Despawn(Object);
        }
    }
}