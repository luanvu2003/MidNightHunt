using UnityEngine;
using UnityEngine.UI;

public class HunterInteraction : MonoBehaviour
{
    [Header("Giao Diện & UI")]
    public GameObject interactUI; // Chữ "Bấm Space"
    public Slider interactionSlider; // Thanh chạy tiến trình
    
    // Thời gian múa thực tế của từng file Animation
    public float timeDapMay = 2.0f;
    public float timeTreoCUASO = 1.5f;
    public float timeTreoMoc = 3.0f;
    public float timeNhatPlayer = 1.5f;

    [Header("Hệ Thống Vác")]
    public Transform shoulderPoint; // Cục bone trên vai
    public bool isCarryingPlayer = false; // Biến cờ: Báo hiệu vai đang bận vác người
    private GameObject carriedPlayerObject; // Lưu cái xác

    [Header("Vượt Cửa Sổ")]
    public float vaultDistance = 2.5f; // Khoảng cách phi thân

    private Collider currentInteractTarget; // Vật đứng gần
    private float currentDuration = 1f; // Thời gian sẽ phải chờ
    private bool isInteracting = false; // Cờ báo bận múa
    private bool isSliderRunning = false; // Cờ bật thanh Slider
    private float sliderTimer = 0f; // Đồng hồ tính giờ
    private bool isVaulting = false; // Cờ báo lướt cửa sổ
    private Vector3 vStart, vEnd; // Tọa độ bay qua cửa

    private Animator animator;
    private CharacterController controller;
    private FPSCamera fpsCameraScript;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        if (Camera.main != null) fpsCameraScript = Camera.main.GetComponent<FPSCamera>();
    }

    private void Start()
    {
        if (interactionSlider != null) interactionSlider.gameObject.SetActive(false);
        if (interactUI != null) interactUI.SetActive(false);
    }

    private void Update()
    {
        // Hệ thống chạy thanh trượt thời gian
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

        // Lướt người qua cửa sổ theo trục tịnh tiến
        if (isVaulting)
        {
            transform.position = Vector3.Lerp(vStart, vEnd, sliderTimer / currentDuration);
        }
    }

    public bool IsDoingAction()
    {
        return isInteracting || isVaulting;
    }

    // Hàm gọi khi bấm Space
    public void TryInteract()
    {
        if (isInteracting || currentInteractTarget == null) return;

        string tag = currentInteractTarget.tag;
        
        // ========================================================
        // 🚨 CHỐT CHẶN LOGIC (ĐÃ NÂNG CẤP)
        // ========================================================
        
        // 1. Đang vác người trên vai? -> CHỈ ĐƯỢC phép tương tác với "Moc", CẤM đạp máy, trèo cửa, nhặt thêm người.
        if (isCarryingPlayer && tag != "Moc") 
        {
            Debug.Log("Đang vác người, cấm làm hành động khác ngoài treo móc!");
            return; 
        }

        // 2. Muốn treo móc? -> Bắt buộc phải có người trên vai mới được treo.
        if (tag == "Moc" && !isCarryingPlayer) return; 

        // Đủ điều kiện thì bắt tay vào làm
        StartInteraction(tag);
    }

    private void StartInteraction(string tag)
    {
        isInteracting = true; 
        sliderTimer = 0f; 
        
        Vector3 lookPos = currentInteractTarget.transform.position;
        lookPos.y = transform.position.y; 
        transform.LookAt(lookPos); 
        
        if (fpsCameraScript != null) {
            fpsCameraScript.isCameraLockedForAnim = true; 
            fpsCameraScript.SyncCameraAngles(transform.eulerAngles.y); 
        }

        if (tag == "May") { animator.SetTrigger("Dapmay"); currentDuration = timeDapMay; }
        else if (tag == "Moc") { animator.SetTrigger("Treomoc"); currentDuration = timeTreoMoc; }
        else if (tag == "Player") { 
            animator.SetTrigger("Nhacplayer"); currentDuration = timeNhatPlayer; 
            carriedPlayerObject = currentInteractTarget.gameObject; 
        }
        else if (tag == "Cuaso") { 
            animator.SetTrigger("Treocuaso"); currentDuration = timeTreoCUASO; 
            isVaulting = true; 
            controller.enabled = false; 
            vStart = transform.position; 
            vEnd = transform.position + transform.forward * vaultDistance; 
        }

        if (interactionSlider != null) {
            interactionSlider.gameObject.SetActive(true);
            interactionSlider.value = 0f;
            isSliderRunning = true;
        }
    }

    // Gắn vào frame Anim nhấc người
    public void AttachPlayerToShoulder() 
    {
        if (carriedPlayerObject != null && shoulderPoint != null)
        {
            Collider[] colliders = carriedPlayerObject.GetComponentsInChildren<Collider>();
            foreach(var col in colliders) col.enabled = false;

            carriedPlayerObject.tag = "Untagged"; // Đổi tag để khỏi quét trúng
            carriedPlayerObject.transform.SetParent(shoulderPoint); 
            carriedPlayerObject.transform.localPosition = Vector3.zero; 
            carriedPlayerObject.transform.localRotation = Quaternion.identity;
            
            isCarryingPlayer = true; // Bật cờ đang vác người
            currentInteractTarget = null;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }

    // Gắn vào frame Anim treo người
    public void HookPlayerToHook() 
    {
        if (carriedPlayerObject != null && currentInteractTarget != null)
        {
            Transform hookPoint = currentInteractTarget.transform.Find("HookPoint"); 
            carriedPlayerObject.transform.SetParent(hookPoint ? hookPoint : currentInteractTarget.transform); 
            carriedPlayerObject.transform.localPosition = Vector3.zero;
            carriedPlayerObject.transform.localRotation = Quaternion.identity;
            
            isCarryingPlayer = false; // Vai đã trống
            currentInteractTarget.tag = "Untagged"; // Đổi tag móc chống treo đè
            carriedPlayerObject = null; 
            currentInteractTarget = null;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }

    // Gắn vào frame Anim cuối cùng
    public void FinishInteraction() 
    {
        isInteracting = false; 
        if (fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = false; 
        if (isVaulting) { isVaulting = false; controller.enabled = true; }
        isSliderRunning = false; 
        if (interactionSlider != null) { interactionSlider.value = 1f; interactionSlider.gameObject.SetActive(false); }
    }

    // ========================================================
    // CẢM BIẾN VÙNG CHẠM DƯỚI CHÂN
    // ========================================================
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Player") || other.CompareTag("Cuaso"))
        {
            // 🚨 ẨN UI THÔNG MINH: Nếu đang vác người mà đứng sát Máy phát điện / Cửa sổ / Nạn nhân khác -> Tắt luôn chữ "Bấm Space" để người chơi biết là cấm dùng.
            if (isCarryingPlayer && !other.CompareTag("Moc")) return;

            // Nếu muốn dùng móc thì cũng phải kiểm tra xem có người không mới hiện chữ
            if (other.CompareTag("Moc") && !isCarryingPlayer) return; 

            currentInteractTarget = other; 
            if (interactUI != null) interactUI.SetActive(true); 
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentInteractTarget == other) 
        {
            currentInteractTarget = null; 
            if (interactUI != null) interactUI.SetActive(false); 
        }
    }
}