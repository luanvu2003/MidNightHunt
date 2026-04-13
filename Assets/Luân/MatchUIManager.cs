using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

// 🚨 ĐÃ ĐỔI TỪ NetworkBehaviour thành MonoBehaviour
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
    }

    public SurvivorUISlot[] survivorSlots;
    private bool _isSetup = false;
    private float maxHookTime = 90f; // Thời gian treo móc tối đa

    private void Start()
    {
        // 1. Vừa vào game: Tắt sạch sẽ tất cả 4 khung Avatar đi
        foreach (var slot in survivorSlots)
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
        }
    }

    // 🚨 SỬ DỤNG HÀM UPDATE CỦA UNITY ĐỂ LÚC NÀO CŨNG CHẠY ĐƯỢC MÀ KHÔNG CẦN CHỜ FUSION
    private void Update()
    {
        // 2. Chờ mọi người load xong thì Bật đúng những khung đang có người chơi
        if (!_isSetup)
        {
            var roomPlayers = FindObjectsOfType<RoomPlayer>().ToList();
            if (roomPlayers.Count > 0) SetupUI(roomPlayers);
            return;
        }

        // 3. Liên tục cập nhật trạng thái Móc cho từng Avatar
        foreach (var slot in survivorSlots)
        {
            if (!slot.rootObj.activeSelf) continue;

            if (slot.linkedSurvivor == null || ((Component)slot.linkedSurvivor).gameObject == null)
            {
                FindLinkedSurvivor(slot);
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
                        slot.hookCooldownText.text = Mathf.Ceil(timeLeft).ToString() + "s";
                    
                    // 🚨 Đã đảm bảo tỷ lệ là timeLeft / maxHookTime (chạy từ 1 về 0)
                    if (slot.hookOverlay != null) 
                        slot.hookOverlay.fillAmount = timeLeft / maxHookTime; 
                }
            }
        }
    }

    private void SetupUI(System.Collections.Generic.List<RoomPlayer> players)
    {
        foreach (var player in players)
        {
            if (player.IsHunter) continue;

            int charID = player.CharacterID;
            var slot = survivorSlots.FirstOrDefault(s => s.characterID == charID);
            
            if (slot != null && slot.rootObj != null)
            {
                slot.rootObj.SetActive(true); 
                slot.rootObj.transform.SetAsLastSibling(); 
                
                if (slot.nameText != null) 
                    slot.nameText.text = player.PlayerName.ToString(); 
            }
        }
        _isSetup = true;
    }

    private void FindLinkedSurvivor(SurvivorUISlot slot)
    {
        var targetRoomPlayer = FindObjectsOfType<RoomPlayer>().FirstOrDefault(p => !p.IsHunter && p.CharacterID == slot.characterID);
        
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