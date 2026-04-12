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
    
    [Header("Âm Thanh (Audio)")]
    public AudioSource audioSource;
    public AudioClip dropSound;    // Tiếng ván ngã (ầm!)
    public AudioClip breakSound;   // Tiếng ván bị Hunter đập vỡ (rắc!)
    public AudioClip stunSound;

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
            if (change == nameof(State))
            {
                UpdateVisuals();
                PlayStateSound(); // 🔊 Phát âm thanh khi đổi trạng thái
            }
        }

        if (State == PalletState.Falling || State == PalletState.Dropped)
        {
            palletPivot.localRotation = Quaternion.Lerp(palletPivot.localRotation, droppedRotation, Time.deltaTime * 15f);
        }
    }
    
    private void PlayStateSound()
    {
        if (audioSource == null) return;

        if (State == PalletState.Falling && dropSound != null)
            audioSource.PlayOneShot(dropSound);

        if (State == PalletState.Destroyed && breakSound != null)
            audioSource.PlayOneShot(breakSound);
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
                // 🚨 Tắt hiển thị Model ván và Trigger để người chơi đi qua được
                if (palletPivot != null) palletPivot.gameObject.SetActive(false);
                if (breakZone != null) breakZone.SetActive(false);

                // 🚨 Delay 0.6 giây để phát xong âm thanh đập vỡ rồi mới hủy Object mạng
                if (Object.HasStateAuthority) Invoke(nameof(DelayedDespawn), 0.6f);
                break;
        }
    }

    // 🚨 HÀM MỚI: Dùng để hủy Object sau khi Delay
    private void DelayedDespawn()
    {
        // Kiểm tra xem Object có còn hợp lệ trên mạng không trước khi xóa
        if (Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
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

    private void CheckLocalPlayerTrigger(Collider other, bool isInside)
    {
        if (State != PalletState.Up) return;

        if (other.CompareTag("Player"))
        {
            var netObj = other.GetComponentInParent<NetworkObject>();

            if (netObj != null)
            {
                if (netObj.HasInputAuthority)
                {
                    _isLocalPlayerInZone = isInside;
                    if (spaceUI != null) spaceUI.SetActive(isInside);
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestDropPallet()
    {
        if (State == PalletState.Up)
        {
            State = PalletState.Falling;
            FallTimer = TickTimer.CreateFromSeconds(Runner, fallTime);

            if (stunZone != null)
            {
                Collider stunCol = stunZone.GetComponent<Collider>();
                if (stunCol != null)
                {
                    Collider[] hits = Physics.OverlapBox(stunCol.bounds.center, stunCol.bounds.extents, stunZone.transform.rotation);

                    foreach (Collider hit in hits)
                    {
                        if (hit.CompareTag("Hunter"))
                        {
                            var hunter = hit.GetComponentInParent<HunterInteraction>();
                            if (hunter != null && hunter.StunTimer.ExpiredOrNotRunning(Runner))
                            {
                                hunter.ApplyStun(3.0f);
                            }
                        }
                    }
                }
            }
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