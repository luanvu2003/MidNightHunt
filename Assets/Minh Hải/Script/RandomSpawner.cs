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
        List<Transform> availablePoints = new List<Transform>();
        foreach (Transform t in data.spawnPoints)
        {
            if (!occupiedPoints.Contains(t))
            {
                availablePoints.Add(t);
            }
        }
        int count = Mathf.Min(data.spawnAmount, availablePoints.Count);
        if (count < data.spawnAmount)
        {
            Debug.LogWarning("Cảnh báo: Không đủ chỗ trống an toàn để tạo toàn bộ " + data.itemName + ". Chỉ tạo được " + count + " cái.");
        }

        for (int i = 0; i < count; i++)
        {
            int randomPointIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomPointIndex];
            int randomPrefabIndex = Random.Range(0, data.prefabs.Length);
            GameObject selectedPrefab = data.prefabs[randomPrefabIndex];
            Instantiate(selectedPrefab, selectedPoint.position, selectedPoint.rotation);
            occupiedPoints.Add(selectedPoint);
            availablePoints.RemoveAt(randomPointIndex);
        }
    }
}