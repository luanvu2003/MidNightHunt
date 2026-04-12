using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class RandomRoleManager : NetworkBehaviour
{
    [Header("== UI HIỂN THỊ ==")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI resultText;

    [Header("== DANH SÁCH NGƯỜI CHƠI ==")]
    public TextMeshProUGUI[] playerNameTexts;

    [Header("== HIỆU ỨNG VÒNG QUAY ==")]
    public RectTransform highlightFrame;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color hunterColor = Color.red;

    [Header("== ÂM THANH (AUDIO) ==")]
    public AudioSource audioSource;
    [Tooltip("Tiếng tạch tạch khi khung viền nhảy qua từng người")]
    public AudioClip spinSound;
    [Tooltip("Tiếng BÙM chốt kết quả (Dùng chung cho Hunter & Survivor)")]
    public AudioClip resultSound;
    [Tooltip("Tiếng Tít đếm ngược 10 giây cuối")]
    public AudioClip tickSound;

    // Các biến Mạng đồng bộ
    [Networked] public TickTimer TransitionTimer { get; set; }
    [Networked] public NetworkBool IsRoleAssigned { get; set; }

    // Biến phụ để canh me tiếng Tít đếm ngược
    private int lastTickSecond = -1;

    public override void Spawned()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.musicSource != null)
        {
            // Nếu Scene này có nhạc nền riêng, bạn nên gán nó vào AudioManager.Instance.musicSource
            // Còn nếu dùng chung nhạc từ Menu thì nó sẽ tự giữ mức âm lượng cũ.
            Debug.Log($"[Random] Đồng bộ âm lượng nhạc: {AudioManager.Instance.musicSource.volume}");
        }
        // 1. Dọn dẹp UI lúc mới vào
        foreach (var txt in playerNameTexts) if (txt != null) txt.text = "";
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);
        if (resultText != null) resultText.text = "";
        lastTickSecond = -1;

        // 2. Chỉ Server mới được quyền quay số
        if (Runner.IsServer)
        {
            StartCoroutine(ServerLogicRoutine());
        }
    }

    IEnumerator ServerLogicRoutine()
    {
        yield return new WaitForSeconds(1f); // Đợi mọi người load Scene xong

        // Ép danh sách đồng bộ theo PlayerId để không bị lệch thứ tự giữa các máy
        var allPlayers = FindObjectsOfType<RoomPlayer>().OrderBy(p => p.Object.InputAuthority.PlayerId).ToList();

        if (allPlayers.Count > 0)
        {
            // Lắc xí ngầu lấy hạt giống thời gian thực để chống trùng lặp
            Random.InitState(System.DateTime.Now.Millisecond + allPlayers.Count);

            // Chốt người làm Hunter
            int hunterIndex = Random.Range(0, allPlayers.Count);
            Debug.Log($"🎲 Server chốt: Người chơi [{hunterIndex}] làm Hunter!");

            for (int i = 0; i < allPlayers.Count; i++)
            {
                allPlayers[i].SetRoleByServer(i == hunterIndex);
            }

            // Lấy tên 4 người (Nếu thiếu người thì để trống)
            string p0 = allPlayers.Count > 0 ? allPlayers[0].PlayerName.ToString() : "";
            string p1 = allPlayers.Count > 1 ? allPlayers[1].PlayerName.ToString() : "";
            string p2 = allPlayers.Count > 2 ? allPlayers[2].PlayerName.ToString() : "";
            string p3 = allPlayers.Count > 3 ? allPlayers[3].PlayerName.ToString() : "";

            // Chọn số vòng quay phụ (từ 50 đến 80 bước) để tạo cảm giác quay ngẫu nhiên
            int randomExtraSpins = Random.Range(51, 80);

            // Phóng lệnh RPC báo TẤT CẢ các máy bắt đầu bật hiệu ứng vòng quay
            RPC_PlayRouletteEffect(hunterIndex, allPlayers.Count, p0, p1, p2, p3, randomExtraSpins);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayRouletteEffect(int winnerIndex, int playerCount, string n0, string n1, string n2, string n3, int extraSpins)
    {
        StartCoroutine(VisualRouletteRoutine(winnerIndex, playerCount, new string[] { n0, n1, n2, n3 }, extraSpins));
    }

    IEnumerator VisualRouletteRoutine(int winnerIndex, int playerCount, string[] names, int extraSpins)
    {
        if (statusText != null) statusText.text = "Hệ thống đang chọn thợ săn...";

        // Gắn tên lên UI
        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            if (i < playerCount && playerNameTexts[i] != null)
            {
                playerNameTexts[i].text = names[i];
                playerNameTexts[i].color = normalColor;
            }
        }

        if (highlightFrame != null) highlightFrame.gameObject.SetActive(true);

        // Tính toán tổng số bước nhảy sao cho điểm dừng cuối cùng khớp với winnerIndex
        int totalSpins = (playerCount * extraSpins) + winnerIndex;
        float delayTime = 0.05f;

        // 🟢 VÒNG LẶP QUAY SỐ
        for (int i = 0; i <= totalSpins; i++)
        {
            for (int j = 0; j < playerCount; j++) playerNameTexts[j].color = normalColor;

            int currentIndex = i % playerCount;
            playerNameTexts[currentIndex].color = highlightColor;

            // Di chuyển khung viền
            if (highlightFrame != null)
            {
                highlightFrame.position = playerNameTexts[currentIndex].rectTransform.position;
            }

            // 🎵 PHÁT ÂM THANH QUAY (Spin)
            if (audioSource != null && spinSound != null)
            {
                // Nhân 0.5f với âm lượng cài đặt
                audioSource.PlayOneShot(spinSound, 0.5f * GetVFXVolume());
            }

            // Hiệu ứng chậm dần đều
            int remainingSpins = totalSpins - i;
            if (remainingSpins < 3) delayTime += 0.2f;       // 3 ô cuối cực chậm
            else if (remainingSpins < 10) delayTime += 0.05f; // 10 ô cuối chậm dần

            yield return new WaitForSeconds(delayTime);
        }

        // 🔴 CHỐT KẾT QUẢ!
        playerNameTexts[winnerIndex].color = hunterColor;
        if (highlightFrame != null && highlightFrame.TryGetComponent<Image>(out Image frameImage))
        {
            frameImage.color = hunterColor;
        }

        // 🎵 PHÁT ÂM THANH CHỐT (Dùng chung cho cả 2 phe)
        if (audioSource != null && resultSound != null)
        {
            audioSource.PlayOneShot(resultSound, 1f * GetVFXVolume());
        }

        // Hiện chữ to báo cho người chơi biết họ là phe nào
        if (RoomPlayer.Local != null && resultText != null)
        {
            if (RoomPlayer.Local.IsHunter)
            {
                resultText.text = "BẠN LÀ:\n<size=150%>HUNTER</size>";
                resultText.color = hunterColor;
            }
            else
            {
                resultText.text = "BẠN LÀ:\n<size=150%>SURVIVOR</size>";
                resultText.color = new Color(0.2f, 0.6f, 1f); // Xanh dương cho Survivor
            }
        }

        // Server bắt đầu đếm ngược 10s để sang Scene tiếp theo
        if (Runner.IsServer)
        {
            IsRoleAssigned = true;
            TransitionTimer = TickTimer.CreateFromSeconds(Runner, 10f); // Đúng 10 giây
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid) return;

        // Xử lý UI và tiếng TÍP đếm ngược
        if (IsRoleAssigned && TransitionTimer.IsRunning && statusText != null)
        {
            int timeLeft = Mathf.CeilToInt(TransitionTimer.RemainingTime(Runner) ?? 0);
            statusText.text = $"Vào phòng chốt nhân vật sau: {timeLeft}s";

            // 🎵 PHÁT ÂM THANH ĐẾM NGƯỢC (Mỗi giây kêu 1 lần)
            if (timeLeft != lastTickSecond && timeLeft <= 10 && timeLeft > 0)
            {
                lastTickSecond = timeLeft;
                if (audioSource != null && tickSound != null)
                {
                    audioSource.PlayOneShot(tickSound, 0.7f * GetVFXVolume());
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Khi đồng hồ mạng hết giờ, Server sẽ lôi cả đám sang Scene 3 (Chọn tướng)
        if (IsRoleAssigned && Runner.IsServer && TransitionTimer.Expired(Runner))
        {
            TransitionTimer = TickTimer.None;
            Runner.LoadScene(SceneRef.FromIndex(3));
        }
    }
    // HÀM MỚI: Lấy âm lượng VFX từ AudioManager (Nếu không có thì mặc định là 100%)
    private float GetVFXVolume()
    {
        if (AudioManager.Instance != null)
        {
            return AudioManager.Instance.vfxVolume;
        }
        return 1f; // Chạy test 1 Scene không qua Menu thì nó vẫn kêu
    }
}