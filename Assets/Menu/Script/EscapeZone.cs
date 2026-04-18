using UnityEngine;
using Fusion;

public class EscapeZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!Runner.IsServer) return; // Chỉ Server mới được quyết định

        if (other.CompareTag("Player"))
        {
            var survivor = other.GetComponent<IShowSpeedController_Fusion>();

            // Kiểm tra xem nó có phải là Survivor hợp lệ không và chưa chết
            if (survivor != null && survivor.Object != null && survivor.Object.IsValid)
            {
                if (!survivor.IsDowned && !survivor.IsHooked)
                {
                    // Báo cáo đã thoát và xóa nhân vật
                    GameMatchManager_Fusion.Instance.RegisterPlayerEscape(survivor.Object);
                }
            }
        }
    }
}