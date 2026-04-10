using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // 🚨 THÊM THƯ VIỆN NÀY ĐỂ DÙNG INPUT ACTIONS

public class ChatSystem : NetworkBehaviour
{
    [Header("UI References")]
    private GameObject _pannelChatRoot;
    private GameObject _pannelcon;
    private TextMeshProUGUI _textDisplay;
    private TMP_InputField _inputField;
    private Button _buttonSend;
    private Button _iconChat;
    private ScrollRect _scrollRect;

    // Biến static để lưu lịch sử chat xuyên Scene
    private static string _globalChatHistory = "";

    // 🚨 KHAI BÁO BIẾN CHO INPUT ACTIONS
    private InputAction _toggleAction;
    private InputAction _sendAction;

    public override void Spawned()
    {
        if (!HasInputAuthority) return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        FindAndSetupUI();

        // ==========================================
        // 🛠️ SETUP INPUT ACTIONS BẰNG CODE
        // ==========================================
        // 1. Tạo hành động bật/tắt chat gán vào nút Tab
        _toggleAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/tab");
        
        // 2. Tạo hành động gửi tin nhắn gán vào nút Enter (Thêm cả Enter bên cụm phím số)
        _sendAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/enter");
        _sendAction.AddBinding("<Keyboard>/numpadEnter");

        // 3. Đăng ký sự kiện: Khi phím được bấm (performed) thì chạy hàm tương ứng
        _toggleAction.performed += ctx => OnToggleKeyPressed();
        _sendAction.performed += ctx => OnSendKeyPressed();

        // 4. Bật Input lên để lắng nghe
        _toggleAction.Enable();
        _sendAction.Enable();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // 🚨 DỌN DẸP INPUT ACTIONS KHI THOÁT ĐỂ TRÁNH LỖI BỘ NHỚ
            if (_toggleAction != null)
            {
                _toggleAction.Disable();
                _toggleAction.Dispose();
            }
            if (_sendAction != null)
            {
                _sendAction.Disable();
                _sendAction.Dispose();
            }
        }
    }

    // ==========================================
    // 🎮 CÁC HÀM XỬ LÝ SỰ KIỆN TỪ INPUT ACTIONS
    // ==========================================
    private void OnToggleKeyPressed()
    {
        if (!HasInputAuthority) return;
        
        // Nếu không bị chặn (ví dụ Hunter) thì mới được bật
        if (_pannelChatRoot != null && _pannelChatRoot.activeSelf)
        {
            ToggleChat();
        }
    }

    private void OnSendKeyPressed()
    {
        if (!HasInputAuthority) return;

        // Chỉ được gửi khi cái bảng chat đang mở
        if (_pannelcon != null && _pannelcon.activeSelf)
        {
            SendMessageChat();
        }
    }

    // ==========================================
    // CÁC HÀM UI CŨ BÊN DƯỚI GIỮ NGUYÊN
    // ==========================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndSetupUI();
    }

    private void FindAndSetupUI()
    {
        _pannelChatRoot = GameObject.Find("Pannel_Chat");
        if (_pannelChatRoot == null) return;

        Transform iconChatTransform = _pannelChatRoot.transform.Find("IconChat");
        _iconChat = iconChatTransform.GetComponent<Button>();

        _pannelcon = iconChatTransform.Find("Pannelcon").gameObject;

        _buttonSend = _pannelcon.transform.Find("Button_SEND").GetComponent<Button>();
        _inputField = _pannelcon.transform.Find("InputField (TMP)").GetComponent<TMP_InputField>();
        _scrollRect = _pannelcon.transform.Find("PanelMesage").GetComponent<ScrollRect>();
        _textDisplay = _pannelcon.transform.Find("PanelMesage/Text_Mesage").GetComponent<TextMeshProUGUI>();

        _iconChat.onClick.RemoveAllListeners();
        _iconChat.onClick.AddListener(ToggleChat);

        _buttonSend.onClick.RemoveAllListeners();
        _buttonSend.onClick.AddListener(SendMessageChat);

        _pannelcon.SetActive(false);

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "HBao_Map")
        {
            if (RoomPlayer.Local != null && RoomPlayer.Local.IsHunter)
            {
                _pannelChatRoot.SetActive(false);
                return;
            }
        }

        if (_textDisplay != null)
        {
            _textDisplay.text = _globalChatHistory;
        }
    }

    public void ToggleChat()
    {
        if (_pannelcon != null)
        {
            bool isActive = !_pannelcon.activeSelf;
            _pannelcon.SetActive(isActive);
            if (isActive)
            {
                _inputField.ActivateInputField(); // Focus luôn vào ô gõ
                StartCoroutine(ForceScrollDown());
            }
        }
    }

    public void SendMessageChat()
    {
        if (string.IsNullOrWhiteSpace(_inputField.text)) return;
        if (RoomPlayer.Local == null) return;

        string myName = RoomPlayer.Local.PlayerName.ToString();

        // Gửi tin nhắn qua mạng
        Rpc_SendChat(myName, _inputField.text);

        // Xóa ô nhập và focus lại để chat liên tục
        _inputField.text = "";
        _inputField.ActivateInputField(); 
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SendChat(string user, string message)
    {
        string time = DateTime.Now.ToString("HH:mm");
        string formatted = $"[{time}] <color=yellow><b>{user}:</b></color> {message}\n";

        // Lưu vào tổng lịch sử (Máy nào cũng tự lưu)
        _globalChatHistory += formatted;

        // Bất kể thằng nào gửi RPC, tao chỉ nhờ cái Script của tao (Local) cập nhật UI thôi.
        if (RoomPlayer.Local != null)
        {
            var myLocalChatSystem = RoomPlayer.Local.GetComponent<ChatSystem>();
            if (myLocalChatSystem != null)
            {
                myLocalChatSystem.RefreshChatUI();
            }
        }
    }

    // Hàm phụ để vẽ lại màn hình trên máy Local
    public void RefreshChatUI()
    {
        if (_textDisplay != null && _pannelChatRoot != null && _pannelChatRoot.activeSelf)
        {
            _textDisplay.text = _globalChatHistory;
            
            // Chỉ kéo thanh scroll nếu bảng chat đang được mở
            if (_pannelcon != null && _pannelcon.activeSelf)
            {
                StartCoroutine(ForceScrollDown());
            }
        }
    }

    IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
    }
}