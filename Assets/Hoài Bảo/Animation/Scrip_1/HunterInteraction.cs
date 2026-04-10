// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections.Generic;

// public class HunterInteraction : MonoBehaviour
// {
//     [Header("Giao Diện & UI")]
//     public Image interactImage;
//     public Slider interactionSlider;

//     [Header("Tự Động Tìm UI (Nhập đúng tên ngoài Hierarchy)")]
//     public string interactImageName = "Imgtt";
//     public string sliderUIName = "Slidertt";

//     [Header("Cài Đặt Thời Gian Animation")]
//     public float timeDapMay = 2.0f;
//     public float timeTreoCUASO = 1.5f;
//     public float timeTreoMoc = 3.0f;
//     public float timeNhatPlayer = 1.5f;

//     [Header("Hệ Thống Vác Người")]
//     public Transform handPoint;
//     public Transform shoulderPoint;
//     public bool isCarryingPlayer = false;
//     private GameObject carriedPlayerObject;

//     [Header("Vượt Cửa Sổ")]
//     public float vaultDistance = 2.5f;

//     [Header("Âm Thanh Tương Tác")]
//     public AudioSource interactAudioSource;
//     public AudioClip clipDapMay;
//     public AudioClip clipTreoCuaso;
//     public AudioClip clipTreoMoc;

//     // =========================================================
//     // 🚨 HỆ THỐNG AURA XUYÊN TƯỜNG (TỰ ĐỘNG TÌM MATERIAL)
//     // =========================================================
//     [Header("Hệ Thống Aura (Tự Động)")]
//     private Material auraMatRed;   // Dành cho Móc
//     private Material auraMatWhite; // Dành cho Máy
//     private GameObject[] allHooks;
//     private GameObject[] allGenerators;

//     private Collider currentInteractTarget;
//     private float currentDuration = 1f;
//     private bool isInteracting = false;
//     private bool isSliderRunning = false;
//     private float sliderTimer = 0f;
//     private bool isVaulting = false;
//     private Vector3 vStart, vEnd;

//     private Animator animator;
//     private CharacterController controller;
//     private FPSCamera fpsCameraScript;

//     private void Awake()
//     {
//         animator = GetComponent<Animator>();
//         controller = GetComponent<CharacterController>();
//         if (Camera.main != null) fpsCameraScript = Camera.main.GetComponent<FPSCamera>();
//         if (interactAudioSource == null) interactAudioSource = GetComponent<AudioSource>();

//         AutoFindUI();

//         // 🚨 TỰ ĐỘNG LOAD MATERIAL TỪ THƯ MỤC "Resources"
//         // Đảm bảo bạn đã bỏ 2 file tên Mat_AuraRed và Mat_AuraWhite vào thư mục Resources!
//         auraMatRed = Resources.Load<Material>("Mat_AuraRed");
//         auraMatWhite = Resources.Load<Material>("Mat_AuraWhite");

//         if (auraMatRed == null) Debug.LogError("❌ KHÔNG TÌM THẤY 'Mat_AuraRed' trong thư mục Resources!");
//         if (auraMatWhite == null) Debug.LogError("❌ KHÔNG TÌM THẤY 'Mat_AuraWhite' trong thư mục Resources!");
//     }

//     private void Start()
//     {
//         // 1. Quét tìm toàn bộ Máy và Móc trên bản đồ
//         allHooks = GameObject.FindGameObjectsWithTag("Moc");
//         allGenerators = GameObject.FindGameObjectsWithTag("May");

//         // 2. Lúc bình thường: Ép Máy phát điện (May) hiện Aura ĐỎ
//         ToggleAuraGroup(allGenerators, auraMatRed, true);
//     }

//     // --- CÁC HÀM UI CŨ GIỮ NGUYÊN ---
//     private void AutoFindUI()
//     {
//         if (interactImage == null)
//         {
//             GameObject foundInteractObj = FindUIObjectByName(interactImageName);
//             if (foundInteractObj != null) interactImage = foundInteractObj.GetComponent<Image>();
//         }

//         if (interactionSlider == null)
//         {
//             GameObject foundSliderObj = FindUIObjectByName(sliderUIName);
//             if (foundSliderObj != null) interactionSlider = foundSliderObj.GetComponent<Slider>();
//         }
//     }

//     private GameObject FindUIObjectByName(string objName)
//     {
//         Canvas[] canvases = FindObjectsOfType<Canvas>(true);
//         foreach (Canvas canvas in canvases)
//         {
//             Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
//             foreach (Transform child in children)
//                 if (child.name.Trim() == objName.Trim()) return child.gameObject;
//         }
//         return null;
//     }

//     private void Update()
//     {
//         if (isSliderRunning && interactionSlider != null)
//         {
//             sliderTimer += Time.deltaTime;
//             interactionSlider.value = sliderTimer / currentDuration;

//             if (sliderTimer >= currentDuration)
//             {
//                 isSliderRunning = false;
//                 interactionSlider.gameObject.SetActive(false);
//             }
//         }

//         if (isVaulting)
//         {
//             transform.position = Vector3.Lerp(vStart, vEnd, sliderTimer / currentDuration);
//         }
//     }

//     public bool IsDoingAction() { return isInteracting || isVaulting; }

//     public void TryInteract()
//     {
//         if (isInteracting || currentInteractTarget == null) return;
//         string tag = currentInteractTarget.tag;
//         if (isCarryingPlayer && tag != "Moc") return;
//         if (tag == "Moc" && !isCarryingPlayer) return;
//         StartInteraction(tag);
//     }

//     private void StartInteraction(string tag)
//     {
//         isInteracting = true;
//         sliderTimer = 0f;

//         if (interactImage != null) interactImage.gameObject.SetActive(false);
//         if (interactionSlider != null) { interactionSlider.gameObject.SetActive(true); interactionSlider.value = 0f; isSliderRunning = true; }

//         Vector3 lookPos = currentInteractTarget.transform.position;
//         lookPos.y = transform.position.y;
//         transform.LookAt(lookPos);

//         if (fpsCameraScript != null) { fpsCameraScript.isCameraLockedForAnim = true; fpsCameraScript.SyncCameraAngles(transform.eulerAngles.y); }

//         if (tag == "May") { animator.SetTrigger("Dapmay"); currentDuration = timeDapMay; }
//         else if (tag == "Moc") { animator.SetTrigger("Treomoc"); currentDuration = timeTreoMoc; }
//         else if (tag == "Playerchet")
//         {
//             animator.SetTrigger("Nhacplayer"); currentDuration = timeNhatPlayer;
//             if (currentInteractTarget.transform.parent != null) carriedPlayerObject = currentInteractTarget.transform.parent.gameObject;
//             else carriedPlayerObject = currentInteractTarget.gameObject;
//         }
//         else if (tag == "Cuaso")
//         {
//             animator.SetTrigger("Treocuaso"); currentDuration = timeTreoCUASO;
//             isVaulting = true; controller.enabled = false;
//             vStart = transform.position; vEnd = transform.position + transform.forward * vaultDistance;
//         }
//     }

//     public void AttachPlayerToHand()
//     {
//         if (carriedPlayerObject != null && handPoint != null)
//         {
//             Transform interactionTrigger = carriedPlayerObject.transform.Find("Playerchet");
//             if (interactionTrigger == null) interactionTrigger = FindChildWithTag(carriedPlayerObject, "Playerchet");
//             if (interactionTrigger != null) interactionTrigger.gameObject.SetActive(false);

//             PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
//             if (receiver != null) receiver.GetPickedUpOrHooked(handPoint);

//             if (interactImage != null) interactImage.gameObject.SetActive(false);
//             currentInteractTarget = null;
//         }
//     }

//     public void AttachPlayerToShoulder()
//     {
//         if (carriedPlayerObject != null && shoulderPoint != null)
//         {
//             PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
//             if (receiver != null) receiver.GetPickedUpOrHooked(shoulderPoint);

//             isCarryingPlayer = true;

//             // 🚨 LOGIC ĐỔI ƯU TIÊN AURA:
//             // Tắt màu ĐỎ của Máy, bật màu TRẮNG cho Máy
//             ToggleAuraGroup(allGenerators, auraMatRed, false);
//             ToggleAuraGroup(allGenerators, auraMatWhite, true);

//             // Bật màu ĐỎ cho Móc
//             ToggleAuraGroup(allHooks, auraMatRed, true);
//         }
//     }

//     public void HookPlayerToHook()
//     {
//         if (carriedPlayerObject != null && currentInteractTarget != null)
//         {
//             Transform hookPoint = currentInteractTarget.transform.Find("HookPoint");
//             Transform finalPoint = hookPoint ? hookPoint : currentInteractTarget.transform;

//             PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
//             if (receiver != null) receiver.GetPickedUpOrHooked(finalPoint);

//             currentInteractTarget.tag = "Untagged";
//             if (interactImage != null) interactImage.gameObject.SetActive(false);

//             isCarryingPlayer = false;
//             carriedPlayerObject = null;
//             currentInteractTarget = null;

//             // 🚨 TRẢ LẠI TRẠNG THÁI BÌNH THƯỜNG:
//             // Tắt màu Trắng của Máy, bật lại màu Đỏ cho Máy
//             ToggleAuraGroup(allGenerators, auraMatWhite, false);
//             ToggleAuraGroup(allGenerators, auraMatRed, true);

//             // Tắt màu Đỏ của Móc
//             ToggleAuraGroup(allHooks, auraMatRed, false);
//         }
//     }

//     private Transform FindChildWithTag(GameObject parent, string tag)
//     {
//         foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
//             if (child.CompareTag(tag)) return child;
//         return null;
//     }

//     public void FinishInteraction()
//     {
//         isInteracting = false;
//         if (fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = false;
//         if (isVaulting) { isVaulting = false; controller.enabled = true; }

//         isSliderRunning = false;
//         if (interactionSlider != null) { interactionSlider.value = 1f; interactionSlider.gameObject.SetActive(false); }
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (currentInteractTarget == other)
//         {
//             currentInteractTarget = null;
//             if (interactImage != null) interactImage.gameObject.SetActive(false);
//         }
//     }

//     // =========================================================
//     // HÀM XỬ LÝ LỚP MÀU AURA (CÓ TRUYỀN BIẾN MATERIAL VÀO)
//     // =========================================================
//     private void ToggleAuraGroup(GameObject[] objects, Material targetMat, bool turnOn)
//     {
//         if (targetMat == null) return;

//         foreach (GameObject obj in objects)
//         {
//             if (obj == null) continue;

//             Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
//             foreach (Renderer r in renderers)
//             {
//                 if (r is ParticleSystemRenderer) continue;

//                 Material[] currentMats = r.materials;
//                 bool hasAura = false;

//                 // Quét xem cái renderer này đã có cái material mục tiêu chưa?
//                 foreach (Material m in currentMats) { if (m.name.Contains(targetMat.name)) hasAura = true; }

//                 if (turnOn && !hasAura)
//                 {
//                     // Mặc thêm áo
//                     Material[] newMats = new Material[currentMats.Length + 1];
//                     for (int i = 0; i < currentMats.Length; i++) newMats[i] = currentMats[i];
//                     newMats[currentMats.Length] = targetMat;
//                     r.materials = newMats;
//                 }
//                 else if (!turnOn && hasAura)
//                 {
//                     // Cởi áo ra
//                     List<Material> cleanedMats = new List<Material>();
//                     foreach (Material m in currentMats) { if (!m.name.Contains(targetMat.name)) cleanedMats.Add(m); }
//                     r.materials = cleanedMats.ToArray();
//                 }
//             }
//         }
//     }


//     // --- ANIMATION EVENTS ---
//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Playerchet") || other.CompareTag("Cuaso"))
//         {
//             if (isCarryingPlayer && !other.CompareTag("Moc")) return;
//             if (other.CompareTag("Moc") && !isCarryingPlayer) return;

//             // 🚨 KIỂM TRA ĐIỀU KIỆN ĐẠP MÁY TRƯỚC KHI HIỆN UI
//             if (other.CompareTag("May"))
//             {
//                 Generator gen = other.GetComponent<Generator>();
//                 // Nếu máy không thể đạp được nữa (chưa ai sửa, hoặc đã đạp rồi) -> KHÔNG HIỆN UI
//                 if (gen == null || !gen.CanBeDamagedByHunter()) return;
//             }

//             if (interactImage == null || interactionSlider == null) AutoFindUI();
//             currentInteractTarget = other;

//             if (interactImage != null) interactImage.gameObject.SetActive(true);
//             if (interactionSlider != null) interactionSlider.gameObject.SetActive(false);
//         }
//     }

//     // --- ANIMATION EVENTS ---
//     public void EventDapMay()
//     {
//         if (currentInteractTarget != null && currentInteractTarget.CompareTag("May"))
//         {
//             // 🔊 Phát âm thanh
//             if (interactAudioSource != null && clipDapMay != null) interactAudioSource.PlayOneShot(clipDapMay);

//             // 🚨 GỌI HÀM TRỪ MÁU MÁY PHÁT ĐIỆN
//             Generator gen = currentInteractTarget.GetComponent<Generator>();
//             if (gen != null)
//             {
//                 gen.DamageByHunter();
//             }
//         }
//     }
//     public void EventVault() { if (isVaulting) { if (interactAudioSource != null && clipTreoCuaso != null) interactAudioSource.PlayOneShot(clipTreoCuaso); } }
//     public void EventTreoMoc() { if (isCarryingPlayer) { if (interactAudioSource != null && clipTreoMoc != null) interactAudioSource.PlayOneShot(clipTreoMoc); HookPlayerToHook(); } }
// }
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Fusion;

public class HunterInteraction : NetworkBehaviour
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
    [Networked] public NetworkBool isCarryingPlayer { get; set; }
    private GameObject carriedPlayerObject;

    [Header("Vượt Cửa Sổ")]
    public float vaultDistance = 2.5f;

    [Header("Âm Thanh Tương Tác")]
    public AudioSource interactAudioSource;
    public AudioClip clipDapMay;
    public AudioClip clipTreoCuaso;
    public AudioClip clipTreoMoc;

    [Header("Hệ Thống Aura (Tự Động)")]
    private Material auraMatRed;
    private Material auraMatWhite;
    private GameObject[] allHooks;
    private GameObject[] allGenerators;

    private Collider currentInteractTarget;
    private float currentDuration = 1f;

    // --- BIẾN ĐỒNG BỘ MẠNG ---
    [Networked] private NetworkBool isInteracting { get; set; }
    [Networked] private NetworkBool isVaulting { get; set; }
    [Networked] private Vector3 syncedTargetPos { get; set; }
    [Networked] private Quaternion syncedTargetRot { get; set; }

    // Tách riêng timer để không bị đụng chạm giữa UI và Vật Lý
    private bool isSliderRunning = false;
    private float sliderTimer = 0f;
    private float vaultTimer = 0f;
    private Vector3 vStart, vEnd;

    private Animator animator;
    private CharacterController controller;
    private FPSCamera fpsCameraScript;
    private NetworkId syncedTargetId;

    public override void Spawned()
    {
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
        if (Camera.main != null && Object.HasInputAuthority) fpsCameraScript = Camera.main.GetComponent<FPSCamera>();
        if (interactAudioSource == null) interactAudioSource = GetComponent<AudioSource>();

        if (Object.HasInputAuthority) AutoFindUI();

        auraMatRed = Resources.Load<Material>("Mat_AuraRed");
        auraMatWhite = Resources.Load<Material>("Mat_AuraWhite");

        if (Object.HasInputAuthority)
        {
            allHooks = GameObject.FindGameObjectsWithTag("Moc");
            allGenerators = GameObject.FindGameObjectsWithTag("May");
            ToggleAuraGroup(allGenerators, auraMatRed, true);
        }
    }

    private void AutoFindUI()
    {
        if (interactImage == null)
        {
            GameObject foundInteractObj = FindUIObjectByName(interactImageName);
            if (foundInteractObj != null) interactImage = foundInteractObj.GetComponent<Image>();
        }

        if (interactionSlider == null)
        {
            GameObject foundSliderObj = FindUIObjectByName(sliderUIName);
            if (foundSliderObj != null) interactionSlider = foundSliderObj.GetComponent<Slider>();
        }
    }

    private GameObject FindUIObjectByName(string objName)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
                if (child.name.Trim() == objName.Trim()) return child.gameObject;
        }
        return null;
    }

    public override void FixedUpdateNetwork()
    {
        if (isVaulting)
        {
            vaultTimer += Runner.DeltaTime;
            float safeDuration = Mathf.Max(0.1f, currentDuration);
            transform.position = Vector3.Lerp(vStart, vEnd, vaultTimer / safeDuration);
        }
    }

    private void Update()
    {
        if (!Object.HasInputAuthority) return;

        // Ép tắt Image nếu Slider đang chạy để chống kẹt hình ảnh
        if (isSliderRunning && interactImage != null && interactImage.gameObject.activeSelf)
        {
            interactImage.gameObject.SetActive(false);
        }

        if (isSliderRunning && interactionSlider != null)
        {
            sliderTimer += Time.deltaTime;
            float safeDuration = Mathf.Max(0.1f, currentDuration);

            interactionSlider.value = sliderTimer / safeDuration;

            if (sliderTimer >= safeDuration)
            {
                isSliderRunning = false;
                interactionSlider.gameObject.SetActive(false);
            }
        }
    }

    public bool IsDoingAction() { return isInteracting || isVaulting || isSliderRunning; }

    public void TryInteract()
    {
        // 🚨 CHỐT DEBUG: Cấm bấm đúp phím Space hoặc spam lệnh
        if (isInteracting || isSliderRunning)
        {
            Debug.LogWarning("❌ [Hunter UI] Đang tương tác, không thể nhấn Space thêm nữa!");
            return;
        }

        if (currentInteractTarget == null) return;

        string tag = currentInteractTarget.tag;

        if (isCarryingPlayer && tag != "Moc") return;
        if (tag == "Moc" && !isCarryingPlayer) return;

        // BƯỚC 1: SET THỜI GIAN
        if (tag == "May") currentDuration = timeDapMay;
        else if (tag == "Moc") currentDuration = timeTreoMoc;
        else if (tag == "Playerchet") currentDuration = timeNhatPlayer;
        else if (tag == "Cuaso") currentDuration = timeTreoCUASO;

        // BƯỚC 2: CHẠY UI NGAY LẬP TỨC 
        if (Object.HasInputAuthority)
        {
            if (interactImage != null) interactImage.gameObject.SetActive(false);
            if (interactionSlider != null)
            {
                interactionSlider.gameObject.SetActive(true);
                interactionSlider.value = 0f;
                sliderTimer = 0f;
                isSliderRunning = true; // Bật cờ này lên để khóa các hàm Trigger lại
                Debug.Log("✅ [Hunter UI] Đã bắt đầu chạy Slider cho: " + tag);
            }
        }

        // BƯỚC 3: GỬI LÊN SERVER 
        NetworkObject netObj = currentInteractTarget.GetComponentInParent<NetworkObject>();
        NetworkId idToSend = netObj != null ? netObj.Id : default;

        Vector3 exactTargetPos = currentInteractTarget.transform.position;
        Quaternion exactTargetRot = currentInteractTarget.transform.rotation;

        if (tag == "Moc")
        {
            Transform hookPoint = currentInteractTarget.transform.Find("HookPoint");
            if (hookPoint != null)
            {
                exactTargetPos = hookPoint.position;
                exactTargetRot = hookPoint.rotation;
            }
        }

        Rpc_RequestInteraction(tag, exactTargetPos, exactTargetRot, idToSend);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestInteraction(string tag, Vector3 targetPosition, Quaternion targetRotation, NetworkId targetId)
    {
        isInteracting = true;
        syncedTargetId = targetId;
        syncedTargetPos = targetPosition;
        syncedTargetRot = targetRotation;
        Rpc_PlayInteractionEffects(tag, targetPosition);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayInteractionEffects(string tag, Vector3 targetPosition)
    {
        Vector3 lookPos = targetPosition;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (Object.HasInputAuthority && fpsCameraScript != null)
        {
            fpsCameraScript.isCameraLockedForAnim = true;
            fpsCameraScript.SyncCameraAngles(transform.eulerAngles.y);
        }

        if (tag == "May") animator.SetTrigger("Dapmay");
        else if (tag == "Moc") animator.SetTrigger("Treomoc");
        else if (tag == "Playerchet")
        {
            animator.SetTrigger("Nhacplayer");

            NetworkObject targetObj = Runner.FindObject(syncedTargetId);
            if (targetObj != null) carriedPlayerObject = targetObj.gameObject;
        }
        else if (tag == "Cuaso")
        {
            animator.SetTrigger("Treocuaso");
            isVaulting = true; controller.enabled = false;
            vStart = transform.position; vEnd = transform.position + transform.forward * vaultDistance;
            vaultTimer = 0f;
        }
    }

    public void AttachPlayerToHand()
    {
        if (carriedPlayerObject != null && handPoint != null)
        {
            Transform interactionTrigger = carriedPlayerObject.transform.Find("Playerchet");
            if (interactionTrigger == null) interactionTrigger = FindChildWithTag(carriedPlayerObject, "Playerchet");
            if (interactionTrigger != null) interactionTrigger.gameObject.SetActive(false);

            if (Object.HasStateAuthority)
            {
                PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
                if (receiver != null) receiver.GetPickedUpOrHooked(handPoint);
            }

            if (Object.HasInputAuthority && interactImage != null) interactImage.gameObject.SetActive(false);
            currentInteractTarget = null;
        }
    }

    public void AttachPlayerToShoulder()
    {
        if (carriedPlayerObject != null && shoulderPoint != null)
        {
            if (Object.HasStateAuthority)
            {
                PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
                if (receiver != null) receiver.GetPickedUpOrHooked(shoulderPoint);
                isCarryingPlayer = true;
            }

            if (Object.HasInputAuthority)
            {
                ToggleAuraGroup(allGenerators, auraMatRed, false);
                ToggleAuraGroup(allGenerators, auraMatWhite, true);
                ToggleAuraGroup(allHooks, auraMatRed, true);
            }
        }
    }

    public void HookPlayerToHook()
    {
        if (carriedPlayerObject != null)
        {
            if (Object.HasStateAuthority)
            {
                IShowSpeedController_Fusion survivor = carriedPlayerObject.GetComponent<IShowSpeedController_Fusion>();
                if (survivor != null)
                {
                    survivor.GetHooked(syncedTargetPos, syncedTargetRot);
                }
                isCarryingPlayer = false;
            }

            if (currentInteractTarget != null) currentInteractTarget.tag = "Untagged";

            if (Object.HasInputAuthority)
            {
                if (interactImage != null) interactImage.gameObject.SetActive(false);
                ToggleAuraGroup(allGenerators, auraMatWhite, false);
                ToggleAuraGroup(allGenerators, auraMatRed, true);
                ToggleAuraGroup(allHooks, auraMatRed, false);
            }

            carriedPlayerObject = null;
            currentInteractTarget = null;
        }
    }

    private Transform FindChildWithTag(GameObject parent, string tag)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            if (child.CompareTag(tag)) return child;
        return null;
    }

    // HÀM NÀY NÊN ĐƯỢC GỌI BỞI ANIMATION EVENT Ở CUỐI HOẠT ẢNH
    public void FinishInteraction()
    {
        if (Object.HasStateAuthority)
        {
            isInteracting = false;
            if (isVaulting) { isVaulting = false; controller.enabled = true; }
        }

        if (Object.HasInputAuthority)
        {
            if (fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = false;

            // Xóa UI sau khi Animation chạy xong
            isSliderRunning = false;
            if (interactionSlider != null) { interactionSlider.value = 1f; interactionSlider.gameObject.SetActive(false); }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Nếu đang tương tác thì bỏ qua va chạm mới
        if (isInteracting || isSliderRunning) return;

        // 🚨 2. FIX LỖI TỰ NHẬN DIỆN: Chống Hunter tự va chạm với các Trigger trên chính cơ thể mình
        if (other.transform.root == transform.root) return;

        if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Playerchet") || other.CompareTag("Cuaso"))
        {
            // Kiểm tra điều kiện vác người
            if (isCarryingPlayer && !other.CompareTag("Moc")) return;
            if (other.CompareTag("Moc") && !isCarryingPlayer) return;

            // Kiểm tra điều kiện đạp máy
            if (other.CompareTag("May"))
            {
                Generator gen = other.GetComponent<Generator>();
                if (gen == null || !gen.CanBeDamagedByHunter()) return;
            }

            // 🚨 3. FIX LỖI HIỆN IMG KHI PLAYER CHƯA GỤC
            if (other.CompareTag("Playerchet"))
            {
                IShowSpeedController_Fusion survivor = other.GetComponentInParent<IShowSpeedController_Fusion>();

                // Nếu KHÔNG tìm thấy script Survivor, HOẶC Survivor chưa gục (IsDowned = false) => HỦY NGAY LẬP TỨC
                if (survivor == null || !survivor.IsDowned)
                {
                    return;
                }
            }

            // Mọi điều kiện hợp lệ -> Chốt mục tiêu
            currentInteractTarget = other;

            if (Object.HasInputAuthority)
            {
                if (interactImage == null || interactionSlider == null) AutoFindUI();

                if (interactImage != null) interactImage.gameObject.SetActive(true);
                if (interactionSlider != null) interactionSlider.gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Tránh lỗi đang tương tác mà trượt khỏi Trigger làm mất UI
        if (isInteracting || isSliderRunning) return;

        if (currentInteractTarget == other)
        {
            currentInteractTarget = null;
            if (Object.HasInputAuthority && interactImage != null) interactImage.gameObject.SetActive(false);
        }
    }

    private void ToggleAuraGroup(GameObject[] objects, Material targetMat, bool turnOn)
    {
        if (targetMat == null) return;
        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                Material[] currentMats = r.materials;
                bool hasAura = false;
                foreach (Material m in currentMats) { if (m.name.Contains(targetMat.name)) hasAura = true; }

                if (turnOn && !hasAura)
                {
                    Material[] newMats = new Material[currentMats.Length + 1];
                    for (int i = 0; i < currentMats.Length; i++) newMats[i] = currentMats[i];
                    newMats[currentMats.Length] = targetMat;
                    r.materials = newMats;
                }
                else if (!turnOn && hasAura)
                {
                    List<Material> cleanedMats = new List<Material>();
                    foreach (Material m in currentMats) { if (!m.name.Contains(targetMat.name)) cleanedMats.Add(m); }
                    r.materials = cleanedMats.ToArray();
                }
            }
        }
    }

    // --- ANIMATION EVENTS ---
    public void EventDapMay()
    {
        if (interactAudioSource != null && clipDapMay != null) interactAudioSource.PlayOneShot(clipDapMay);

        if (Object.HasStateAuthority)
        {
            NetworkObject targetObj = Runner.FindObject(syncedTargetId);
            if (targetObj != null)
            {
                Generator gen = targetObj.GetComponent<Generator>();
                if (gen != null) gen.DamageByHunterServer();
            }
        }
    }
    public void EventVault() { if (isVaulting) { if (interactAudioSource != null && clipTreoCuaso != null) interactAudioSource.PlayOneShot(clipTreoCuaso); } }
    public void EventTreoMoc() { if (isCarryingPlayer) { if (interactAudioSource != null && clipTreoMoc != null) interactAudioSource.PlayOneShot(clipTreoMoc); HookPlayerToHook(); } }
}