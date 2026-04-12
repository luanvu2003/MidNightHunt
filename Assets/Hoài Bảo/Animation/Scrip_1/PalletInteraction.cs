using UnityEngine;
using Fusion;

public class PalletInteraction_Fusion : NetworkBehaviour
{
    public enum PalletState { Up, Falling, Dropped, Destroyed }

    [Header("Cấu Hình Trạng Thái")]
    [Networked] public PalletState State { get; set; } = PalletState.Up;
    
    // Đếm ngược thời gian ngã chuẩn mạng
    [Networked] private TickTimer FallTimer { get; set; } 

    [Header("Tham Chiếu Render & Physics")]
    public Transform palletPivot; 
    public GameObject stunZone;   
    public GameObject breakZone;  
    public GameObject spaceUI;    

    [Header("Thông Số")]
    [Tooltip("Thời gian để ván ngã xuống đất (giây)")]
    public float fallTime = 0.25f; // Tối ưu: Dùng thời gian cố định thay vì tốc độ
    public Quaternion droppedRotation = Quaternion.Euler(0, 0, 90); 

    private ChangeDetector _changes;
    private bool _isLocalPlayerInZone = false; // Biến kiểm tra xem Player đang ở gần ván không

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        if (spaceUI != null) spaceUI.SetActive(false);
        UpdateVisuals();
    }

    // 🚨 TỐI ƯU 1: Server quản lý việc chuyển trạng thái cực kỳ chuẩn xác
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && State == PalletState.Falling)
        {
            if (FallTimer.Expired(Runner))
            {
                State = PalletState.Dropped;
            }
        }
    }

    public override void Render()
    {
        // Nhận diện thay đổi trạng thái từ mạng
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(State)) UpdateVisuals();
        }

        // Xoay mượt mà (Chỉ ảnh hưởng phần nhìn)
        if (State == PalletState.Falling || State == PalletState.Dropped)
        {
            palletPivot.localRotation = Quaternion.Lerp(palletPivot.localRotation, droppedRotation, Time.deltaTime * 15f);
        }
    }

    private void UpdateVisuals()
    {
        switch (State)
        {
            case PalletState.Up:
                if (stunZone) stunZone.SetActive(false);
                if (breakZone) breakZone.SetActive(false);
                break;
            case PalletState.Falling:
                if (stunZone) stunZone.SetActive(true);
                if (breakZone) breakZone.SetActive(false);
                break;
            case PalletState.Dropped:
                if (stunZone) stunZone.SetActive(false);
                if (breakZone) breakZone.SetActive(true);
                palletPivot.localRotation = droppedRotation; // Chốt hạ góc
                break;
            case PalletState.Destroyed:
                if (Object.HasStateAuthority) Runner.Despawn(Object);
                break;
        }
    }

    // 🚨 TỐI ƯU 2: Chuyển Input ra Update để NHẠY BẤM 100%
    private void Update()
    {
        if (_isLocalPlayerInZone && State == PalletState.Up)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Rpc_RequestDropPallet();
                
                // Tắt UI ngay lập tức ở client để chống spam bấm nhiều lần
                _isLocalPlayerInZone = false; 
                if (spaceUI != null) spaceUI.SetActive(false);
            }
        }
    }

    // --- LOGIC NHẬN DIỆN PLAYER VÀ HUNTER ---
    private void OnTriggerEnter(Collider other)
    {
        CheckLocalPlayerTrigger(other, true);

        // Kiểm tra Hunter bị đập ván trúng đầu
        if (State == PalletState.Falling && other.CompareTag("Hunter"))
        {
            var hunter = other.GetComponentInParent<HunterInteraction>();
            if (hunter != null)
            {
                hunter.ApplyStun(3.0f); // Gây choáng 3 giây
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        CheckLocalPlayerTrigger(other, true);
    }

    private void OnTriggerExit(Collider other)
    {
        CheckLocalPlayerTrigger(other, false);
    }

    // Hàm dùng chung để bật/tắt UI khi Player ra/vào vùng
    private void CheckLocalPlayerTrigger(Collider other, bool isInside)
    {
        if (State != PalletState.Up) return;

        if (other.CompareTag("Player"))
        {
            var networkObj = other.GetComponentInParent<NetworkObject>();
            if (networkObj != null && networkObj.HasInputAuthority)
            {
                _isLocalPlayerInZone = isInside;
                if (spaceUI != null) spaceUI.SetActive(isInside);
            }
        }
    }

    // --- CÁC HÀM GỌI MẠNG (RPC) ---
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestDropPallet()
    {
        if (State == PalletState.Up)
        {
            State = PalletState.Falling;
            // Kích hoạt bộ đếm thời gian
            FallTimer = TickTimer.CreateFromSeconds(Runner, fallTime); 
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
}