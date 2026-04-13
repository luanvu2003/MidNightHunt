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
                // 🚨 ĐÃ XÓA DÒNG CHẶN ID = 0 Ở ĐÂY ĐỂ Y TÁ CÓ THỂ XUẤT HIỆN
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
        foreach (var slot in survivorSlots) 
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
            slot.isAssigned = false;
        }

        foreach (var player in survivors)
        {
            int charID = player.CharacterID;
            // 🚨 ĐÃ XÓA DÒNG CHẶN ID = 0 Ở ĐÂY 

            var slot = survivorSlots.FirstOrDefault(s => s.characterID == charID);
            
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