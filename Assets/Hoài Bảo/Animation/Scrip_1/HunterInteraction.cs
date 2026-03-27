using UnityEngine;
using UnityEngine.UI;

public class HunterInteraction : MonoBehaviour
{
    [Header("Giao Diện & UI")]
    public Image interactImage;
    public Slider interactionSlider;

    [Header("Tự Động Tìm UI (Nhập đúng tên ngoài Hierarchy)")]
    public string interactImageName = "Imgtt";
    public string sliderUIName = "Slidertt";

    [Header("Cài Đặt Thời Gian Animation")]
    public float timeDapMay = 2.0f;
    public float timeTreoCUASO = 1.5f;
    public float timeTreoMoc = 3.0f;
    public float timeNhatPlayer = 1.5f;

    [Header("Hệ Thống Vác Người")]
    public Transform handPoint;
    public Transform shoulderPoint;
    public bool isCarryingPlayer = false;
    private GameObject carriedPlayerObject;

    [Header("Vượt Cửa Sổ")]
    public float vaultDistance = 2.5f;

    // =========================================================
    // 🔊 THÊM MỚI: HỆ THỐNG ÂM THANH TƯƠNG TÁC
    // =========================================================
    [Header("Âm Thanh Tương Tác")]
    public AudioSource interactAudioSource; // Cục loa phát âm thanh
    public AudioClip clipDapMay;            // Tiếng rầm! (đạp máy)
    public AudioClip clipTreoCuaso;         // Tiếng sột soạt, hự! (trèo cửa sổ)
    public AudioClip clipTreoMoc;           // Tiếng xoẹt, xích sắt (treo người)

    private Collider currentInteractTarget;
    private float currentDuration = 1f;
    private bool isInteracting = false;
    private bool isSliderRunning = false;
    private float sliderTimer = 0f;
    private bool isVaulting = false;
    private Vector3 vStart, vEnd;

    private Animator animator;
    private CharacterController controller;
    private FPSCamera fpsCameraScript;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        if (Camera.main != null) fpsCameraScript = Camera.main.GetComponent<FPSCamera>();

        // Tự động tìm loa nếu bạn quên kéo thả
        if (interactAudioSource == null) interactAudioSource = GetComponent<AudioSource>();

        // Lần tìm kiếm số 1
        AutoFindUI();
    }

    private void AutoFindUI()
    {
        Debug.Log("🔍 Hunter đang đi lùng sục UI có tên chính xác là: [" + interactImageName + "] và [" + sliderUIName + "]");
        if (interactImage == null)
        {
            GameObject foundInteractObj = FindUIObjectByName(interactImageName);
            if (foundInteractObj != null)
            {
                interactImage = foundInteractObj.GetComponent<Image>();
                Debug.Log("✅ Đã móc thành công: " + interactImageName);
            }
        }

        if (interactionSlider == null)
        {
            GameObject foundSliderObj = FindUIObjectByName(sliderUIName);
            if (foundSliderObj != null)
            {
                interactionSlider = foundSliderObj.GetComponent<Slider>();
                Debug.Log("✅ Đã móc thành công: " + sliderUIName);
            }
        }
    }

    private GameObject FindUIObjectByName(string objName)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name.Trim() == objName.Trim()) return child.gameObject;
            }
        }
        return null;
    }

    private void Update()
    {
        if (isSliderRunning && interactionSlider != null)
        {
            sliderTimer += Time.deltaTime;
            interactionSlider.value = sliderTimer / currentDuration;

            if (sliderTimer >= currentDuration)
            {
                isSliderRunning = false;
                interactionSlider.gameObject.SetActive(false);
            }
        }

        if (isVaulting)
        {
            transform.position = Vector3.Lerp(vStart, vEnd, sliderTimer / currentDuration);
        }
    }

    public bool IsDoingAction()
    {
        return isInteracting || isVaulting;
    }

    public void TryInteract()
    {
        if (isInteracting || currentInteractTarget == null) return;

        string tag = currentInteractTarget.tag;

        if (isCarryingPlayer && tag != "Moc") return;
        if (tag == "Moc" && !isCarryingPlayer) return;

        StartInteraction(tag);
    }

    private void StartInteraction(string tag)
    {
        isInteracting = true;
        sliderTimer = 0f;

        if (interactImage != null) interactImage.gameObject.SetActive(false);

        if (interactionSlider != null)
        {
            interactionSlider.gameObject.SetActive(true);
            interactionSlider.value = 0f;
            isSliderRunning = true;
        }

        Vector3 lookPos = currentInteractTarget.transform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (fpsCameraScript != null)
        {
            fpsCameraScript.isCameraLockedForAnim = true;
            fpsCameraScript.SyncCameraAngles(transform.eulerAngles.y);
        }

        if (tag == "May") { animator.SetTrigger("Dapmay"); currentDuration = timeDapMay; }
        else if (tag == "Moc") { animator.SetTrigger("Treomoc"); currentDuration = timeTreoMoc; }
        else if (tag == "Playerchet")
        {
            animator.SetTrigger("Nhacplayer"); currentDuration = timeNhatPlayer;

            // SỬA Ở ĐÂY: Lấy cục Cha (nguyên cái xác) thay vì cái Trigger vô hình
            if (currentInteractTarget.transform.parent != null)
                carriedPlayerObject = currentInteractTarget.transform.parent.gameObject;
            else
                carriedPlayerObject = currentInteractTarget.gameObject;
        }
        else if (tag == "Cuaso")
        {
            animator.SetTrigger("Treocuaso"); currentDuration = timeTreoCUASO;
            isVaulting = true;
            controller.enabled = false;
            vStart = transform.position;
            vEnd = transform.position + transform.forward * vaultDistance;
        }
    }

    public void AttachPlayerToHand()
    {
        if (carriedPlayerObject != null && handPoint != null)
        {
            // Gọi script trên Nạn nhân và bảo nó: "Bay vào TAY tao đi!"
            PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
            if (receiver != null)
            {
                receiver.GetPickedUpOrHooked(handPoint);
            }

            currentInteractTarget = null;
        }
    }

    public void AttachPlayerToShoulder()
    {
        if (carriedPlayerObject != null && shoulderPoint != null)
        {
            // Gọi script trên Nạn nhân và bảo nó: "Trượt từ tay lên VAI tao đi!"
            PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
            if (receiver != null)
            {
                receiver.GetPickedUpOrHooked(shoulderPoint);
            }

            isCarryingPlayer = true;
        }
    }

    public void HookPlayerToHook()
    {
        if (carriedPlayerObject != null && currentInteractTarget != null)
        {
            Transform hookPoint = currentInteractTarget.transform.Find("HookPoint");
            Transform finalPoint = hookPoint ? hookPoint : currentInteractTarget.transform;

            // Tìm script chịu trận trên người Player
            PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
            if (receiver != null)
            {
                receiver.GetPickedUpOrHooked(finalPoint); // Truyền cái Móc vào
            }

            isCarryingPlayer = false;
            currentInteractTarget.tag = "Untagged"; // Khóa cái móc lại
            carriedPlayerObject = null;
            currentInteractTarget = null;
        }
    }

    public void FinishInteraction()
    {
        isInteracting = false;
        if (fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = false;
        if (isVaulting) { isVaulting = false; controller.enabled = true; }

        isSliderRunning = false;
        if (interactionSlider != null) { interactionSlider.value = 1f; interactionSlider.gameObject.SetActive(false); }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Playerchet") || other.CompareTag("Cuaso"))
        {
            if (isCarryingPlayer && !other.CompareTag("Moc")) return;
            if (other.CompareTag("Moc") && !isCarryingPlayer) return;

            if (interactImage == null || interactionSlider == null)
            {
                AutoFindUI();
                if (interactImage == null) Debug.LogError("❌ Vẫn tàng hình! Kiểm tra lại tên: " + interactImageName);
            }

            currentInteractTarget = other;

            if (interactImage != null) interactImage.gameObject.SetActive(true);
            if (interactionSlider != null) interactionSlider.gameObject.SetActive(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentInteractTarget == other)
        {
            currentInteractTarget = null;
            if (interactImage != null) interactImage.gameObject.SetActive(false);
        }
    }

    // =========================================================
    // 🔊 CÁC HÀM GẮN VÀO ANIMATION EVENT (KHOẢNH KHẮC TÁC ĐỘNG)
    // =========================================================

    // 1. Gắn vào Frame chân đạp mạnh vào máy (Anim Dapmay)
    public void EventDapMay()
    {
        if (currentInteractTarget != null && currentInteractTarget.CompareTag("May"))
        {
            // 🔊 Phát âm thanh Đạp máy
            if (interactAudioSource != null && clipDapMay != null)
            {
                interactAudioSource.PlayOneShot(clipDapMay);
            }

            Debug.Log("💥 Đã đạp máy! Máy bị hỏng.");
            // Ví dụ: currentInteractTarget.GetComponent<Generator>().Break();
        }
    }

    // 2. Gắn vào Frame bắt đầu nhảy lên bệ cửa (Anim Treocuaso)
    public void EventVault()
    {
        if (isVaulting)
        {
            // 🔊 Phát âm thanh Trèo cửa sổ
            if (interactAudioSource != null && clipTreoCuaso != null)
            {
                interactAudioSource.PlayOneShot(clipTreoCuaso);
            }

            Debug.Log("🧗 Hunter đang trèo qua cửa sổ!");
        }
    }

    // 3. Gắn vào Frame tay móc nạn nhân lên móc (Anim Treomoc)
    public void EventTreoMoc()
    {
        if (isCarryingPlayer)
        {
            // 🔊 Phát âm thanh Treo móc (Tiếng xích sắt, v.v...)
            if (interactAudioSource != null && clipTreoMoc != null)
            {
                interactAudioSource.PlayOneShot(clipTreoMoc);
            }

            HookPlayerToHook(); // Gọi hàm xử lý logic treo đã viết ở trên
            Debug.Log("⛓️ Đã treo nạn nhân lên móc thành công!");
        }
    }
}