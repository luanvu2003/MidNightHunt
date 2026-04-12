using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

public class PalletInteraction : NetworkBehaviour
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
            bool isSpacePressed = false;

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isSpacePressed = true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)) isSpacePressed = true;
#endif

            if (isSpacePressed)
            {
                Rpc_RequestDropPallet();
                _isLocalPlayerInZone = false;
                if (spaceUI != null) spaceUI.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isSpawned) return;

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

    // 🚨 ĐÃ FIX: DÙNG NETWORK OBJECT ĐỂ NHẬN DIỆN CẢ 4 NHÂN VẬT
    private void CheckLocalPlayerTrigger(Collider other, bool isInside)
    {
        if (State != PalletState.Up) return;

        // Chỉ xét những vật thể có Tag là Player
        if (other.CompareTag("Player"))
        {
            // Lấy thẳng NetworkObject thay vì script của từng nhân vật
            var netObj = other.GetComponentInParent<NetworkObject>();

            if (netObj != null)
            {
                // Nếu đây là nhân vật do máy mình điều khiển
                if (netObj.HasInputAuthority)
                {
                    _isLocalPlayerInZone = isInside;
                    if (spaceUI != null) spaceUI.SetActive(isInside);
                }
            }
        }
    }

    // 🚨 SỬA InputAuthority THÀNH All Ở ĐÂY:
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
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