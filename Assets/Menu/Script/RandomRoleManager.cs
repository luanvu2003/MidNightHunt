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
    [Networked] public TickTimer TransitionTimer { get; set; }
    [Networked] public NetworkBool IsRoleAssigned { get; set; }
    private int lastTickSecond = -1;

    public override void Spawned()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.musicSource != null)
        {
            Debug.Log($"[Random] Đồng bộ âm lượng nhạc: {AudioManager.Instance.musicSource.volume}");
        }
        foreach (var txt in playerNameTexts) if (txt != null) txt.text = "";
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);
        if (resultText != null) resultText.text = "";
        lastTickSecond = -1;
        if (Runner.IsServer)
        {
            StartCoroutine(ServerLogicRoutine());
        }
    }
    IEnumerator ServerLogicRoutine()
    {
        yield return new WaitForSeconds(1f); 
        var allPlayers = FindObjectsOfType<RoomPlayer>().OrderBy(p => p.Object.InputAuthority.PlayerId).ToList();
        if (allPlayers.Count > 0)
        {
            Random.InitState(System.DateTime.Now.Millisecond + allPlayers.Count);
            int hunterIndex = Random.Range(0, allPlayers.Count);
            Debug.Log($"🎲 Server chốt: Người chơi [{hunterIndex}] làm Hunter!");
            for (int i = 0; i < allPlayers.Count; i++)
            {
                allPlayers[i].SetRoleByServer(i == hunterIndex);
            }
            string p0 = allPlayers.Count > 0 ? allPlayers[0].PlayerName.ToString() : "";
            string p1 = allPlayers.Count > 1 ? allPlayers[1].PlayerName.ToString() : "";
            string p2 = allPlayers.Count > 2 ? allPlayers[2].PlayerName.ToString() : "";
            string p3 = allPlayers.Count > 3 ? allPlayers[3].PlayerName.ToString() : "";
            int randomExtraSpins = Random.Range(51, 80);
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
        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            if (i < playerCount && playerNameTexts[i] != null)
            {
                playerNameTexts[i].text = names[i];
                playerNameTexts[i].color = normalColor;
            }
        }
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(true);
        int totalSpins = (playerCount * extraSpins) + winnerIndex;
        float delayTime = 0.05f;
        for (int i = 0; i <= totalSpins; i++)
        {
            for (int j = 0; j < playerCount; j++) playerNameTexts[j].color = normalColor;
            int currentIndex = i % playerCount;
            playerNameTexts[currentIndex].color = highlightColor;
            if (highlightFrame != null)
            {
                highlightFrame.position = playerNameTexts[currentIndex].rectTransform.position;
            }
            if (audioSource != null && spinSound != null)
            {
                audioSource.PlayOneShot(spinSound, 0.5f * GetVFXVolume());
            }
            int remainingSpins = totalSpins - i;
            if (remainingSpins < 3) delayTime += 0.2f;       
            else if (remainingSpins < 10) delayTime += 0.05f; 
            yield return new WaitForSeconds(delayTime);
        }
        playerNameTexts[winnerIndex].color = hunterColor;
        if (highlightFrame != null && highlightFrame.TryGetComponent<Image>(out Image frameImage))
        {
            frameImage.color = hunterColor;
        }
        if (audioSource != null && resultSound != null)
        {
            audioSource.PlayOneShot(resultSound, 1f * GetVFXVolume());
        }
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
                resultText.color = new Color(0.2f, 0.6f, 1f); 
            }
        }
        if (Runner.IsServer)
        {
            IsRoleAssigned = true;
            TransitionTimer = TickTimer.CreateFromSeconds(Runner, 10f); 
        }
    }    
    private void Update()
    {
        if (Object == null || !Object.IsValid) return;
        if (IsRoleAssigned && TransitionTimer.IsRunning && statusText != null)
        {
            int timeLeft = Mathf.CeilToInt(TransitionTimer.RemainingTime(Runner) ?? 0);
            statusText.text = $"Vào phòng chốt nhân vật sau: {timeLeft}s";
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
        if (IsRoleAssigned && Runner.IsServer && TransitionTimer.Expired(Runner))
        {
            TransitionTimer = TickTimer.None;
            Runner.LoadScene(SceneRef.FromIndex(3));
        }
    }
    private float GetVFXVolume()
    {
        if (AudioManager.Instance != null)
        {
            return AudioManager.Instance.vfxVolume;
        }
        return 1f;
    }
}