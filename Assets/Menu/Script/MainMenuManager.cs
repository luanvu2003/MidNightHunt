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
        // Khi mở game: Hiện bảng nhập tên, ẩn các bảng còn lại
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
    public async void StartGame(GameMode mode)
    {
        // 1. Khởi tạo NetworkRunner
        var runner = Instantiate(runnerPrefab);
        DontDestroyOnLoad(runner); // Rất quan trọng: Giữ kết nối mạng khi chuyển Scene

        // 2. Chuyển Scene (Index 1 là Scene "Room")
        // Lưu ý: Đảm bảo Scene Room đã được add vào Build Settings ở vị trí số 1
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = hostIDInput.text,
            Scene = SceneRef.FromIndex(1),
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    // Nút Single Player (Chế độ chơi đơn trong Fusion)
    public void SinglePlayer()
    {
        // Dùng GameMode.Single để vẫn chạy được logic của Fusion mà không cần server
        StartGame(GameMode.Single);
    }

    // Nút Create Room trên bảng gỗ
    public void OnClickCreate()
    {
        // Nếu HostID trống, tự tạo một ID ngẫu nhiên
        if (string.IsNullOrEmpty(hostIDInput.text))
        {
            string randomID = "ROOM_" + Random.Range(1000, 9999).ToString();
            hostIDInput.text = randomID; // Hiển thị lên UI cho người chơi thấy để gửi cho bạn
        }

        StartGame(GameMode.Host);
    }

    // Nút Join Room trên bảng gỗ
    public void OnClickJoin() => StartGame(GameMode.Client);

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game Exited");
    }
}