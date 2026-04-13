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
        [HideInInspector] public bool isAssigned; // Đánh dấu slot đã có người chơi
    }

    public SurvivorUISlot[] survivorSlots;
    private float maxHookTime = 90f; 
    private int _lastPlayerCount = 0; // Biến đếm số lượng người chơi

    private void Start()
    {
        // Vừa vào game: Tắt sạch sẽ tất cả 4 khung Avatar đi
        foreach (var slot in survivorSlots)
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
        }
    }

    private void Update()
    {
        // 1. Quét tìm tất cả người chơi trong phòng hiện tại
        var roomPlayers = FindObjectsOfType<RoomPlayer>().ToList();

        // 🚨 CHÌA KHÓA FIX LỖI THIẾU NGƯỜI: 
        // Nếu thấy số người tăng lên hoặc giảm đi -> Bật/Tắt Avatar lại từ đầu!
        if (roomPlayers.Count != _lastPlayerCount)
        {
            SetupUI(roomPlayers);
            _lastPlayerCount = roomPlayers.Count;
        }

        // 2. Liên tục cập nhật trạng thái Treo Móc
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
                    
                    if (slot.hookCooldownText != null) 
                        slot.hookCooldownText.text = Mathf.Ceil(timeLeft).ToString();
                    
                    if (slot.hookOverlay != null) 
                        slot.hookOverlay.fillAmount = timeLeft / maxHookTime; 
                }
            }
        }
    }

    private void SetupUI(List<RoomPlayer> players)
    {
        // Reset: Tắt hết Avatar trước khi xếp lại
        foreach (var slot in survivorSlots) 
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
            slot.isAssigned = false;
        }

        // Chỉ lọc ra những ai là Survivor
        var survivors = players.Where(p => !p.IsHunter).ToList();

        foreach (var player in survivors)
        {
            int charID = player.CharacterID;
            
            // Tìm cái Slot có ID khớp với ID của người chơi
            var slot = survivorSlots.FirstOrDefault(s => s.characterID == charID);
            
            if (slot != null && slot.rootObj != null)
            {
                slot.rootObj.SetActive(true); // Bật hình lên
                slot.rootObj.transform.SetAsLastSibling(); // Xếp nối đuôi nhau
                slot.isAssigned = true;
                
                if (slot.nameText != null) 
                    slot.nameText.text = player.PlayerName.ToString(); // Đổi tên thật
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