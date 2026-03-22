
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CellSpawner : MonoBehaviour
{
    public static CellSpawner Instance { get; private set; }
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform[] spawnPoints;
    private DraggableCell[] _activeChoices;
    private CubeDataConfig cubeConfig;
    private LevelConfigData _currentLevel;

    private void Awake()
    {
        Instance = this;
        _activeChoices = new DraggableCell[spawnPoints.Length];
    }

    void Start()
    {
        LevelManager.Instance.BeforeGameStart+= BeforeGameStart;
        
    }

    public void BeforeGameStart(CubeDataConfig cubeDataConfig, LevelConfigData levelConfigData)
    {
        cubeConfig = cubeDataConfig;
        _currentLevel = levelConfigData;

        RepositionSpawner();
        SpawnAllSlots();
    }



    public void SpawnAllSlots()
    {
        for (int i = 0; i < spawnPoints.Length; i++) 
            if (_activeChoices[i] == null) SpawnAtSlot(i);
    }

    private void SpawnAtSlot(int index)
    {
        GameObject cellObj = Instantiate(cellPrefab, spawnPoints[index].position, Quaternion.identity, transform);
        GridCell cell = cellObj.GetComponent<GridCell>();

        
        cell.Initialize(
            GridController.Instance, 
            cubeConfig, 
            GenerateRandomIDs(), 
            new Vector2Int(-1, -1)
        );

        _activeChoices[index] = cellObj.AddComponent<DraggableCell>();
    }

    private List<int> GenerateRandomIDs()
    {
        
        var ids = _currentLevel.availableCubeIds;
        
        var result = new List<int>();
        int count = Random.Range(2, 5); 
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
    public void RepositionSpawner()
    {
        CameraAutoFit camFit = Camera.main.GetComponent<CameraAutoFit>();
        if (camFit != null)
        {
            transform.position = camFit.GetWorldPointAtScreenBottom();
        }
    }
}