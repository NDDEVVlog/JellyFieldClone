// CellSpawner.cs hoàn chỉnh
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CellSpawner : MonoBehaviour
{
    public static CellSpawner Instance { get; private set; }
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform[] spawnPoints;
    private DraggableCell[] _activeChoices;

    private void Awake()
    {
        Instance = this;
        _activeChoices = new DraggableCell[spawnPoints.Length];
    }

    private void Start() => SpawnAllSlots();

    public void SpawnAllSlots()
    {
        for (int i = 0; i < spawnPoints.Length; i++) 
            if (_activeChoices[i] == null) SpawnAtSlot(i);
    }

    private void SpawnAtSlot(int index)
    {
        GameObject cellObj = Instantiate(cellPrefab, spawnPoints[index].position, Quaternion.identity, transform);
        GridCell cell = cellObj.GetComponent<GridCell>();

        // TRUYỀN DATA (Dependency Injection)
        // Vì chưa đặt vào lưới, ta truyền tọa độ tạm là (-1, -1)
        cell.Initialize(
            GridController.Instance, 
            GridController.Instance.cubeConfig, 
            GenerateRandomIDs(), 
            new Vector2Int(-1, -1)
        );

        _activeChoices[index] = cellObj.AddComponent<DraggableCell>();
    }

    private List<int> GenerateRandomIDs()
    {
        // Fix lỗi GridManager: Lấy palette màu từ Config của Controller
        var palette = GridController.Instance.cubeConfig.configCubeData;
        var ids = palette.ConvertAll(s => s.id);
        
        var result = new List<int>();
        int count = Random.Range(2, 5); // Game thường yêu cầu ít nhất 2 màu để dễ merge
        for (int i = 0; i < count; i++)
        {
            result.Add(ids[Random.Range(0, ids.Count)]);
        }
        return result;
    }

    public void OnCellPlaced(DraggableCell cell)
    {
        for (int i = 0; i < _activeChoices.Length; i++)
        {
            if (_activeChoices[i] == cell) 
            { 
                _activeChoices[i] = null; 
                // Delay một chút trước khi spawn slot mới cho mượt
                SpawnNewSlotWithDelay(i).Forget(); 
                break; 
            }
        }
    }

    private async UniTaskVoid SpawnNewSlotWithDelay(int index)
    {
        await UniTask.Delay(500);
        SpawnAtSlot(index);
    }
}