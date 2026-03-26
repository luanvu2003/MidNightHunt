using UnityEngine;
using UnityEngine.UI; 

public class HunterInteraction : MonoBehaviour
{
    [Header("Giao Diện & UI")]
    public Image interactImage; 
    public Slider interactionSlider; 
    
    [Header("Tự Động Tìm UI (Nhập đúng tên ngoài Hierarchy)")]
    // LƯU Ý: Chú ý chữ ThanhTuonngtac của bạn có bị dư chữ 'n' ngoài Inspector không nhé!
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

        // Lần tìm kiếm số 1: Ngay khi vừa đẻ ra (Có thể xịt nếu Canvas load chậm)
        AutoFindUI();
    }

    // =========================================================
    // THUẬT TOÁN TỰ ĐỘNG QUÉT CANVAS SIÊU CẤP (CHỐNG LỖI 100%)
    // =========================================================
    private void AutoFindUI()
    {
        Debug.Log("🔍 Hunter đang đi lùng sục UI có tên chính xác là: [" + interactImageName + "] và [" + sliderUIName + "]");
        // Nếu ô hình ảnh đang bị None
        if (interactImage == null)
        {
            GameObject foundInteractObj = FindUIObjectByName(interactImageName);
            if (foundInteractObj != null) 
            {
                interactImage = foundInteractObj.GetComponent<Image>();
                Debug.Log("✅ Đã móc thành công: " + interactImageName);
            }
        }

        // Nếu ô Slider đang bị None
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
        // ĐÃ NÂNG CẤP: Thêm chữ 'true' vào đây để nó tìm cả những Canvas đang bị tắt ngang phè phè
        Canvas[] canvases = FindObjectsOfType<Canvas>(true); 
        
        foreach (Canvas canvas in canvases)
        {
            Transform[] children = canvas.GetComponentsInChildren<Transform>(true); 
            foreach (Transform child in children)
            {
                // ĐÃ NÂNG CẤP: Dùng Trim() để gọt sạch các dấu cách (Space) lỡ tay gõ thừa ở đầu/cuối tên
                if (child.name.Trim() == objName.Trim()) 
                {
                    return child.gameObject;
                }
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
        
        if (interactionSlider != null) {
            interactionSlider.gameObject.SetActive(true);
            interactionSlider.value = 0f; 
            isSliderRunning = true;       
        }

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
    }

    public void AttachPlayerToHand() 
    {
        if (carriedPlayerObject != null && handPoint != null)
        {
            Collider[] colliders = carriedPlayerObject.GetComponentsInChildren<Collider>();
            foreach(var col in colliders) col.enabled = false;

            carriedPlayerObject.tag = "Untagged"; 
            carriedPlayerObject.transform.SetParent(handPoint); 
            carriedPlayerObject.transform.localPosition = Vector3.zero; 
            carriedPlayerObject.transform.localRotation = Quaternion.identity;
            
            currentInteractTarget = null;
        }
    }

    public void AttachPlayerToShoulder() 
    {
        if (carriedPlayerObject != null && shoulderPoint != null)
        {
            carriedPlayerObject.transform.SetParent(shoulderPoint); 
            carriedPlayerObject.transform.localPosition = Vector3.zero; 
            carriedPlayerObject.transform.localRotation = Quaternion.identity;
            isCarryingPlayer = true; 
        }
    }

    public void HookPlayerToHook() 
    {
        if (carriedPlayerObject != null && currentInteractTarget != null)
        {
            Transform hookPoint = currentInteractTarget.transform.Find("HookPoint"); 
            carriedPlayerObject.transform.SetParent(hookPoint ? hookPoint : currentInteractTarget.transform); 
            carriedPlayerObject.transform.localPosition = Vector3.zero;
            carriedPlayerObject.transform.localRotation = Quaternion.identity;
            
            isCarryingPlayer = false; 
            currentInteractTarget.tag = "Untagged"; 
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
        if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Player") || other.CompareTag("Cuaso"))
        {
            if (isCarryingPlayer && !other.CompareTag("Moc")) return;
            if (other.CompareTag("Moc") && !isCarryingPlayer) return; 

            // 🚨 NÂNG CẤP CHỐNG MÙ UI LẦN 2: Lỡ đẻ ra mà tìm xịt, thì lúc lại gần đồ vật TÌM LẠI LẦN NỮA!
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
}