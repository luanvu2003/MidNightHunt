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

        [Header("Thông tin cơ bản")]
        public Image playerImage;        // Hình ảnh đại diện nhân vật
        public TextMeshProUGUI nameText; // Tên người chơi

        [Header("Trạng Thái Máu (Status)")]
        public Image statusIcon;         // Hình ảnh sẽ thay đổi khi bị Hit/Gục

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
    public Sprite iconHealthy; // (Tùy chọn) Bình thường, không bị gì
    public Sprite iconHit1;    // Bị chém 1 nhát
    public Sprite iconHit2;    // Bị chém 2 nhát
    public Sprite iconDowned;  // Nằm gục (3 nhát)
    public Sprite iconDead;    // 🚨 THÊM MỚI: Icon đã chết (Cái sọ người / Dấu X)

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

    // ==========================================
    // LOGIC CẬP NHẬT MÓC & ICON TRẠNG THÁI
    // ==========================================
    private void UpdatePlayerUIStates(List<RoomPlayer> allPlayers)
    {
        // 1. KIỂM TRA XEM NGƯỜI ĐANG NHÌN MÀN HÌNH NÀY CÓ PHẢI LÀ HUNTER KHÔNG?
        bool isLocalHunter = false;
        var localRoomPlayer = allPlayers.FirstOrDefault(p => p.Object.HasInputAuthority);
        if (localRoomPlayer != null && localRoomPlayer.IsHunter)
        {
            isLocalHunter = true;
        }

        foreach (var slot in playerSlots)
        {
            if (!slot.isAssigned || slot.rootObj == null) continue;

            // 🚨 KIỂM TRA OBJECT NHÂN VẬT CÒN TỒN TẠI KHÔNG (Ép kiểu sang MonoBehaviour để check null chuẩn của Unity)
            bool isSurvivorDestroyed = (slot.linkedSurvivor as MonoBehaviour) == null;

            if (isSurvivorDestroyed)
            {
                // Thử tìm lại lần nữa phòng trường hợp nhân vật mới Spawn
                FindLinkedSurvivor(slot, allPlayers);
                isSurvivorDestroyed = (slot.linkedSurvivor as MonoBehaviour) == null;
            }

            // 🚨 NẾU NHÂN VẬT THẬT SỰ ĐÃ BỊ XÓA (DESPAWN DO CHẾT)
            if (isSurvivorDestroyed)
            {
                // Tắt hoàn toàn overlay móc để không bị kẹt số 0
                if (slot.hookOverlay != null) slot.hookOverlay.gameObject.SetActive(false);
                if (slot.hookCooldownText != null) slot.hookCooldownText.gameObject.SetActive(false);

                // Hiển thị Icon đã chết
                if (slot.statusIcon != null)
                {
                    slot.statusIcon.gameObject.SetActive(true);
                    slot.statusIcon.color = Color.white;
                    if (iconDead != null) 
                        slot.statusIcon.sprite = iconDead;
                    else 
                        slot.statusIcon.gameObject.SetActive(false);
                }
                
                // Bỏ qua đoạn code tính móc ở dưới vì người này bay màu rồi
                continue; 
            }

            // ==========================================
            // NẾU NHÂN VẬT VẪN CÒN SỐNG THÌ CHẠY TIẾP XUỐNG ĐÂY
            // ==========================================
            if (slot.linkedSurvivor != null)
            {
                bool isHooked = slot.linkedSurvivor.GetIsHooked();
                bool isDowned = slot.linkedSurvivor.GetIsDowned();
                int hits = GetCurrentHitsSafe(slot.linkedSurvivor); // Lấy số Hit hiện tại

                // ----------------------------------------------------
                // A. LOGIC THANH MÓC (HUNTER SẼ KHÔNG ĐƯỢC THẤY)
                // ----------------------------------------------------
                bool showHookUI = isHooked && !isLocalHunter;

                if (slot.hookOverlay != null) slot.hookOverlay.gameObject.SetActive(showHookUI);
                if (slot.hookCooldownText != null) slot.hookCooldownText.gameObject.SetActive(showHookUI);

                if (showHookUI)
                {
                    float timeLeft = slot.linkedSurvivor.GetSacrificeTimer();
                    slot.hookCooldownText.text = Mathf.Ceil(timeLeft).ToString();
                    slot.hookOverlay.fillAmount = timeLeft / maxHookTime;
                }

                // ----------------------------------------------------
                // B. LOGIC ĐỔI ICON THEO SỐ HIT / GỤC
                // ----------------------------------------------------
                if (slot.statusIcon != null)
                {
                    // NẾU BỊ TREO MÓC: Tắt luôn cái Icon Trạng Thái đi cho đỡ rác UI
                    if (isHooked)
                    {
                        slot.statusIcon.gameObject.SetActive(false);
                    }
                    else
                    {
                        slot.statusIcon.gameObject.SetActive(true);
                        slot.statusIcon.color = Color.white; // Ép màu trắng với Alpha 100% để chống tàng hình

                        if (isDowned && iconDowned != null)
                            slot.statusIcon.sprite = iconDowned;
                        else if (hits >= 2 && iconHit2 != null)
                            slot.statusIcon.sprite = iconHit2;
                        else if (hits == 1 && iconHit1 != null)
                            slot.statusIcon.sprite = iconHit1;
                        else
                        {
                            // Chưa bị chém cái nào (Khỏe mạnh)
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

    // Hàm lấy số Hit an toàn cho tất cả các loại Nhân Vật mà không cần sửa interface ISurvivor
    private int GetCurrentHitsSafe(ISurvivor survivor)
    {
        if (survivor is IShowSpeedController_Fusion speed) return speed.CurrentHits;
        if (survivor is NurseController_Fusion nurse) return nurse.CurrentHits;
        if (survivor is MrBeanController_Fusion bean) return bean.CurrentHits;
        return 0; // Trả về 0 nếu không lỗi/khỏe mạnh
    }
}