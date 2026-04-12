using UnityEngine;
using Fusion;
using UnityEngine.InputSystem; // 🚨 Bắt buộc thêm dòng này để nhận diện phím Space mới

public class PalletInteraction_Fusion : NetworkBehaviour
{
    public enum PalletState { Up, Falling, Dropped, Destroyed }

    [Header("Cấu Hình Trạng Thái")]
    [Networked] public PalletState State { get; set; } = PalletState.Up;

    [Networked] private TickTimer FallTimer { get; set; }

    [Header("Tham Chiếu Render & Physics")]
    public Transform palletPivot;
    public GameObject stunZone;
    public GameObject breakZone;
    public GameObject spaceUI;

    [Header("Thông Số")]
    public float fallTime = 0.25f;
    public Quaternion droppedRotation = Quaternion.Euler(0, 0, 90);

    private ChangeDetector _changes;
    private bool _isLocalPlayerInZone = false;
    private bool _isSpawned = false;

    public override void Spawned()
    {
        _isSpawned = true;
        Debug.Log("🌐 [FUSION] Ván đã được Mạng Spawn thành công!");

        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        if (spaceUI != null) spaceUI.SetActive(false);
        UpdateVisuals();
    }

    public override void FixedUpdateNetwork()
    {
        if (!_isSpawned) return;

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
        if (!_isSpawned) return;

        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(State)) UpdateVisuals();
        }

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
                palletPivot.localRotation = droppedRotation;
                break;
            case PalletState.Destroyed:
                if (Object.HasStateAuthority) Runner.Despawn(Object);
                break;
        }
    }

    private void Update()
    {
        if (!_isSpawned) return;

        if (_isLocalPlayerInZone && State == PalletState.Up)
        {
            // 🚨 TỐI ƯU INPUT: Dùng New Input System để đọc phím Space thay vì hàm cũ
            bool isSpacePressed = false;

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isSpacePressed = true;
            }

            // Nếu dự án của bạn đang dùng cả 2 Input (Both), dự phòng thêm hàm cũ
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)) isSpacePressed = true;
#endif

            if (isSpacePressed)
            {
                Debug.Log("🟢 Đã bấm Space! Đang gửi lệnh ngã ván lên Server...");
                Rpc_RequestDropPallet();

                _isLocalPlayerInZone = false;
                if (spaceUI != null) spaceUI.SetActive(false);
            }
        }
    }

    // --- LOGIC NHẬN DIỆN PLAYER VÀ HUNTER ---
    private void OnTriggerEnter(Collider other)
    {
        // Đặt Debug LÊN TRÊN CÙNG để bắt quả tang vật lý
        Debug.Log("💥 [VẬT LÝ] Có vật thể chạm vào Ván: " + other.name);

        if (!_isSpawned)
        {
            Debug.LogError("❌ [LỖI FUSION] Vật lý có hoạt động, nhưng Mạng chưa Spawn Ván! (Cần kiểm tra lại Network Object)");
            return;
        }

        CheckLocalPlayerTrigger(other, true);

        if (State == PalletState.Falling && other.CompareTag("Hunter"))
        {
            var hunter = other.GetComponentInParent<HunterInteraction>();
            if (hunter != null) hunter.ApplyStun(3.0f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_isSpawned) return;
        CheckLocalPlayerTrigger(other, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_isSpawned) return;
        CheckLocalPlayerTrigger(other, false);
    }

    private void CheckLocalPlayerTrigger(Collider other, bool isInside)
    {
        if (State != PalletState.Up) return;

        var playerScript = other.GetComponentInParent<IShowSpeedController_Fusion>();

        if (playerScript != null)
        {
            // Kiểm tra xem nhân vật này có phải do máy mình điều khiển không
            if (playerScript.Object != null && playerScript.Object.HasInputAuthority)
            {
                if (isInside && !_isLocalPlayerInZone)
                    Debug.Log("✅ Player hợp lệ đã vào vùng ván! Bật UI.");

                _isLocalPlayerInZone = isInside;
                if (spaceUI != null) spaceUI.SetActive(isInside);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestDropPallet()
    {
        if (State == PalletState.Up)
        {
            State = PalletState.Falling;
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