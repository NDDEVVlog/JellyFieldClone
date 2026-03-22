using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

public class GridView : MonoBehaviour
{
    [SerializeField] private GameObject backgroundPrefab;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private float cellSpacing = 2.4f;

    public UnityEvent afterMerge;

    public Vector3 GetWorldPos(int x, int z) => new Vector3(x * cellSpacing, 0, z * cellSpacing);

    public void ClearBoard()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
    }

    public void SpawnBackground(int x, int z)
    {
        Instantiate(backgroundPrefab, GetWorldPos(x, z), Quaternion.Euler(90, 0, 0), transform);
    }

    public GridCell CreateCell(int x, int z, GridController controller, CubeDataConfig config, List<int> ids)
    {
        var obj = Instantiate(cellPrefab, GetWorldPos(x, z), Quaternion.identity, transform);
        var cell = obj.GetComponent<GridCell>();
        cell.Initialize(controller, config, ids, new Vector2Int(x, z));
        return cell;
    }

    public async UniTask PlayMergeAnimation(List<GameObject> cubes,int id)
    {
        if (MergeAnimator.Instance != null)
        {   Debug.Log("Play MergeAnimation");
            await MergeAnimator.Instance.PlayMergeAnimation(cubes, id);
            afterMerge?.Invoke();
        }
            
        else 
            await UniTask.Delay(500);
    }
}