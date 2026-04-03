using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RandomRoleManager : NetworkBehaviour
{
    [Header("UI Hiển Thị")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI resultText;
    
    [Header("Danh Sách Tên Người Chơi")]
    [Tooltip("Kéo 4 cái TextMeshPro của 4 người chơi vào đây")]
    public TextMeshProUGUI[] playerNameTexts;

    [Header("Hiệu Ứng Vòng Quay")]
    [Tooltip("Kéo Object Khung (Image) xung quanh tên vào đây")]
    public RectTransform highlightFrame; // Thêm biến cho cái khung
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow; // Màu lúc đang chạy
    public Color hunterColor = Color.red;       // Màu chốt hạ Thợ Săn

    [Networked] public TickTimer TransitionTimer { get; set; }
    [Networked] public NetworkBool IsRoleAssigned { get; set; }

    public override void Spawned()
    {
        // Giấu hết tên đi lúc mới vào
        foreach (var txt in playerNameTexts) txt.text = "";
        
        // Giấu khung đi lúc mới vào
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);

        if (Runner.IsServer)
        {
            StartCoroutine(ServerLogicRoutine());
        }
    }

    IEnumerator ServerLogicRoutine()
    {
        yield return new WaitForSeconds(1f); // Đợi mọi người load Scene ổn định

        var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();

        if (allPlayers.Count > 0)
        {
            // 1. SERVER BỐC THĂM KẾT QUẢ TRƯỚC
            int hunterIndex = Random.Range(0, allPlayers.Count);

            // Gắn mác cho tụi nó
            for (int i = 0; i < allPlayers.Count; i++)
            {
                allPlayers[i].SetRoleByServer(i == hunterIndex);
            }

            // 2. LẤY TÊN ĐỂ GỬI CHO CÁC MÁY CON (Tối đa 4 người)
            string p0 = allPlayers.Count > 0 ? allPlayers[0].PlayerName.ToString() : "";
            string p1 = allPlayers.Count > 1 ? allPlayers[1].PlayerName.ToString() : "";
            string p2 = allPlayers.Count > 2 ? allPlayers[2].PlayerName.ToString() : "";
            string p3 = allPlayers.Count > 3 ? allPlayers[3].PlayerName.ToString() : "";

            // 3. RA LỆNH CHO TOÀN BỘ CÁC MÁY CHẠY HIỆU ỨNG VÒNG QUAY
            RPC_PlayRouletteEffect(hunterIndex, allPlayers.Count, p0, p1, p2, p3);
        }
    }

    // 🚨 LỆNH RPC: Chạy trên TẤT CẢ các máy cùng lúc
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayRouletteEffect(int winnerIndex, int playerCount, string n0, string n1, string n2, string n3)
    {
        // Bắt đầu Coroutine hiệu ứng hình ảnh
        StartCoroutine(VisualRouletteRoutine(winnerIndex, playerCount, new string[] { n0, n1, n2, n3 }));
    }

    IEnumerator VisualRouletteRoutine(int winnerIndex, int playerCount, string[] names)
    {
        if (statusText != null) statusText.text = "Hệ thống đang chọn thợ săn...";
        if (resultText != null) resultText.text = "";

        // Hiển thị tên người chơi lên bảng
        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            if (i < playerCount)
            {
                playerNameTexts[i].text = names[i];
                playerNameTexts[i].color = normalColor;
            }
        }

        // Bật khung lên
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(true);

        // THIẾT LẬP VÒNG QUAY NGẪU NHIÊN & LÂU HƠN
        // Random số vòng chạy lố từ 40 đến 60 vòng để không bị lặp lại quy luật
        int randomExtraSpins = Random.Range(40, 60); 
        int totalSpins = (playerCount * randomExtraSpins) + winnerIndex; 
        
        float delayTime = 0.1f; // Bắt đầu cực nhanh để tạo cảm giác xoay tít thò lò

        // CHẠY HIỆU ỨNG ÁNH SÁNG
        for (int i = 0; i <= totalSpins; i++)
        {
            // Trả tất cả về màu trắng
            for (int j = 0; j < playerCount; j++) playerNameTexts[j].color = normalColor;

            // Bật sáng người hiện tại
            int currentIndex = i % playerCount;
            playerNameTexts[currentIndex].color = highlightColor;

            // Di chuyển cái khung tới vị trí của text hiện tại
            if (highlightFrame != null)
            {
                highlightFrame.position = playerNameTexts[currentIndex].rectTransform.position;
            }

            // HIỆU ỨNG CHẬM DẦN ĐỀU (Khi gần đến đích)
            if (i > totalSpins - (playerCount * 3)) 
            {
                delayTime += 0.06f; // Rùa bò khúc cuối để tạo hồi hộp
            }
            else if (i > totalSpins - (playerCount * 6))
            {
                delayTime += 0.02f; // Bắt đầu hãm phanh
            }

            yield return new WaitForSeconds(delayTime);
        }

        // BÙM! CHỐT KẾT QUẢ NHÁY ĐỎ
        playerNameTexts[winnerIndex].color = hunterColor;
        playerNameTexts[winnerIndex].text = names[winnerIndex];
        
        // Đổi màu khung thành màu đỏ luôn cho ngầu (nếu khung có component Image)
        if (highlightFrame != null && highlightFrame.TryGetComponent<Image>(out Image frameImage))
        {
            frameImage.color = hunterColor;
        }

        // Show chữ tổ chảng giữa màn hình
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
                resultText.color = new Color(0.2f, 0.6f, 1f); // Xanh dương
            }
        }

        // Server bắt đầu đếm ngược 5s chuyển phòng
        if (Runner.IsServer)
        {
            IsRoleAssigned = true;
            TransitionTimer = TickTimer.CreateFromSeconds(Runner, 10f);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsRoleAssigned)
        {
            if (TransitionTimer.Expired(Runner))
            {
                if (Runner.IsServer)
                {
                    TransitionTimer = TickTimer.None;
                    Runner.LoadScene(SceneRef.FromIndex(3)); 
                }
            }
            else if (TransitionTimer.IsRunning)
            {
                if (statusText != null)
                {
                    int timeLeft = Mathf.CeilToInt(TransitionTimer.RemainingTime(Runner) ?? 0);
                    statusText.text = $"Vào phòng chốt nhân vật sau: {timeLeft}s";
                }
            }
        }
    }
}