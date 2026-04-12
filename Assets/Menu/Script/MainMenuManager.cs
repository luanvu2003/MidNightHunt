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
    // 🚨 ĐÃ XÓA: public GameObject OptionPanel; (Không dùng cái cũ nữa)

    [Header("Fusion Setup")]
    public NetworkRunner runnerPrefab; 
    public TMP_InputField usernameInputField; 
    public TMP_InputField hostIDInput;        

    private void Start()
    {
        if (PlayerInfo.Instance != null && !string.IsNullOrEmpty(PlayerInfo.Instance.PlayerName))
        {
            UsernamePanel.SetActive(false);
            MainMenuPanel.SetActive(true);
            HostPanel.SetActive(false);

            usernameInputField.text = PlayerInfo.Instance.PlayerName; 
        }
        else
        {
            UsernamePanel.SetActive(true);
            MainMenuPanel.SetActive(false);
            HostPanel.SetActive(false);

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
            PlayerInfo.Instance.PlayerName = name; 
            PlayerPrefs.SetString("SavedUsername", name); 

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
    
    // 🚨 ĐÃ XÓA: OpenOptionPanel() và CloseOptionPanel()

    // --- LOGIC KẾT NỐI FUSION ---
    public async void StartGame(GameMode mode, string sessionID)
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.ShowLoading();
        }

        await System.Threading.Tasks.Task.Delay(200);

        var runner = Instantiate(runnerPrefab);
        DontDestroyOnLoad(runner);

        var customProps = new System.Collections.Generic.Dictionary<string, SessionProperty>();

        if (mode == GameMode.Host || mode == GameMode.Server)
        {
            customProps["HostName"] = PlayerInfo.Instance.PlayerName;
        }

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionID,
            SessionProperties = customProps,
            Scene = SceneRef.FromIndex(1), 
            PlayerCount = 5,
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnClickCreate()
    {
        string randomID = Random.Range(100000, 999999).ToString();
        StartGame(GameMode.Host, randomID);
    }

    public void SinglePlayer()
    {
        StartGame(GameMode.Single, "SinglePlayer_" + Random.Range(0, 1000));
    }

    public void OnClickJoin()
    {
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