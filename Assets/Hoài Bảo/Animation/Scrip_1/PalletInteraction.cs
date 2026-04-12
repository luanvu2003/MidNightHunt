using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class PalletInteraction_Fusion : NetworkBehaviour
{
    public enum PalletState { Up, Falling, Dropped, Destroyed }

    [Header("Cấu Hình Trạng Thái")]
    [Networked] public PalletState State { get; set; } = PalletState.Up;

    [Header("Tham Chiếu Render & Physics")]
    public Transform palletPivot; // Điểm xoay của ván (nằm ở cạnh dưới)
    public GameObject stunZone;   // Vùng gây choáng (Trigger)
    public GameObject breakZone;  // Vùng để Hunter đập ván (Trigger)
    public GameObject spaceUI;    // Image "Space" để hiển thị

    [Header("Thông Số")]
    public float fallSpeed = 10f;
    public Quaternion droppedRotation = Quaternion.Euler(0, 0, 90); // Góc khi ván nằm xuống

    private ChangeDetector _changes;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        UpdateVisuals();
    }

    public override void Render()
    {
        // Đồng bộ hình ảnh mượt mà cho tất cả người chơi
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(State))
            {
                UpdateVisuals();
            }
        }

        // Hiệu ứng xoay mượt mà khi ván đang ngã
        if (State == PalletState.Falling)
        {
            palletPivot.localRotation = Quaternion.Lerp(palletPivot.localRotation, droppedRotation, Time.deltaTime * fallSpeed);

            // Nếu gần sát góc đích thì chuyển hẳn sang trạng thái Dropped (Chỉ Server làm)
            if (Object.HasStateAuthority && Quaternion.Angle(palletPivot.localRotation, droppedRotation) < 1f)
            {
                State = PalletState.Dropped;
            }
        }
    }

    private void UpdateVisuals()
    {
        switch (State)
        {
            case PalletState.Up:
                stunZone.SetActive(false);
                breakZone.SetActive(false);
                break;
            case PalletState.Falling:
                stunZone.SetActive(true);
                breakZone.SetActive(false);
                break;
            case PalletState.Dropped:
                palletPivot.localRotation = droppedRotation;
                stunZone.SetActive(false);
                breakZone.SetActive(true); // Hiện vùng cho Hunter đập
                break;
            case PalletState.Destroyed:
                if (Object.HasStateAuthority) Runner.Despawn(Object);
                break;
        }
    }

    // --- LOGIC CHO SURVIVOR ---
    private void OnTriggerStay(Collider other)
    {
        if (State != PalletState.Up) return;

        // Nếu là Player (Survivor) và có quyền điều khiển
        if (other.CompareTag("Player"))
        {
            var networkObj = other.GetComponentInParent<NetworkObject>();
            if (networkObj != null && networkObj.HasInputAuthority)
            {
                if (spaceUI != null) spaceUI.SetActive(true);

                // Kiểm tra bấm nút Space (Sử dụng hệ thống Input cũ hoặc mới tùy bạn)
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Rpc_RequestDropPallet();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var networkObj = other.GetComponentInParent<NetworkObject>();
            if (networkObj != null && networkObj.HasInputAuthority)
            {
                if (spaceUI != null) spaceUI.SetActive(false);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestDropPallet()
    {
        if (State == PalletState.Up)
        {
            State = PalletState.Falling;
        }
    }

    // --- LOGIC GÂY CHOÁNG HUNTER (Gắn vào StunZone) ---
    // Hàm này sẽ được gọi từ script con hoặc xử lý trực tiếp nếu Hunter chạm vào Falling Pallet
    public void NotifyHunterStun(GameObject hunterObj)
    {
        if (!Object.HasStateAuthority) return;

        // Giả sử Hunter có script tên là HunterInteraction như bạn đã gửi
        var hunter = hunterObj.GetComponentInParent<HunterInteraction>();
        if (hunter != null)
        {
            // Bạn cần thêm hàm ApplyStun vào script HunterInteraction
            // hunter.ApplyStun(); 
            Debug.Log("Hunter bị choáng bởi ván!");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_DestroyPallet()
    {
        if (State == PalletState.Dropped)
        {
            State = PalletState.Destroyed;
        }
    }
    // Trong PalletInteraction_Fusion.cs
    private void OnTriggerEnter(Collider other)
    {
        if (State == PalletState.Falling && other.CompareTag("Hunter"))
        {
            var hunter = other.GetComponentInParent<HunterInteraction>();
            if (hunter != null)
            {
                // Gây choáng 3 giây
                hunter.ApplyStun(3.0f);
            }
        }
    }
}