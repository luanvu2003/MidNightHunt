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
    public string interactImageName = "Imgtt";
    public string sliderUIName = "Slidertt";

    [Header("Cài Đặt Thời Gian")]
    public float timeDapMay = 2.0f;
    public float timeTreoCUASO = 1.5f;
    public float timeTreoMoc = 3.0f;
    public float timeNhatPlayer = 1.5f;

    [Header("Hệ Thống Vác Người")]
    public Transform handPoint;
    public Transform shoulderPoint;
    [Networked] public NetworkBool isCarryingPlayer { get; set; }
    private GameObject carriedPlayerObject;

    public float vaultDistance = 2.5f;

    [Header("Âm Thanh Tương Tác")]
    public AudioSource interactAudioSource;
    public AudioClip clipDapMay;
    public AudioClip clipTreoCuaso;
    public AudioClip clipTreoMoc;

    private Material auraMatRed;   
    private Material auraMatWhite; 
    private GameObject[] allHooks;
    private GameObject[] allGenerators;

    private Collider currentInteractTarget;
    private float currentDuration = 1f;
    [Networked] private NetworkBool isInteracting { get; set; }
    private bool isSliderRunning = false;
    private float sliderTimer = 0f;
    [Networked] private NetworkBool isVaulting { get; set; }
    private Vector3 vStart, vEnd;

    private Animator animator;
    private CharacterController controller;
    private FPSCamera fpsCameraScript;

    public override void Spawned()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        if (Camera.main != null && Object.HasInputAuthority) fpsCameraScript = Camera.main.GetComponent<FPSCamera>();
        if (interactAudioSource == null) interactAudioSource = GetComponent<AudioSource>();

        if (Object.HasInputAuthority) AutoFindUI();

        auraMatRed = Resources.Load<Material>("Mat_AuraRed");
        auraMatWhite = Resources.Load<Material>("Mat_AuraWhite");

        if (Object.HasInputAuthority) // Aura chỉ xử lý local
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
            transform.position = Vector3.Lerp(vStart, vEnd, sliderTimer / currentDuration);
        }
    }

    private void Update()
    {
        if (isSliderRunning && interactionSlider != null && Object.HasInputAuthority)
        {
            sliderTimer += Time.deltaTime;
            interactionSlider.value = sliderTimer / currentDuration;

            if (sliderTimer >= currentDuration)
            {
                isSliderRunning = false;
                interactionSlider.gameObject.SetActive(false);
            }
        }
    }

    public bool IsDoingAction() { return isInteracting || isVaulting; }

    public void TryInteract()
    {
        if (isInteracting || currentInteractTarget == null) return;
        string tag = currentInteractTarget.tag;
        if (isCarryingPlayer && tag != "Moc") return;
        if (tag == "Moc" && !isCarryingPlayer) return;
        
        Rpc_StartInteraction(tag, currentInteractTarget.transform.position);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_StartInteraction(string tag, Vector3 targetPos)
    {
        isInteracting = true;
        Rpc_SyncInteractionVisuals(tag, targetPos);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_SyncInteractionVisuals(string tag, Vector3 targetPos)
    {
        sliderTimer = 0f;
        if (Object.HasInputAuthority)
        {
            if (interactImage != null) interactImage.gameObject.SetActive(false);
            if (interactionSlider != null) { interactionSlider.gameObject.SetActive(true); interactionSlider.value = 0f; isSliderRunning = true; }
        }

        Vector3 lookPos = targetPos;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (Object.HasInputAuthority && fpsCameraScript != null) { fpsCameraScript.isCameraLockedForAnim = true; fpsCameraScript.SyncCameraAngles(transform.eulerAngles.y); }

        if (tag == "May") { animator.SetTrigger("Dapmay"); currentDuration = timeDapMay; }
        else if (tag == "Moc") { animator.SetTrigger("Treomoc"); currentDuration = timeTreoMoc; }
        else if (tag == "Playerchet")
        {
            animator.SetTrigger("Nhacplayer"); currentDuration = timeNhatPlayer;
            if (currentInteractTarget != null) 
                carriedPlayerObject = currentInteractTarget.transform.parent != null ? currentInteractTarget.transform.parent.gameObject : currentInteractTarget.gameObject;
        }
        else if (tag == "Cuaso")
        {
            animator.SetTrigger("Treocuaso"); currentDuration = timeTreoCUASO;
            isVaulting = true; controller.enabled = false;
            vStart = transform.position; vEnd = transform.position + transform.forward * vaultDistance;
        }
    }

    public void AttachPlayerToHand()
    {
        if (carriedPlayerObject != null && handPoint != null && Object.HasStateAuthority)
        {
            PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
            if (receiver != null) receiver.Rpc_GetPickedUpOrHooked(Object, handPoint.name);

            if (Object.HasInputAuthority && interactImage != null) interactImage.gameObject.SetActive(false);
        }
    }

    public void AttachPlayerToShoulder()
    {
        if (carriedPlayerObject != null && shoulderPoint != null && Object.HasStateAuthority)
        {
            PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
            if (receiver != null) receiver.Rpc_GetPickedUpOrHooked(Object, shoulderPoint.name);

            isCarryingPlayer = true;
            Rpc_ToggleAuraShoulderLocal();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void Rpc_ToggleAuraShoulderLocal()
    {
        ToggleAuraGroup(allGenerators, auraMatRed, false);
        ToggleAuraGroup(allGenerators, auraMatWhite, true);
        ToggleAuraGroup(allHooks, auraMatRed, true);
    }

    public void HookPlayerToHook()
    {
        if (carriedPlayerObject != null && currentInteractTarget != null && Object.HasStateAuthority)
        {
            Transform hookPoint = currentInteractTarget.transform.Find("HookPoint");
            Transform finalPoint = hookPoint ? hookPoint : currentInteractTarget.transform;

            PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
            NetworkObject hookNetObj = finalPoint.GetComponentInParent<NetworkObject>();
            
            if (receiver != null && hookNetObj != null) receiver.Rpc_GetPickedUpOrHooked(hookNetObj, finalPoint.name);

            currentInteractTarget.tag = "Untagged";
            isCarryingPlayer = false;
            carriedPlayerObject = null;
            currentInteractTarget = null;

            Rpc_ToggleAuraHookLocal();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void Rpc_ToggleAuraHookLocal()
    {
        if (interactImage != null) interactImage.gameObject.SetActive(false);
        ToggleAuraGroup(allGenerators, auraMatWhite, false);
        ToggleAuraGroup(allGenerators, auraMatRed, true);
        ToggleAuraGroup(allHooks, auraMatRed, false);
    }

    public void FinishInteraction()
    {
        if (Object.HasStateAuthority)
        {
            isInteracting = false;
            isVaulting = false;
            controller.enabled = true;
        }

        if (Object.HasInputAuthority)
        {
            if (fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = false;
            isSliderRunning = false;
            if (interactionSlider != null) { interactionSlider.value = 1f; interactionSlider.gameObject.SetActive(false); }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentInteractTarget == other && Object.HasInputAuthority)
        {
            currentInteractTarget = null;
            if (interactImage != null) interactImage.gameObject.SetActive(false);
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

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasInputAuthority) return;

        if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Playerchet") || other.CompareTag("Cuaso"))
        {
            if (isCarryingPlayer && !other.CompareTag("Moc")) return;
            if (other.CompareTag("Moc") && !isCarryingPlayer) return;

            if (other.CompareTag("May"))
            {
                // TODO: Đảm bảo script Generator cũng được nâng cấp lên Fusion
                // Generator gen = other.GetComponent<Generator>();
                // if (gen == null || !gen.CanBeDamagedByHunter()) return;
            }

            if (interactImage == null || interactionSlider == null) AutoFindUI();
            currentInteractTarget = other;

            if (interactImage != null) interactImage.gameObject.SetActive(true);
            if (interactionSlider != null) interactionSlider.gameObject.SetActive(false);
        }
    }

    public void EventDapMay()
    {
        if (currentInteractTarget != null && currentInteractTarget.CompareTag("May") && Object.HasStateAuthority)
        {
            Rpc_PlaySound(1);
            // TODO: Gọi hàm trừ máu máy phát điện qua mạng
        }
    }
    
    public void EventVault() { if (isVaulting) Rpc_PlaySound(2); }
    public void EventTreoMoc() { if (isCarryingPlayer && Object.HasStateAuthority) { Rpc_PlaySound(3); HookPlayerToHook(); } }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlaySound(int soundID)
    {
        if (interactAudioSource != null)
        {
            if (soundID == 1 && clipDapMay != null) interactAudioSource.PlayOneShot(clipDapMay);
            if (soundID == 2 && clipTreoCuaso != null) interactAudioSource.PlayOneShot(clipTreoCuaso);
            if (soundID == 3 && clipTreoMoc != null) interactAudioSource.PlayOneShot(clipTreoMoc);
        }
    }
}