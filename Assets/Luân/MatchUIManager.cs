using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class MatchUIManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerUISlot
    {
        public GameObject rootObj; // Giao diện tổng của 1 người chơi (Slot)
        
        [Header("Thông tin cơ bản (Sẽ tự tìm nếu để trống)")]
        public Image playerImage;        // Hình ảnh đại diện của nhân vật
        public TextMeshProUGUI nameText; // Tên người chơi

        [Header("Hook Cooldown")]
        public Image hookOverlay; 
        public TextMeshProUGUI hookCooldownText; 

        [HideInInspector] public ISurvivor linkedSurvivor;
        [HideInInspector] public bool isAssigned; 
        [HideInInspector] public int lastAssignedPlayerID = -1;
    }

    [Header("Cài đặt UI Khu vực người chơi")]
    [Tooltip("Khung chứa có gắn component Vertical Layout Group")]
    public RectTransform slotContainer; 
    public PlayerUISlot[] playerSlots; 

    [Header("Dữ liệu hình ảnh (Đồng bộ theo CharacterID)")]
    [Tooltip("Thứ tự hình ảnh phải khớp với CharacterID của người chơi")]
    public Sprite[] characterAvatars; 

    private float maxHookTime = 90f; 

    private void Start()
    {
        // Tắt hết các slot khi mới bắt đầu
        foreach (var slot in playerSlots)
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
        }
    }

    private void Update()
    {
        // Lấy danh sách RoomPlayer đã được spawn trên mạng
        var roomPlayers = FindObjectsOfType<RoomPlayer>().Where(p => p.Object != null && p.Object.IsValid).ToList();
        
        // Chỉ lấy những người chơi là Survivor
        var survivors = roomPlayers.Where(p => !p.IsHunter).OrderBy(p => p.Object.InputAuthority.PlayerId).ToList();

        // Kiểm tra xem có ai mới vào/ra phòng không để vẽ lại UI
        if (CheckIfNeedsRefresh(survivors)) 
        {
            SetupUI(survivors);
            
            // Ép Layout Group cập nhật ngay lập tức để xếp dọc sát nhau
            if (slotContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer);
            }
        }

        // Chạy logic đếm ngược Hook
        UpdateHookCooldownLogic(roomPlayers);
    }

    private bool CheckIfNeedsRefresh(List<RoomPlayer> currentSurvivors)
    {
        int activeCount = playerSlots.Count(s => s.isAssigned);
        if (currentSurvivors.Count != activeCount) return true;

        foreach (var surv in currentSurvivors)
        {
            if (!playerSlots.Any(s => s.isAssigned && s.lastAssignedPlayerID == surv.Object.InputAuthority.PlayerId))
                return true;
        }
        return false;
    }

    private void SetupUI(List<RoomPlayer> survivors)
    {
        // Reset lại toàn bộ slots
        foreach (var slot in playerSlots) 
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
            slot.isAssigned = false;
            slot.lastAssignedPlayerID = -1;
            slot.linkedSurvivor = null;
        }

        // Bật slot cho từng người chơi
        foreach (var player in survivors)
        {
            // Tìm 1 slot trống đầu tiên
            var slot = playerSlots.FirstOrDefault(s => !s.isAssigned);

            if (slot != null && slot.rootObj != null)
            {
                slot.rootObj.SetActive(true);
                
                // Đẩy xuống cuối hierarchy để Vertical Layout Group xếp chồng khít lên nhau
                slot.rootObj.transform.SetAsLastSibling(); 
                
                slot.isAssigned = true;
                slot.lastAssignedPlayerID = player.Object.InputAuthority.PlayerId;
                
                // --- TỰ ĐỘNG TÌM COMPONENT NẾU CHƯA GÁN ---
                if (slot.nameText == null) 
                    slot.nameText = slot.rootObj.GetComponentInChildren<TextMeshProUGUI>(true);
                
                if (slot.playerImage == null) 
                    slot.playerImage = slot.rootObj.GetComponentInChildren<Image>(true);

                // --- GÁN DỮ LIỆU ĐỒNG BỘ MẠNG ---
                // 1. Gán Tên
                if (slot.nameText != null)
                {
                    string pName = player.PlayerName.ToString();
                    slot.nameText.text = string.IsNullOrEmpty(pName) ? "Player..." : pName;
                }

                // 2. Gán Hình Ảnh (Image) dựa trên CharacterID
                if (slot.playerImage != null)
                {
                    int charID = player.CharacterID;
                    if (charID >= 0 && charID < characterAvatars.Length)
                    {
                        slot.playerImage.sprite = characterAvatars[charID];
                    }
                    else
                    {
                        Debug.LogWarning($"[UI] Không tìm thấy hình ảnh Avatar cho CharacterID {charID}");
                    }
                }
            }
        }
    }

    // ==========================================
    // LOGIC HOOK COOLDOWN (GIỮ NGUYÊN TỪ CODE CŨ)
    // ==========================================
    private void UpdateHookCooldownLogic(List<RoomPlayer> allPlayers)
    {
        foreach (var slot in playerSlots)
        {
            if (!slot.isAssigned || slot.rootObj == null) continue;

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

    private void FindLinkedSurvivor(PlayerUISlot slot, List<RoomPlayer> allPlayers)
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