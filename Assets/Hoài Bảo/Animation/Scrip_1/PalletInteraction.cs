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

    // 🚨 HÀM TÍNH TOÁN VÀ ĐẨY NHÂN VẬT ĐÃ ĐƯỢC NÂNG CẤP 🚨
    private void PushCharacterOut(Transform charTransform)
    {
        CharacterController cc = charTransform.GetComponent<CharacterController>();

        // Quy đổi tọa độ để biết nhân vật đứng trước hay sau ván
        Vector3 localPos = stunZone.transform.InverseTransformPoint(charTransform.position);
        Vector3 pushDirection = Vector3.zero;

        if (localPos.z <= 0)
        {
            pushDirection = -stunZone.transform.forward; // Đẩy lùi
        }
        else
        {
            pushDirection = stunZone.transform.forward;  // Đẩy tới
        }

        // ==================================================
        // 🚨 SỬA LỖI 1: KHÔNG BAY LÊN TRỜI HOẶC CHUI XUỐNG ĐẤT
        // ==================================================
        // Triệt tiêu hoàn toàn độ nghiêng trục Y của ván, ép đẩy theo chiều ngang song song mặt đất
        pushDirection.y = 0;
        pushDirection.Normalize();

        // ==================================================
        // 🚨 SỬA LỖI 2: KHÔNG BỊ XUYÊN TƯỜNG (RAYCAST CHECK)
        // ==================================================
        float safePushDistance = pushOutDistance;

        // Bắn một khối cầu ảo (SphereCast) từ ngang ngực nhân vật (cao 1m) về hướng bị đẩy
        Vector3 castOrigin = charTransform.position + Vector3.up * 1.0f;
        float characterRadius = 0.3f; // Bán kính giả định của cơ thể nhân vật

        // Nếu khối cầu đụng trúng cái gì đó trong phạm vi đẩy...
        if (Physics.SphereCast(castOrigin, characterRadius, pushDirection, out RaycastHit hit, pushOutDistance))
        {
            // Bỏ qua nếu cái đụng trúng chỉ là vùng Trigger, hoặc là người chơi khác
            if (!hit.collider.isTrigger &&
                !hit.collider.CompareTag("Player") &&
                !hit.collider.CompareTag("Hunter") &&
                !hit.collider.CompareTag("Playerchet"))
            {
                // Ngay lập tức cắt ngắn khoảng cách đẩy lại để không chui vào tường
                // Trừ đi 0.1f để nhân vật đứng cách vách tường một khoảng mỏng, không bị kẹt
                safePushDistance = Mathf.Max(0f, hit.distance - 0.1f);
            }
        }

        // Tính điểm đến an toàn
        Vector3 targetPosition = charTransform.position + (pushDirection * safePushDistance);

        // Ép cứng độ cao Y của điểm đến bằng đúng độ cao Y ban đầu của nhân vật
        targetPosition.y = charTransform.position.y;

        // --- BẮT ĐẦU DỊCH CHUYỂN ---
        // Tắt CC để không bị giật vật lý
        if (cc != null) cc.enabled = false;

        charTransform.position = targetPosition;

        // Đồng bộ mạng
        NetworkTransform netTransform = charTransform.GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(targetPosition, charTransform.rotation);
        }

        // Bật lại CC
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
