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
        public int characterID; // ID nhân vật để lấy đúng Avatar (Ví dụ: MrBeast = 1, MrBean = 2, v.v.)
        public GameObject rootObj; // GameObject cha (Ytamain, MrBeastMain...)
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
        // Ẩn toàn bộ UI lúc mới vào game (Layout Group sẽ tự động co lại)
        foreach (var slot in survivorSlots)
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
        }
    }

    private void Update()
    {
        var roomPlayers = FindObjectsOfType<RoomPlayer>().ToList();
        var survivors = roomPlayers.Where(p => !p.IsHunter).ToList(); // Chỉ lấy người chơi là Survivor

        bool needsRefresh = false;
        int activeSlotCount = survivorSlots.Count(s => s.isAssigned);

        // Kích hoạt làm mới UI nếu số lượng người chơi thay đổi
        if (survivors.Count != activeSlotCount)
        {
            needsRefresh = true;
        }
        else
        {
            foreach (var surv in survivors)
            {
                if (!survivorSlots.Any(s => s.isAssigned && s.characterID == surv.CharacterID))
                {
                    needsRefresh = true;
                    break;
                }
            }
        }

        if (needsRefresh) SetupUI(survivors);

        // Xử lý Hook logic cho các UI đang hoạt động
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
        // 1. Tắt hết và reset trạng thái của 4 slot UI
        foreach (var slot in survivorSlots) 
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
            slot.isAssigned = false;
            slot.linkedSurvivor = null; 

            if (slot.hookOverlay != null) slot.hookOverlay.gameObject.SetActive(false);
            if (slot.hookCooldownText != null) slot.hookCooldownText.gameObject.SetActive(false);
        }

        // 2. Chỉ bật UI cho số lượng người chơi thực tế
        foreach (var player in survivors)
        {
            int charID = player.CharacterID;
            
            // Lọc ra UI Slot có characterID khớp với ID mà player đã chọn
            var slot = survivorSlots.FirstOrDefault(s => s.characterID == charID && !s.isAssigned);
            
            // Dành cho trường hợp hiếm: 2 người chơi chọn CÙNG 1 nhân vật 
            // Sẽ lấy tạm 1 slot trống để không bị mất UI
            if (slot == null) 
            {
                slot = survivorSlots.FirstOrDefault(s => !s.isAssigned);
            }

            if (slot != null && slot.rootObj != null)
            {
                slot.rootObj.SetActive(true); // Bật Image/Text của nhân vật đó lên
                slot.rootObj.transform.SetAsLastSibling(); // Đẩy object xuống cuối hệ thống (Giúp Layout sắp xếp đúng thứ tự)
                slot.isAssigned = true;
                
                if (slot.nameText != null) slot.nameText.text = player.PlayerName.ToString(); 
            }
        }
    }

    private void FindLinkedSurvivor(SurvivorUISlot slot, List<RoomPlayer> allRoomPlayers)
    {
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