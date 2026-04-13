using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class MatchUIManager : MonoBehaviour
{
    [System.Serializable]
    public class SurvivorUISlot
    {
        public int characterID; 
        public GameObject rootObj; 
        public TextMeshProUGUI nameText; 
        
        [Header("Hook Cooldown")]
        public Image hookOverlay; 
        public TextMeshProUGUI hookCooldownText; 

        [HideInInspector] public ISurvivor linkedSurvivor;
        [HideInInspector] public bool isAssigned; 
    }

    public SurvivorUISlot[] survivorSlots;
    private float maxHookTime = 90f; 

    private void Start()
    {
        foreach (var slot in survivorSlots)
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
        }
    }

    private void Update()
    {
        // Tối ưu: Dùng mảng có sẵn thay vì FindObjectsOfType liên tục nếu có thể, 
        // nhưng tạm thời giữ cấu trúc của bạn.
        var roomPlayers = FindObjectsOfType<RoomPlayer>().ToList();
        var survivors = roomPlayers.Where(p => !p.IsHunter).ToList();

        bool needsRefresh = false;
        int activeSlotCount = survivorSlots.Count(s => s.isAssigned);

        if (survivors.Count != activeSlotCount)
        {
            needsRefresh = true;
        }
        else
        {
            foreach (var surv in survivors)
            {
                // Kiểm tra xem có slot nào đang được gán đúng characterID này chưa
                if (!survivorSlots.Any(s => s.isAssigned && s.characterID == surv.CharacterID))
                {
                    needsRefresh = true;
                    break;
                }
            }
        }

        if (needsRefresh) SetupUI(survivors);

        foreach (var slot in survivorSlots)
        {
            if (!slot.rootObj.activeSelf || !slot.isAssigned) continue;

            if (slot.linkedSurvivor == null || ((Component)slot.linkedSurvivor).gameObject == null)
            {
                FindLinkedSurvivor(slot, roomPlayers);
            }

            if (slot.linkedSurvivor != null)
            {
                bool isHooked = slot.linkedSurvivor.GetIsHooked();
                
                if (slot.hookOverlay != null) slot.hookOverlay.gameObject.SetActive(isHooked);
                if (slot.hookCooldownText != null) slot.hookCooldownText.gameObject.SetActive(isHooked);

                if (isHooked)
                {
                    float timeLeft = slot.linkedSurvivor.GetSacrificeTimer();
                    if (slot.hookCooldownText != null) slot.hookCooldownText.text = Mathf.Ceil(timeLeft).ToString();
                    if (slot.hookOverlay != null) slot.hookOverlay.fillAmount = timeLeft / maxHookTime; 
                }
            }
        }
    }

    private void SetupUI(List<RoomPlayer> survivors)
    {
        // 1. Dọn dẹp thật sạch các Slot và tắt các UI hiển thị ảo (ví dụ số 90)
        foreach (var slot in survivorSlots) 
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
            slot.isAssigned = false;
            slot.linkedSurvivor = null; // Phải reset link cũ

            // FIX LỖI 1: Tắt UI Hook ngay từ đầu để tránh hiện số 90 ảo lúc mới load Scene
            if (slot.hookOverlay != null) slot.hookOverlay.gameObject.SetActive(false);
            if (slot.hookCooldownText != null) slot.hookCooldownText.gameObject.SetActive(false);
        }

        // 2. Gán slot cho từng người chơi
        foreach (var player in survivors)
        {
            int charID = player.CharacterID;
            
            // FIX LỖI 2: Tìm slot có đúng characterID NHƯNG phải CHƯA được gán (!s.isAssigned)
            // Tránh trường hợp 2 người chơi có cùng ID đè lên cùng 1 bảng UI.
            var slot = survivorSlots.FirstOrDefault(s => s.characterID == charID && !s.isAssigned);
            
            // Nếu không tìm thấy slot theo ID, tự động lấy đại một slot còn trống để tránh mất UI
            if (slot == null) 
            {
                slot = survivorSlots.FirstOrDefault(s => !s.isAssigned);
                if (slot != null) Debug.LogWarning($"[MatchUIManager] Không tìm thấy UI cho CharacterID {charID}, đã mượn tạm slot của ID {slot.characterID}");
            }

            if (slot != null && slot.rootObj != null)
            {
                slot.rootObj.SetActive(true); 
                slot.rootObj.transform.SetAsLastSibling(); 
                slot.isAssigned = true;
                
                if (slot.nameText != null) slot.nameText.text = player.PlayerName.ToString(); 
            }
        }
    }

    private void FindLinkedSurvivor(SurvivorUISlot slot, List<RoomPlayer> allRoomPlayers)
    {
        // Do lúc SetupUI chúng ta có thể đã mượn slot nếu lỗi, nên việc tìm theo tên/quyền sẽ chuẩn xác hơn
        // Tuy nhiên tạm giữ nguyên logic tìm theo CharacterID của bạn, chỉ cần bổ sung thêm check
        var targetRoomPlayer = allRoomPlayers.FirstOrDefault(p => !p.IsHunter && p.CharacterID == slot.characterID);
        
        if (targetRoomPlayer != null)
        {
            var allSurvivors = FindObjectsOfType<MonoBehaviour>().OfType<ISurvivor>();
            foreach (var surv in allSurvivors)
            {
                var netObj = ((Component)surv).GetComponent<Fusion.NetworkObject>();
                if (netObj != null && netObj.InputAuthority == targetRoomPlayer.Object.InputAuthority)
                {
                    slot.linkedSurvivor = surv;
                    break;
                }
            }
        }
    }
}