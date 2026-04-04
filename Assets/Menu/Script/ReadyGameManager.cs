using Fusion;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections; // Nhớ thêm dòng này để dùng Coroutine

public class ReadyGameManager : NetworkBehaviour
{
    [Header("UI Sẵn Sàng Cá Nhân")]
    public TextMeshProUGUI readyButtonText;
    public TextMeshProUGUI timerText;

    // 🚨 THÊM MỚI: Text cảnh báo khi quên chọn tướng
    [Header("UI Cảnh Báo")]
    public TextMeshProUGUI warningText; 

    [Header("Hiệu Ứng Cá Nhân")]
    public GameObject readyOverlayObject; 
    public ParticleSystem readyParticle; 
    public AudioSource readyAudio;       

    [Header("Đồng Bộ Sẵn Sàng (4 Người)")]
    public GameObject[] readyIndicators; 

    [Networked] public TickTimer SelectionTimer { get; set; }

    public override void Spawned()
    {
        if (readyOverlayObject != null) readyOverlayObject.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false); // Giấu cảnh báo lúc mới vào

        foreach (var indicator in readyIndicators) if (indicator != null) indicator.SetActive(false);

        if (Runner.IsServer)
        {
            SelectionTimer = TickTimer.CreateFromSeconds(Runner, 60f);
        }
    }

    private void Update()
    {
        if (RoomPlayer.Local == null || Object == null || !Object.IsValid) return;

        if (readyButtonText != null)
        {
            readyButtonText.text = RoomPlayer.Local.IsReady ? "ĐÃ CHỐT" : "";
            readyButtonText.color = RoomPlayer.Local.IsReady ? Color.green : Color.white;
        }

        if (readyOverlayObject != null) readyOverlayObject.SetActive(RoomPlayer.Local.IsReady);

        var allPlayers = FindObjectsOfType<RoomPlayer>().OrderBy(p => p.Object.InputAuthority.PlayerId).ToList();

        for (int i = 0; i < readyIndicators.Length; i++)
        {
            if (readyIndicators[i] != null)
            {
                if (i < allPlayers.Count) readyIndicators[i].SetActive(allPlayers[i].IsReady);
                else readyIndicators[i].SetActive(false);
            }
        }

        if (SelectionTimer.IsRunning && timerText != null)
        {
            int timeLeft = Mathf.CeilToInt(SelectionTimer.RemainingTime(Runner) ?? 0);
            timerText.text = $"Vào game sau: {timeLeft}s";
        }
    }

    public void OnClickToggleReady()
    {
        if (RoomPlayer.Local != null)
        {
            // Kiểm tra xem đã chọn tướng chưa (ID >= 0)
            if (RoomPlayer.Local.CharacterID >= 0)
            {
                RoomPlayer.Local.RPC_ToggleReady();
                
                // Tắt cảnh báo nếu lỡ hiện
                if (warningText != null) warningText.gameObject.SetActive(false);

                if (!RoomPlayer.Local.IsReady) 
                {
                    if (readyParticle != null) readyParticle.Play();
                    if (readyAudio != null) readyAudio.Play();
                }
            }
            else
            {
                // 🚨 BẬT CẢNH BÁO NẾU CHƯA CHỌN TƯỚNG
                if (warningText != null)
                {
                    warningText.text = "Bạn chưa chọn nhân vật!";
                    warningText.gameObject.SetActive(true);
                    
                    // Tắt cảnh báo đang chạy cũ (nếu có) và chạy cái mới để đếm lại 3 giây
                    StopAllCoroutines(); 
                    StartCoroutine(HideWarningRoutine());
                }
            }
        }
    }

    // 🚨 HÀM TỰ ĐỘNG TẮT CẢNH BÁO SAU 3 GIÂY
    IEnumerator HideWarningRoutine()
    {
        yield return new WaitForSeconds(3f);
        if (warningText != null) warningText.gameObject.SetActive(false);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return; 

        var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();
        
        bool isAllReady = allPlayers.Count > 0 && allPlayers.All(p => p.IsReady);

        if (isAllReady || SelectionTimer.Expired(Runner))
        {
            SelectionTimer = TickTimer.None; 
            Runner.LoadScene(SceneRef.FromIndex(4)); 
        }
    }
}