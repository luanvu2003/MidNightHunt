using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemSpawnData
{
    public string itemName; 
    public GameObject[] prefabs; 
    public Transform[] spawnPoints; 
    public int spawnAmount; 
}

public class RandomSpawner : MonoBehaviour
{
    [Header("Danh sách các vật phẩm cần tạo trên bản đồ")]
    public ItemSpawnData[] itemsToSpawn; 

    // QUAN TRỌNG: "Cuốn sổ" ghi nhớ các điểm đã bị chiếm trên toàn bản đồ
    private List<Transform> occupiedPoints = new List<Transform>();

    void Start()
    {
        foreach (ItemSpawnData itemData in itemsToSpawn)
        {
            SpawnSingleItemType(itemData);
        }
    }

    void SpawnSingleItemType(ItemSpawnData data)
    {
        if (data.prefabs.Length == 0 || data.spawnPoints.Length == 0) return;

        // 1. LỌC TÌM CHỖ TRỐNG: Chỉ lấy những điểm CHƯA có trong "Cuốn sổ" occupiedPoints
        List<Transform> availablePoints = new List<Transform>();
        foreach (Transform t in data.spawnPoints)
        {
            if (!occupiedPoints.Contains(t))
            {
                availablePoints.Add(t);
            }
        }

        // Kiểm tra xem số chỗ trống còn lại có đủ để spawn số lượng yêu cầu không
        int count = Mathf.Min(data.spawnAmount, availablePoints.Count);
        if (count < data.spawnAmount)
        {
            Debug.LogWarning("Cảnh báo: Không đủ chỗ trống an toàn để tạo toàn bộ " + data.itemName + ". Chỉ tạo được " + count + " cái.");
        }

        for (int i = 0; i < count; i++)
        {
            // Chọn điểm random trong số các chỗ còn trống
            int randomPointIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomPointIndex];

            int randomPrefabIndex = Random.Range(0, data.prefabs.Length);
            GameObject selectedPrefab = data.prefabs[randomPrefabIndex];

            // Tạo vật thể
            Instantiate(selectedPrefab, selectedPoint.position, selectedPoint.rotation);

            // 2. CẬP NHẬT "CUỐN SỔ": Đánh dấu điểm này đã có chủ để vật phẩm loại khác không đè vào
            occupiedPoints.Add(selectedPoint);

            // Xóa khỏi danh sách trống của vòng lặp hiện tại
            availablePoints.RemoveAt(randomPointIndex);
        }
    }
}