using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class ChatSystem : NetworkBehaviour
{
    [Header("Giao diện")]
    public GameObject pannelcon;
    public Button iconChat;
    public TextMeshProUGUI textDisplay;
    public TMP_InputField inputField;
    public Button buttonSend;
    public ScrollRect scrollRect;

    private string _myName;
    private static string _globalChatHistory = "";

    public override void Spawned()
    {
        // Chỉ chạy trên máy của chính mình (Local Player)
        if (!HasStateAuthority) return;

        // Tự động kết nối các thành phần UI dựa trên tên bạn đã đặt
        FindUIElements();
        UpdateChatDisplay();

        _myName = "Người chơi " + Runner.LocalPlayer.PlayerId;

        // Gán sự kiện bấm nút
        iconChat.onClick.AddListener(ToggleChatPanel);
        buttonSend.onClick.AddListener(SendMessageChat);

        // Kiểm tra quyền: Hunter ở scene "PlayGame" sẽ không thấy chat
        CheckPermissions();
    }

    void FindUIElements()
    {
        pannelcon = GameObject.Find("Pannelcon");
        iconChat = GameObject.Find("IconChat").GetComponent<Button>();
        textDisplay = GameObject.Find("Text_Mesage").GetComponent<TextMeshProUGUI>();
        inputField = GameObject.Find("InputField (TMP)").GetComponent<TMP_InputField>();
        buttonSend = GameObject.Find("Button_SEND").GetComponent<Button>();
        scrollRect = GameObject.Find("PanelMesage").GetComponent<ScrollRect>();
    }

    void CheckPermissions()
    {
        // Giả sử Hunter có PlayerId là 0 hoặc dựa vào logic role của bạn
        bool isHunter = (Runner.LocalPlayer.PlayerId == 0);
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "HBao_Map" && isHunter)
        {
            GameObject.Find("Pannel_Chat").SetActive(false); // Ẩn hoàn toàn chat với Hunter
        }
    }

    public void ToggleChatPanel()
    {
        pannelcon.SetActive(!pannelcon.activeSelf);
        if (pannelcon.activeSelf)
        {
            inputField.ActivateInputField();
            StartCoroutine(ScrollToBottom());
        }
    }

    public void SendMessageChat()
    {
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        // Gửi qua mạng cho tất cả mọi người
        Rpc_SendChat(_myName, inputField.text);

        inputField.text = "";
        inputField.ActivateInputField();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SendChat(string user, string message)
    {
        string time = DateTime.Now.ToString("HH:mm");
        string formatted = $"[{time}] <color=yellow>{user}:</color> {message}\n";

        // Lưu vào bộ nhớ tạm toàn cục
        _globalChatHistory += formatted;

        // Hiển thị lên UI hiện tại
        UpdateChatDisplay();
    }
    private void UpdateChatDisplay()
    {
        if (textDisplay != null)
        {
            textDisplay.text = _globalChatHistory;
            StartCoroutine(ScrollToBottom());
        }
    }
    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame(); // Đợi UI render xong
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f; // Kéo thanh cuộn xuống đáy
    }
}