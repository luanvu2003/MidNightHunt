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
        public GameObject rootObj; 
        [Header("Thông tin cơ bản")]
        public Image playerImage;      
        public TextMeshProUGUI nameText; 
        [Header("Trạng Thái Máu (Status)")]
        public Image statusIcon;        

        [Header("Hook Cooldown")]
        public Image hookOverlay;
        public TextMeshProUGUI hookCooldownText;

        [HideInInspector] public ISurvivor linkedSurvivor;
        [HideInInspector] public bool isAssigned;
        [HideInInspector] public int lastAssignedPlayerID = -1;
    }

    [Header("Cài đặt UI Khu vực người chơi")]
    public RectTransform slotContainer;
    public PlayerUISlot[] playerSlots;

    [Header("Dữ liệu hình ảnh Avatar (Theo CharacterID)")]
    public Sprite[] characterAvatars;

    [Header("🚨 CÁC ICON TRẠNG THÁI (Máu / Gục / Chết)")]
    public Sprite iconHealthy; 
    public Sprite iconHit1;    
    public Sprite iconHit2;    
    public Sprite iconDowned;  
    public Sprite iconDead;   
    private float maxHookTime = 90f;
    private void Start()
    {
        foreach (var slot in playerSlots)
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
        }
    }
    private void Update()
    {
        var roomPlayers = FindObjectsOfType<RoomPlayer>().Where(p => p.Object != null && p.Object.IsValid).ToList();
        var survivors = roomPlayers.Where(p => !p.IsHunter).OrderBy(p => p.Object.InputAuthority.PlayerId).ToList();
        if (CheckIfNeedsRefresh(survivors))
        {
            SetupUI(survivors);

            if (slotContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer);
            }
        }
        UpdatePlayerUIStates(roomPlayers);
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
        foreach (var slot in playerSlots)
        {
            if (slot.rootObj != null) slot.rootObj.SetActive(false);
            slot.isAssigned = false;
            slot.lastAssignedPlayerID = -1;
            slot.linkedSurvivor = null;
        }

        foreach (var player in survivors)
        {
            var slot = playerSlots.FirstOrDefault(s => !s.isAssigned);
            if (slot != null && slot.rootObj != null)
            {
                slot.rootObj.SetActive(true);
                slot.rootObj.transform.SetAsLastSibling();
                slot.isAssigned = true;
                slot.lastAssignedPlayerID = player.Object.InputAuthority.PlayerId;
                if (slot.nameText == null) slot.nameText = slot.rootObj.GetComponentInChildren<TextMeshProUGUI>(true);
                if (slot.playerImage == null) slot.playerImage = slot.rootObj.GetComponentInChildren<Image>(true);
                if (slot.nameText != null)
                {
                    string pName = player.PlayerName.ToString();
                    slot.nameText.text = string.IsNullOrEmpty(pName) ? "Player..." : pName;
                }

                if (slot.playerImage != null)
                {
                    int charID = player.CharacterID;
                    if (charID >= 0 && charID < characterAvatars.Length)
                    {
                        slot.playerImage.sprite = characterAvatars[charID];
                    }
                }
            }
        }
    }
    private void UpdatePlayerUIStates(List<RoomPlayer> allPlayers)
    {
        bool isLocalHunter = false;
        var localRoomPlayer = allPlayers.FirstOrDefault(p => p.Object.HasInputAuthority);
        if (localRoomPlayer != null && localRoomPlayer.IsHunter)
        {
            isLocalHunter = true;
        }

        foreach (var slot in playerSlots)
        {
            if (!slot.isAssigned || slot.rootObj == null) continue;
            bool isSurvivorDestroyed = (slot.linkedSurvivor as MonoBehaviour) == null;
            if (isSurvivorDestroyed)
            {
                FindLinkedSurvivor(slot, allPlayers);
                isSurvivorDestroyed = (slot.linkedSurvivor as MonoBehaviour) == null;
            }
            if (isSurvivorDestroyed)
            {
                if (slot.hookOverlay != null) slot.hookOverlay.gameObject.SetActive(false);
                if (slot.hookCooldownText != null) slot.hookCooldownText.gameObject.SetActive(false);
                if (slot.statusIcon != null)
                {
                    slot.statusIcon.gameObject.SetActive(true);
                    slot.statusIcon.color = Color.white;
                    if (iconDead != null) 
                        slot.statusIcon.sprite = iconDead;
                    else 
                        slot.statusIcon.gameObject.SetActive(false);
                }
                continue; 
            }
            if (slot.linkedSurvivor != null)
            {
                bool isHooked = slot.linkedSurvivor.GetIsHooked();
                bool isDowned = slot.linkedSurvivor.GetIsDowned();
                int hits = GetCurrentHitsSafe(slot.linkedSurvivor); 
                bool showHookUI = isHooked && !isLocalHunter;
                if (slot.hookOverlay != null) slot.hookOverlay.gameObject.SetActive(showHookUI);
                if (slot.hookCooldownText != null) slot.hookCooldownText.gameObject.SetActive(showHookUI);
                if (showHookUI)
                {
                    float timeLeft = slot.linkedSurvivor.GetSacrificeTimer();
                    slot.hookCooldownText.text = Mathf.Ceil(timeLeft).ToString();
                    slot.hookOverlay.fillAmount = timeLeft / maxHookTime;
                }
                if (slot.statusIcon != null)
                {
                    if (isHooked)
                    {
                        slot.statusIcon.gameObject.SetActive(false);
                    }
                    else
                    {
                        slot.statusIcon.gameObject.SetActive(true);
                        slot.statusIcon.color = Color.white; 
                        if (isDowned && iconDowned != null)
                            slot.statusIcon.sprite = iconDowned;
                        else if (hits >= 2 && iconHit2 != null)
                            slot.statusIcon.sprite = iconHit2;
                        else if (hits == 1 && iconHit1 != null)
                            slot.statusIcon.sprite = iconHit1;
                        else
                        {
                            if (iconHealthy != null)
                                slot.statusIcon.sprite = iconHealthy;
                            else
                                slot.statusIcon.gameObject.SetActive(false);
                        }
                    }
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
    private int GetCurrentHitsSafe(ISurvivor survivor)
    {
        if (survivor is IShowSpeedController_Fusion speed) return speed.CurrentHits;
        if (survivor is NurseController_Fusion nurse) return nurse.CurrentHits;
        if (survivor is MrBeanController_Fusion bean) return bean.CurrentHits;
        return 0; 
    }
}