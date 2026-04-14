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
        [HideInInspector] public int lastAssignedPlayerID = -1; // Để theo dõi player nào đang giữ slot này
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
        // Lấy danh sách RoomPlayer đã được spawn hoàn toàn trên mạng
        var roomPlayers = FindObjectsOfType<RoomPlayer>().Where(p => p.Object != null && p.Object.IsValid).ToList();
        var survivors = roomPlayers.Where(p => !p.IsHunter).OrderBy(p => p.Object.InputAuthority.PlayerId).ToList();

        // Kiểm tra xem có cần cập nhật UI không (Dựa trên số lượng hoặc ID người chơi thay đổi)
        bool needsRefresh = CheckIfNeedsRefresh(survivors);

        if (needsRefresh) 
        {
            SetupUI(survivors);
            // Ép buộc Layout Group sắp xếp lại ngay lập tức để tránh lỗi "dính" hoặc "xa" nhau
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

        UpdateSlotsLogic(roomPlayers);
    }

    private bool CheckIfNeedsRefresh(List<RoomPlayer> currentSurvivors)
    {
        int activeCount = survivorSlots.Count(s => s.isAssigned);
        if (currentSurvivors.Count != activeCount) return true;

        foreach (var surv in currentSurvivors)
        {
            // Nếu có một người chơi chưa có slot nào được gán đúng ID của họ
            if (!survivorSlots.Any(s => s.isAssigned && s.lastAssignedPlayerID == surv.Object.InputAuthority.PlayerId))
                return true;
        }
        return false;
    }

    private void SetupUI(List<RoomPlayer> survivors)
    {
        // Reset toàn bộ slots
        foreach (var slot in survivorSlots) 
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
            slot.isAssigned = false;
            slot.lastAssignedPlayerID = -1;
            slot.linkedSurvivor = null;
        }

        foreach (var player in survivors)
        {
            // Ưu tiên tìm slot đúng CharacterID, nếu không có thì lấy slot trống đầu tiên
            var slot = survivorSlots.FirstOrDefault(s => s.characterID == player.CharacterID && !s.isAssigned) 
                     ?? survivorSlots.FirstOrDefault(s => !s.isAssigned);

            if (slot != null)
            {
                if (slot.rootObj != null)
                {
                    slot.rootObj.SetActive(true);
                    // Quan trọng: Đẩy xuống cuối để Layout Group sắp xếp theo thứ tự khít nhau
                    slot.rootObj.transform.SetAsLastSibling();
                }
                
                slot.isAssigned = true;
                slot.lastAssignedPlayerID = player.Object.InputAuthority.PlayerId;
                
                // === THÊM CODE TÌM TEXT NẾU CHƯA GÁN ===
                if (slot.nameText == null && slot.rootObj != null)
                {
                    // Tự động tìm component TextMeshProUGUI nằm trong rootObj (tìm cả các object đang bị tắt)
                    slot.nameText = slot.rootObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                // Cập nhật Text
                if (slot.nameText != null)
                {
                    string pName = player.PlayerName.ToString();
                    slot.nameText.text = string.IsNullOrEmpty(pName) ? "Player..." : pName;
                    
                    // Bật object text lên để đảm bảo nó hiển thị
                    slot.nameText.gameObject.SetActive(true); 
                }
                else
                {
                    Debug.LogError($"[CẢNH BÁO] Slot ID {slot.characterID} không tìm thấy TextMeshProUGUI! Hãy kiểm tra lại prefab của bạn.");
                }
            }
        }
    }

    private void UpdateSlotsLogic(List<RoomPlayer> allPlayers)
    {
        foreach (var slot in survivorSlots)
        {
            if (!slot.isAssigned || slot.rootObj == null) continue;

            // Tìm ISurvivor tương ứng nếu chưa có link
            if (slot.linkedSurvivor == null)
            {
                FindLinkedSurvivor(slot, allPlayers);
            }

            if (slot.linkedSurvivor != null)
            {
                bool isHooked = slot.linkedSurvivor.GetIsHooked();
                if (slot.hookOverlay != null) slot.hookOverlay.gameObject.SetActive(isHooked);
                if (slot.hookCooldownText != null) slot.hookCooldownText.gameObject.SetActive(isHooked);

                if (isHooked)
                {
                    float timeLeft = slot.linkedSurvivor.GetSacrificeTimer();
                    slot.hookCooldownText.text = Mathf.Ceil(timeLeft).ToString();
                    slot.hookOverlay.fillAmount = timeLeft / maxHookTime;
                }
            }
        }
    }

    private void FindLinkedSurvivor(SurvivorUISlot slot, List<RoomPlayer> allPlayers)
    {
        var targetPlayer = allPlayers.FirstOrDefault(p => p.Object.InputAuthority.PlayerId == slot.lastAssignedPlayerID);
        if (targetPlayer == null) return;

        var survivorsInScene = FindObjectsOfType<MonoBehaviour>().OfType<ISurvivor>();
        foreach (var s in survivorsInScene)
        {
            var netObj = ((Component)s).GetComponent<Fusion.NetworkObject>();
            if (netObj != null && netObj.InputAuthority == targetPlayer.Object.InputAuthority)
            {
                slot.linkedSurvivor = s;
                break;
            }
        }
    }
}