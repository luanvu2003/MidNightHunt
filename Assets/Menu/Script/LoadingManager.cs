using UnityEngine;
using UnityEngine.SceneManagement; // Cần thư viện này để theo dõi tiến độ load Scene

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;
    [Header("UI Cài đặt")]
    public GameObject loadingContainer; 
    public RectTransform loadingIcon;
    [Header("Cài đặt hiệu ứng")]
    public float rotationSpeed = -300f; 
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (loadingContainer != null) loadingContainer.SetActive(false);
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneFinishLoading;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneFinishLoading;
    }
    private void Update()
    {
        if (loadingContainer.activeSelf && loadingIcon != null)
        {
            loadingIcon.Rotate(0, 0, rotationSpeed * Time.unscaledDeltaTime);
        }
    }
    public void ShowLoading()
    {
        if (loadingContainer != null)
        {
            loadingContainer.SetActive(true);
        }
    }
    private void OnSceneFinishLoading(Scene scene, LoadSceneMode mode)
    {
        if (loadingContainer != null)
        {
            loadingContainer.SetActive(false);
        }
    }
    public void HideLoading()
    {
        if (loadingContainer != null) loadingContainer.SetActive(false);
    }
}