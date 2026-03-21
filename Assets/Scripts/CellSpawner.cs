using UnityEngine;
using System.Collections.Generic;

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
        for (int i = 0; i < spawnPoints.Length; i++) if (_activeChoices[i] == null) SpawnAtSlot(i);
    }

    private void SpawnAtSlot(int index)
    {
        GameObject cellObj = Instantiate(cellPrefab, spawnPoints[index].position, Quaternion.identity, transform);
        GridCell cell = cellObj.GetComponent<GridCell>();
        cell.Initialize(GenerateRandomIDs());
        _activeChoices[index] = cellObj.AddComponent<DraggableCell>();
    }

    private List<int> GenerateRandomIDs()
    {
        var ids = GridManager.Instance.cubeSettings.ConvertAll(s => s.id);
        var result = new List<int>();
        int count = Random.Range(1, 5);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, ids.Count);
            result.Add(ids[idx]);
            ids.RemoveAt(idx);
        }
        return result;
    }

    public void OnCellPlaced(DraggableCell cell)
    {
        for (int i = 0; i < _activeChoices.Length; i++)
        {
            if (_activeChoices[i] == cell) { _activeChoices[i] = null; Invoke(nameof(SpawnAllSlots), 0.5f); break; }
        }
    }
}