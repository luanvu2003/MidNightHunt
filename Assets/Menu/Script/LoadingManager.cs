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

    // 🚨 QUAN TRỌNG: Lắng nghe sự kiện Scene Load xong
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneFinishLoading;
    }

    // 🚨 Nhớ tắt lắng nghe khi script bị hủy để tránh lỗi memory leak
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneFinishLoading;
    }

    private void Update()
    {
        // Chỉ xoay khi màn hình loading đang bật
        if (loadingContainer.activeSelf && loadingIcon != null)
        {
            loadingIcon.Rotate(0, 0, rotationSpeed * Time.unscaledDeltaTime);
        }
    }

    // --- GỌI HÀM NÀY ĐỂ BẬT LOADING BẤT TẬN ---
    public void ShowLoading()
    {
        if (loadingContainer != null)
        {
            loadingContainer.SetActive(true);
        }
    }

    // --- HÀM TỰ ĐỘNG CHẠY KHI SCENE LOAD XONG 100% ---
    private void OnSceneFinishLoading(Scene scene, LoadSceneMode mode)
    {
        if (loadingContainer != null)
        {
            // Có thể thêm 1 chút delay nhỏ xíu ở đây nếu muốn đợi Fusion đồng bộ mạng mượt hơn
            loadingContainer.SetActive(false);
        }
    }

    // (Tùy chọn) Hàm tắt thủ công đề phòng bạn muốn tự gọi
    public void HideLoading()
    {
        if (loadingContainer != null) loadingContainer.SetActive(false);
    }
}