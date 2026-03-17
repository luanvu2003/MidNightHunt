using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject UsernamePanel;
    public GameObject MainMenuPanel;
    public GameObject HostPanel;
    public GameObject OptionPanel;

    [Header("Fusion Setup")]
    public NetworkRunner runnerPrefab; // Kéo file NetworkRunner Prefab vào đây
    public TMP_InputField usernameInputField; // Ô nhập tên người chơi
    public TMP_InputField hostIDInput;        // Ô nhập ID phòng (Session Name)

    private void Start()
    {
        // Kiểm tra xem đã có tên trong PlayerInfo chưa (Trường hợp vừa từ Room thoát ra)
        if (PlayerInfo.Instance != null && !string.IsNullOrEmpty(PlayerInfo.Instance.PlayerName))
        {
            // Bỏ qua bước nhập tên, vào thẳng Menu
            UsernamePanel.SetActive(false);
            MainMenuPanel.SetActive(true);
            HostPanel.SetActive(false);
            OptionPanel.SetActive(false);

            usernameInputField.text = PlayerInfo.Instance.PlayerName; // Điền sẵn lại tên vào ô input cho chắc
        }
        else
        {
            // Trường hợp mới mở game lần đầu
            UsernamePanel.SetActive(true);
            MainMenuPanel.SetActive(false);
            HostPanel.SetActive(false);
            OptionPanel.SetActive(false);

            // Load lại tên cũ nếu đã từng chơi
            if (PlayerPrefs.HasKey("SavedUsername"))
            {
                usernameInputField.text = PlayerPrefs.GetString("SavedUsername");
            }
        }
    }

    // --- QUẢN LÝ USERNAME ---
    public void ConfirmUsername()
    {
        string name = usernameInputField.text;
        if (!string.IsNullOrEmpty(name))
        {
            PlayerInfo.Instance.PlayerName = name; // Lưu vào Singleton
            PlayerPrefs.SetString("SavedUsername", name); // Lưu vào máy

            UsernamePanel.SetActive(false);
            MainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Vui lòng nhập tên!");
        }
    }

    // --- ĐIỀU HƯỚNG UI ---
    public void OpenHostPanel() => HostPanel.SetActive(true);
    public void CloseHostPanel() => HostPanel.SetActive(false);
    public void OpenOptionPanel() => OptionPanel.SetActive(true);
    public void CloseOptionPanel() => OptionPanel.SetActive(false);

    // --- LOGIC KẾT NỐI FUSION ---
    public async void StartGame(GameMode mode, string sessionID)
    {
        var runner = Instantiate(runnerPrefab);
        DontDestroyOnLoad(runner);

        // Tạo dữ liệu đính kèm cho phòng (Session Properties)
        var customProps = new System.Collections.Generic.Dictionary<string, SessionProperty>();

        // Nếu là Host, ta đính kèm thêm tên của mình vào thuộc tính "HostName"
        if (mode == GameMode.Host || mode == GameMode.Server)
        {
            customProps["HostName"] = PlayerInfo.Instance.PlayerName;
        }

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionID, // Đây là ID (Ví dụ: 12345)
            SessionProperties = customProps, // Đây là nơi giữ tên "Hoài Bảo"
            Scene = SceneRef.FromIndex(1),
            PlayerCount = 5,
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnClickCreate()
    {
        // Tạo mã ID ngẫu nhiên (chỉ là số hoặc mã ngắn)
        string randomID = Random.Range(100000, 999999).ToString();
        StartGame(GameMode.Host, randomID);
    }

    // Nút Single Player (Chế độ chơi đơn trong Fusion)
    public void SinglePlayer()
    {
        StartGame(GameMode.Single, "SinglePlayer_" + Random.Range(0, 1000));
    }


    // Nút Join Room trên bảng gỗ
    public void OnClickJoin()
    {
        // Khi Join thì lấy tên từ ô Input Host ID mà người chơi nhập vào
        if (!string.IsNullOrEmpty(hostIDInput.text))
        {
            StartGame(GameMode.Client, hostIDInput.text);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game Exited");
    }
}