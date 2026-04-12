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
        if (escapeZone != null) escapeZone.SetActive(false); // Tắt vùng Win đi

        foreach (var light in indicatorLights)
            if (light != null) light.SetActive(false); // Tắt 6 đèn

        // Load sẵn Shader đỏ xuyên tường (Đã có trong project của bạn)
        auraMatRed = Resources.Load<Material>("Mat_AuraRed");
    }

    // Hàm này được gọi từ GameManager khi 4 máy đã xong
    public void OnGatesPoweredUp()
    {
        if (Object.HasStateAuthority) IsPowered = true;

        // Thực hiện logic Aura cho máy khách cục bộ (Local)
        HandleAuraVisibility();
    }
    private void HandleAuraVisibility()
    {
        // 1. Xác định xem máy này là Hunter hay Survivor
        // Cách kiểm tra tùy thuộc vào cách bạn đặt Tag hoặc Script trên Player
        bool isHunter = false;

        // Giả sử Hunter của bạn có tag là "Hunter" hoặc có script HunterInteraction
        var localPlayerObj = Runner.GetPlayerObject(Runner.LocalPlayer);
        if (localPlayerObj != null && localPlayerObj.CompareTag("Hunter"))
        {
            isHunter = true;
        }

        if (isHunter)
        {
            // HUNTER: Thấy vĩnh viễn -> Chỉ cần bật lên
            ApplyAuraToSwitch();
        }
        else
        {
            // SURVIVOR: Chỉ thấy 10 giây
            ApplyAuraToSwitch();
            // Sau 10 giây gọi hàm xóa Aura
            Invoke(nameof(RemoveAuraFromSwitch), 10f);
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

        // Tắt bức tường chặn
        if (gateBlocker != null) gateBlocker.SetActive(false);

        // Bật vùng Win
        if (escapeZone != null) escapeZone.SetActive(true);

        // Phát tiếng cạch mở cửa
        if (gateAudioSource && openedSound)
        {
            gateAudioSource.Stop();
            gateAudioSource.PlayOneShot(openedSound);
        }

        // Tắt lớp Aura đỏ của Cầu dao đi vì cửa đã mở rồi
        RemoveAuraFromSwitch();
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

    // ===============================================
    // Hệ Thống Aura Đỏ (Dùng chung cách của Hunter)
    // ===============================================
    private void ApplyAuraToSwitch()
    {
        if (switchBox == null || auraMatRed == null) return;
        Renderer[] renderers = switchBox.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            bool hasAura = false;
            foreach (Material m in mats) { if (m.name.Contains(auraMatRed.name)) hasAura = true; }
            if (!hasAura)
            {
                Material[] newMats = new Material[mats.Length + 1];
                for (int i = 0; i < mats.Length; i++) newMats[i] = mats[i];
                newMats[mats.Length] = auraMatRed;
                r.materials = newMats;
            }
        }
    }

    private void RemoveAuraFromSwitch()
    {
        if (switchBox == null || auraMatRed == null) return;
        Renderer[] renderers = switchBox.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            System.Collections.Generic.List<Material> cleanedMats = new System.Collections.Generic.List<Material>();
            foreach (Material m in mats) { if (!m.name.Contains(auraMatRed.name)) cleanedMats.Add(m); }
            r.materials = cleanedMats.ToArray();
        }
    }
}