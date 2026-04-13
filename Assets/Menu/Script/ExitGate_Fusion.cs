using UnityEngine;
using UnityEngine.UI;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
public class ExitGate_Fusion : NetworkBehaviour
{
    [Header("Cài Đặt Cửa")]
    public float timeToOpen = 60f; // Tổng thời gian mở cửa (60s)
    public float interactDistance = 3.5f; // Đứng xa quá không mở được

    [Header("UI & Tương Tác (Local)")]
    [Tooltip("Text 'Giữ E để mở' - Phải nằm trong Canvas của riêng Prefab này")]
    public GameObject interactText; 
    [Tooltip("Thanh tiến trình - Phải nằm trong Canvas của riêng Prefab này")]
    public Slider progressBar;

    [Header("Object Cửa (Kéo thả vào đây)")]
    public GameObject switchBox; // Cục cầu dao để đổi màu Aura Đỏ
    public GameObject gateBlocker; // Bức tường chặn cửa, sẽ bị tắt khi cửa mở
    public GameObject escapeZone;  // Vùng đi vào là Win (Ban đầu nên tắt)
    public GameObject[] indicatorLights = new GameObject[6]; // 6 cái Đèn (Object ánh sáng)
    
    [Header("Hệ thống Aura Đỏ")]
    public GameObject auraSwitchObject;

    [Header("Âm thanh")]
    public AudioSource gateAudioSource; // Chú ý: Component này phải set Spatial Blend = 1 (3D)
    public AudioClip globalPowerUpSound; // Âm thanh KHI SỬA XONG 4 MÁY (Toàn bản đồ nghe thấy)
    public AudioClip openingSound; // Tiếng cọt kẹt lúc đang mở (Local 3D)
    public AudioClip openedSound;  // Tiếng "Cạch" cửa mở toang (Local 3D)

    // ================== BIẾN ĐỒNG BỘ MẠNG ==================
    [Networked] public NetworkBool IsPowered { get; set; } // Đã sửa xong 4 máy chưa?
    [Networked] public NetworkBool IsOpened { get; set; }  // Cửa đã mở toang chưa?
    [Networked] public float Progress { get; set; }        // Thanh tiến trình (Lưu lại)
    [Networked, Capacity(4)] public NetworkLinkedList<NetworkId> ActiveOpeners => default;

    // ================== BIẾN CỤC BỘ ==================
    private bool _inRange = false;
    private ISurvivor _localPlayer;
    private ChangeDetector _changeDetector;
    private bool _isOpeningLocally = false;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (interactText != null) interactText.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (escapeZone != null) escapeZone.SetActive(false);

        foreach (var light in indicatorLights)
            if (light != null) light.SetActive(false);

        if (auraSwitchObject != null) auraSwitchObject.SetActive(false);
    }

    // Hàm này được gọi từ GameManager khi 4 máy đã xong
    public void OnGatesPoweredUp()
    {
        if (Object.HasStateAuthority) IsPowered = true;

        // --- FIX: PHÁT ÂM THANH TOÀN BẢN ĐỒ ---
        if (globalPowerUpSound != null)
        {
            // Phát âm thanh tại vị trí Camera chính để giả lập âm thanh 2D, ai cũng sẽ nghe rõ 100%
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(globalPowerUpSound, playPos, GetVFXVolume());
        }

        HandleAuraVisibility();
    }

    private void HandleAuraVisibility()
    {
        if (auraSwitchObject == null) return;

        bool isHunter = false;
        NetworkObject[] allNetObjects = FindObjectsOfType<NetworkObject>();

        foreach (var networkObj in allNetObjects)
        {
            if (networkObj.HasInputAuthority)
            {
                if (networkObj.CompareTag("Hunter") || networkObj.GetComponent<HunterInteraction>() != null)
                {
                    isHunter = true;
                    break;
                }
            }
        }

        if (isHunter)
        {
            auraSwitchObject.SetActive(true);
        }
        else
        {
            auraSwitchObject.SetActive(true);
            Invoke(nameof(HideAuraSwitch), 10f);
        }
    }

    public override void Render()
    {
        // --- FIX: BẮT SỰ KIỆN CỬA MỞ TỪ SERVER ĐỂ GỌI OPENGATEVISUALS ---
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsOpened):
                    if (IsOpened) OpenGateVisuals();
                    break;
            }
        }

        // 1. Đồng bộ 6 cái đèn theo tiến trình (Cứ 10s sáng 1 đèn riêng cho Prefab này)
        int lightsToTurnOn = Mathf.FloorToInt(Progress / 10f); 
        for (int i = 0; i < indicatorLights.Length; i++)
        {
            if (indicatorLights[i] != null)
            {
                indicatorLights[i].SetActive(i < lightsToTurnOn);
            }
        }

        // 2. Cập nhật thanh Slider
        if (progressBar != null && progressBar.gameObject.activeSelf)
        {
            progressBar.value = Progress / timeToOpen;
        }

        // 3. Xử lý âm thanh mở cửa (Từng Prefab tự xử lý, ai đứng gần mới nghe)
        bool isSomeoneOpening = ActiveOpeners.Count > 0;
        if (gateAudioSource != null && openingSound != null)
        {
            if (isSomeoneOpening && !IsOpened)
            {
                if (!gateAudioSource.isPlaying || gateAudioSource.clip != openingSound)
                {
                    gateAudioSource.clip = openingSound;
                    gateAudioSource.loop = true;
                    gateAudioSource.volume = GetVFXVolume();
                    gateAudioSource.Play();
                }
                else
                {
                    gateAudioSource.volume = GetVFXVolume(); // Cập nhật theo Setting
                }
            }
            else if ((!isSomeoneOpening || IsOpened) && gateAudioSource.clip == openingSound && gateAudioSource.isPlaying)
            {
                gateAudioSource.Stop();
            }
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid || !IsPowered || IsOpened) return;

        // Failsafe: Đi quá xa tự động ngắt
        if (_localPlayer != null)
        {
            float dist = Vector3.Distance(switchBox.transform.position, _localPlayer.Object.transform.position);
            if (dist > interactDistance)
            {
                ResetInteractionLocal();
            }
        }

        if (_inRange)
        {
            // BẮT ĐẦU GIỮ PHÍM E
            if (Input.GetKeyDown(KeyCode.E) && !_isOpeningLocally)
            {
                _isOpeningLocally = true;
                if (interactText != null) interactText.SetActive(false);
                if (progressBar != null) progressBar.gameObject.SetActive(true);
                StartOpeningLocally();
            }

            // BUÔNG PHÍM E
            if (Input.GetKeyUp(KeyCode.E) && _isOpeningLocally)
            {
                _isOpeningLocally = false;
                if (interactText != null) interactText.SetActive(true);
                if (progressBar != null) progressBar.gameObject.SetActive(false);
                StopOpeningLocally();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !IsPowered || IsOpened) return;

        if (ActiveOpeners.Count > 0)
        {
            // Tiến trình tăng dần. x2, x3 nếu nhiều người cùng mở
            Progress += Runner.DeltaTime * ActiveOpeners.Count;

            if (Progress >= timeToOpen)
            {
                Progress = timeToOpen;
                IsOpened = true; 
                ActiveOpeners.Clear(); 
            }
        }
    }

    private void StartOpeningLocally()
    {
        if (_localPlayer == null) return;
        RPC_SetOpeningState(_localPlayer.Object.Id, true);
    }

    private void StopOpeningLocally()
    {
        if (_localPlayer == null) return;
        RPC_SetOpeningState(_localPlayer.Object.Id, false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetOpeningState(NetworkId playerId, NetworkBool isOpening)
    {
        if (IsOpened) return;

        if (isOpening)
        {
            if (!ActiveOpeners.Contains(playerId)) ActiveOpeners.Add(playerId);
        }
        else
        {
            ActiveOpeners.Remove(playerId);
        }
    }

    private void OpenGateVisuals()
    {
        // Khi cửa mở toang, dọn dẹp sạch UI
        _isOpeningLocally = false;
        if (interactText) interactText.SetActive(false);
        if (progressBar) progressBar.gameObject.SetActive(false);
        if (gateBlocker != null) gateBlocker.SetActive(false);
        if (escapeZone != null) escapeZone.SetActive(true);

        if (gateAudioSource && openedSound)
        {
            gateAudioSource.Stop();
            gateAudioSource.PlayOneShot(openedSound, GetVFXVolume());
        }

        HideAuraSwitch();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsOpened || !IsPowered) return;

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<ISurvivor>();
            if (player != null && player.Object.HasInputAuthority)
            {
                _inRange = true;
                _localPlayer = player;
                _isOpeningLocally = false;

                if (interactText != null) interactText.SetActive(true);
                if (progressBar != null) progressBar.gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<ISurvivor>();
            if (player != null && player.Object.HasInputAuthority)
            {
                ResetInteractionLocal();
            }
        }
    }

    private void ResetInteractionLocal()
    {
        _inRange = false;

        if (_isOpeningLocally) StopOpeningLocally();

        _isOpeningLocally = false;
        _localPlayer = null;

        if (interactText != null) interactText.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }

    private void HideAuraSwitch()
    {
        if (auraSwitchObject != null) auraSwitchObject.SetActive(false);
    }

    private float GetVFXVolume()
    {
        return AudioManager.Instance != null ? AudioManager.Instance.vfxVolume : 1f;
    }
}