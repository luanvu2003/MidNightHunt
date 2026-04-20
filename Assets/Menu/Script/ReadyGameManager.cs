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
    [Header("UI Cảnh Báo")]
    public TextMeshProUGUI warningText;
    private bool _isTransitioning = false;
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
        if (warningText != null) warningText.gameObject.SetActive(false); 
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
            if (RoomPlayer.Local.CharacterID >= 0)
            {
                RoomPlayer.Local.RPC_ToggleReady();
                if (warningText != null) warningText.gameObject.SetActive(false);
                if (!RoomPlayer.Local.IsReady)
                {
                    if (readyParticle != null) readyParticle.Play();
                    if (readyAudio != null) readyAudio.Play();
                }
            }
            else
            {
                if (warningText != null)
                {
                    warningText.text = "Bạn chưa chọn nhân vật!";
                    warningText.gameObject.SetActive(true);
                    StopAllCoroutines();
                    StartCoroutine(HideWarningRoutine());
                }
            }
        }
    }    IEnumerator HideWarningRoutine()
    {
        yield return new WaitForSeconds(3f);
        if (warningText != null) warningText.gameObject.SetActive(false);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowLoadingScreen()
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.ShowLoading();
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;
        var allPlayers = FindObjectsOfType<RoomPlayer>().ToList();
        bool isAllReady = allPlayers.Count > 0 && allPlayers.All(p => p.IsReady);
        if ((isAllReady || SelectionTimer.Expired(Runner)) && !_isTransitioning)
        {
            if (Runner.IsServer)
            {
                _isTransitioning = true;
                SelectionTimer = TickTimer.None;
                RPC_ShowLoadingScreen();
                StartCoroutine(DelayLoadSceneRoutine());
            }
        }
    }
    private IEnumerator DelayLoadSceneRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        Debug.Log("🚀 Bắt đầu chuyển cảnh sang HBao_Map...");
        Runner.LoadScene(SceneRef.FromIndex(4));
    }
}