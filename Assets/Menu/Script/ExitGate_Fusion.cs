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
    public GameObject interactText; // Chữ "Giữ E để mở"
    public Slider progressBar;

    [Header("Object Cửa (Kéo thả vào đây)")]
    public GameObject switchBox; // Cục cầu dao để đổi màu Aura Đỏ
    public GameObject gateBlocker; // Bức tường chặn cửa, sẽ bị tắt khi cửa mở
    public GameObject escapeZone;  // Vùng đi vào là Win (Ban đầu nên tắt)
    public GameObject[] indicatorLights = new GameObject[6]; // 6 cái Đèn (Object ánh sáng)
    [Header("Hệ thống Aura Đỏ")]
    public GameObject auraSwitchObject;

    [Header("Âm thanh")]
    public AudioSource gateAudioSource;
    public AudioClip openingSound; // Tiếng cọt kẹt lúc đang mở
    public AudioClip openedSound;  // Tiếng "Cạch" cửa mở toang

    // ================== BIẾN ĐỒNG BỘ MẠNG ==================
    [Networked] public NetworkBool IsPowered { get; set; } // Đã sửa xong 4 máy chưa?
    [Networked] public NetworkBool IsOpened { get; set; }  // Cửa đã mở toang chưa?
    [Networked] public float Progress { get; set; }        // Thanh tiến trình (Lưu lại)
    [Networked, Capacity(4)] public NetworkLinkedList<NetworkId> ActiveOpeners => default;

    // ================== BIẾN CỤC BỘ ==================
    private bool _inRange = false;
    private ISurvivor _localPlayer;
    private ChangeDetector _changeDetector;
    private Material auraMatRed;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (interactText != null) interactText.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (escapeZone != null) escapeZone.SetActive(false);

        foreach (var light in indicatorLights)
            if (light != null) light.SetActive(false);

        // Đảm bảo lúc mới vào game là cục đỏ lè bị tắt
        if (auraSwitchObject != null) auraSwitchObject.SetActive(false);
    }
    // Hàm này được gọi từ GameManager khi 4 máy đã xong
    public void OnGatesPoweredUp()
    {
        if (Object.HasStateAuthority) IsPowered = true;

        // Kích hoạt việc hiển thị cục đỏ
        HandleAuraVisibility();
    }
    private void HandleAuraVisibility()
    {
        if (auraSwitchObject == null) return;

        bool isHunter = false;

        // 🚨 TÌM XEM MÁY NÀY LÀ HUNTER HAY SURVIVOR
        // Giả sử Hunter của bạn có Tag là "Hunter" (nhớ set đúng Tag ngoài Unity nhé)
        var localPlayerObj = Runner.GetPlayerObject(Runner.LocalPlayer);
        if (localPlayerObj != null && localPlayerObj.CompareTag("Hunter"))
        {
            isHunter = true;
        }

        if (isHunter)
        {
            // HUNTER: Bật cục đỏ lè lên và để đó luôn
            auraSwitchObject.SetActive(true);
        }
        else
        {
            // SURVIVOR: Bật lên, sau đó đếm ngược 10 giây rồi gọi hàm tắt đi
            auraSwitchObject.SetActive(true);
            Invoke(nameof(HideAuraSwitch), 10f);
        }
    }

    public override void Render()
    {
        // 1. Đồng bộ 6 cái đèn theo tiến trình (Cứ 10s sáng 1 đèn)
        int lightsToTurnOn = Mathf.FloorToInt(Progress / 10f); // Tối đa 6 đèn
        for (int i = 0; i < indicatorLights.Length; i++)
        {
            if (indicatorLights[i] != null)
            {
                indicatorLights[i].SetActive(i < lightsToTurnOn);
            }
        }

        // 2. Cập nhật Slider UI cho người đang đứng gần
        if (progressBar != null && (_inRange || ActiveOpeners.Contains(_localPlayer?.Object.Id ?? default)))
        {
            progressBar.value = Progress / timeToOpen;
        }

        // 3. Xử lý âm thanh mở cửa
        bool isSomeoneOpening = ActiveOpeners.Count > 0;
        if (gateAudioSource != null && openingSound != null)
        {
            if (isSomeoneOpening && !IsOpened && !gateAudioSource.isPlaying)
            {
                gateAudioSource.clip = openingSound;
                gateAudioSource.loop = true;
                gateAudioSource.Play();
            }
            else if ((!isSomeoneOpening || IsOpened) && gateAudioSource.clip == openingSound && gateAudioSource.isPlaying)
            {
                gateAudioSource.Stop();
            }
        }

        // 4. Bắt sự kiện khi cửa chính thức mở
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(IsOpened) && IsOpened)
            {
                OpenGateVisuals();
            }
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid || !IsPowered || IsOpened) return;

        // Failsafe: Nếu người chơi đi quá xa thì tự ngắt mở cửa (Tránh ấn E từ xa)
        if (_localPlayer != null)
        {
            float dist = Vector3.Distance(switchBox.transform.position, _localPlayer.Object.transform.position);
            if (dist > interactDistance)
            {
                if (ActiveOpeners.Contains(_localPlayer.Object.Id)) StopOpeningLocally();
                _inRange = false;
                _localPlayer = null;
                if (interactText) interactText.SetActive(false);
                if (progressBar) progressBar.gameObject.SetActive(false);
            }
        }

        if (_inRange)
        {
            bool amIOpening = ActiveOpeners.Contains(_localPlayer.Object.Id);

            // Ẩn Text "Giữ E" nếu mình đang kéo thanh rồi
            if (interactText != null) interactText.SetActive(!amIOpening);

            // Xử lý Input: GIỮ nút E
            if (Input.GetKeyDown(KeyCode.E)) StartOpeningLocally();
            if (Input.GetKeyUp(KeyCode.E) && amIOpening) StopOpeningLocally();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !IsPowered || IsOpened) return;

        if (ActiveOpeners.Count > 0)
        {
            // Tiến trình tăng dần. Nếu có nhiều người cùng mở thì sẽ x2, x3 tốc độ.
            Progress += Runner.DeltaTime * ActiveOpeners.Count;

            if (Progress >= timeToOpen)
            {
                Progress = timeToOpen;
                IsOpened = true; // Kích hoạt mở cửa toàn bản đồ
                ActiveOpeners.Clear(); // Kick mọi người ra khỏi trạng thái tương tác
            }
        }
    }

    // ===============================================
    // Lệnh tương tác Local -> Báo lên Server
    // ===============================================
    private void StartOpeningLocally()
    {
        if (_localPlayer == null) return;
        if (progressBar) progressBar.gameObject.SetActive(true);
        RPC_SetOpeningState(_localPlayer.Object.Id, true);
    }

    private void StopOpeningLocally()
    {
        if (_localPlayer == null) return;
        RPC_SetOpeningState(_localPlayer.Object.Id, false);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
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

        // Tương tự Generator: bạn có thể gọi SetRepairAnimation(isOpening) cho nhân vật ở đây nếu muốn họ có Anim gạt cầu dao
    }

    // ===============================================
    // Visual & Trigger Zones
    // ===============================================
    private void OpenGateVisuals()
    {
        if (interactText) interactText.SetActive(false);
        if (progressBar) progressBar.gameObject.SetActive(false);
        if (gateBlocker != null) gateBlocker.SetActive(false);
        if (escapeZone != null) escapeZone.SetActive(true);

        if (gateAudioSource && openedSound)
        {
            gateAudioSource.Stop();
            gateAudioSource.PlayOneShot(openedSound);
        }

        // Cửa đã mở rồi thì tắt luôn cục đỏ lè đi (nếu Hunter đến gần) cho sạch màn hình
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
                if (interactText != null) interactText.SetActive(true);
                if (progressBar != null) progressBar.gameObject.SetActive(true);
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
                _inRange = false;
                if (ActiveOpeners.Contains(player.Object.Id)) StopOpeningLocally();
                _localPlayer = null;
                if (interactText != null) interactText.SetActive(false);
            }
        }
    }
    private void HideAuraSwitch()
    {
        // Hàm dùng để tắt cục đỏ lè
        if (auraSwitchObject != null) auraSwitchObject.SetActive(false);
    }
}