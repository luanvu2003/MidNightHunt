using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Thêm thư viện này để dùng HashSet

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

    [Header("Thông Số Mới (Dễ chỉnh)")]
    public float fallTime = 0.25f;
    [Tooltip("Góc gập xuống khi ngã. (Thử X=90 hoặc X=-90)")]
    public Vector3 dropRotationOffset = new Vector3(90, 0, 0);

    [Header("Hệ Thống Đẩy Tránh Kẹt (Push Out)")]
    [Tooltip("Khoảng cách đẩy nhân vật văng ra khỏi ván (đơn vị: mét)")]
    public float pushOutDistance = 1.8f;

    // Lưu trữ góc tự động tính toán
    private Quaternion _startRotation;
    private Quaternion _targetDroppedRotation;

    private ChangeDetector _changes;
    private bool _isLocalPlayerInZone = false;
    private bool _isSpawned = false;

    [Header("Âm Thanh (Audio)")]
    public AudioSource audioSource;
    public AudioClip dropSound;
    public AudioClip breakSound;
    public AudioClip stunSound;

    public override void Spawned()
    {
        _isSpawned = true;
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        _startRotation = palletPivot.localRotation;
        _targetDroppedRotation = _startRotation * Quaternion.Euler(dropRotationOffset);

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
                PlayStateSound();
            }
        }

        if (State == PalletState.Falling || State == PalletState.Dropped)
        {
            palletPivot.localRotation = Quaternion.Lerp(palletPivot.localRotation, _targetDroppedRotation, Time.deltaTime * 15f);
        }
    }

    private void PlayStateSound()
    {
        if (audioSource == null) return;
        if (State == PalletState.Falling && dropSound != null) audioSource.PlayOneShot(dropSound);
        if (State == PalletState.Destroyed && breakSound != null) audioSource.PlayOneShot(breakSound);
    }

    private void UpdateVisuals()
    {
        if (State != PalletState.Up)
        {
            if (spaceUI != null && _isLocalPlayerInZone)
            {
                spaceUI.SetActive(false);
            }
            _isLocalPlayerInZone = false;
        }

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
                palletPivot.localRotation = _targetDroppedRotation;
                break;
            case PalletState.Destroyed:
                if (palletPivot != null) palletPivot.gameObject.SetActive(false);
                if (breakZone != null) breakZone.SetActive(false);
                if (Object.HasStateAuthority) Invoke(nameof(DelayedDespawn), 0.6f);
                break;
        }
    }

    private void DelayedDespawn()
    {
        if (Object != null && Object.IsValid) Runner.Despawn(Object);
    }

    private void Update()
    {
        if (!_isSpawned) return;

        if (_isLocalPlayerInZone && State == PalletState.Up)
        {
            bool isSpacePressed = false;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) isSpacePressed = true;
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

        // Stun Hunter nếu đi vào vùng ván đang rơi
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

        if (other.CompareTag("Player") || other.CompareTag("Playerchet"))
        {
            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority)
            {
                _isLocalPlayerInZone = isInside;
                if (spaceUI != null) spaceUI.SetActive(isInside);
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
                    // Dùng HashSet để đảm bảo 1 nhân vật có nhiều collider cũng chỉ bị đẩy 1 lần
                    HashSet<Transform> processedCharacters = new HashSet<Transform>();

                    Collider[] hits = Physics.OverlapBox(stunCol.bounds.center, stunCol.bounds.extents, stunZone.transform.rotation);
                    foreach (Collider hit in hits)
                    {
                        // 1. STUN HUNTER (Như cũ)
                        if (hit.CompareTag("Hunter"))
                        {
                            var hunter = hit.GetComponentInParent<HunterInteraction>();
                            if (hunter != null && hunter.StunTimer.ExpiredOrNotRunning(Runner))
                            {
                                hunter.ApplyStun(3.0f);
                            }
                        }

                        // 2. ĐẨY HUNTER & SURVIVOR RA KHỎI VÁN KHI VÁN NGÃ
                        if (hit.CompareTag("Hunter") || hit.CompareTag("Player") || hit.CompareTag("Playerchet"))
                        {
                            Transform rootTransform = hit.transform.root; // Lấy object gốc của nhân vật

                            // Nếu nhân vật này chưa bị đẩy
                            if (!processedCharacters.Contains(rootTransform))
                            {
                                processedCharacters.Add(rootTransform);
                                PushCharacterOut(rootTransform);
                            }
                        }
                    }
                }
            }
        }
    }

    // 🚨 HÀM TÍNH TOÁN VÀ ĐẨY NHÂN VẬT ÉP CHUẨN THEO TRỤC Z (TIẾN/LÙI) 🚨
    private void PushCharacterOut(Transform charTransform)
    {
        CharacterController cc = charTransform.GetComponent<CharacterController>();

        // Dùng transform GỐC của Ván (cái khung cố định, không bao giờ bị xoay khi ván ngã)
        Transform baseTransform = this.transform;

        // Quy đổi tọa độ để biết nhân vật đứng trước hay sau cái khung ván
        Vector3 localPos = baseTransform.InverseTransformPoint(charTransform.position);
        Vector3 pushDirection = Vector3.zero;

        // ÉP BUỘC CHỈ ĐẨY DỌC THEO TRỤC Z (Forward/Backward) CỦA KHUNG VÁN
        if (localPos.z <= 0)
        {
            pushDirection = -baseTransform.forward; // Đứng nửa sau -> Đẩy thẳng lùi về sau
        }
        else
        {
            pushDirection = baseTransform.forward;  // Đứng nửa trước -> Đẩy thẳng lên trước
        }

        // Triệt tiêu trục Y để đẩy song song mặt đất hoàn toàn
        pushDirection.y = 0;
        if (pushDirection != Vector3.zero)
        {
            pushDirection.Normalize();
        }

        // ==================================================
        // 🚨 HỆ THỐNG RAYCAST CHỐNG XUYÊN TƯỜNG
        // ==================================================
        float safePushDistance = pushOutDistance;

        Vector3 castOrigin = charTransform.position + Vector3.up * 1.0f;
        float characterRadius = 0.3f;

        if (Physics.SphereCast(castOrigin, characterRadius, pushDirection, out RaycastHit hit, pushOutDistance))
        {
            if (!hit.collider.isTrigger &&
                !hit.collider.CompareTag("Player") &&
                !hit.collider.CompareTag("Hunter") &&
                !hit.collider.CompareTag("Playerchet"))
            {
                safePushDistance = Mathf.Max(0f, hit.distance - 0.1f);
            }
        }

        Vector3 targetPosition = charTransform.position + (pushDirection * safePushDistance);
        targetPosition.y = charTransform.position.y; // Khóa chặt độ cao

        // --- BẮT ĐẦU DỊCH CHUYỂN ---
        if (cc != null) cc.enabled = false;

        charTransform.position = targetPosition;

        NetworkTransform netTransform = charTransform.GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(targetPosition, charTransform.rotation);
        }

        if (cc != null) cc.enabled = true;
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
