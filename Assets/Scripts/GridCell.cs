using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;

public class GridCell : MonoBehaviour
{
    private const int SIZE = 2;
    public Vector2Int coord;
    public GameObject cubePrefab;
    private int[,] _matrix = new int[SIZE, SIZE];

    [System.Serializable]
    public class ClusterData
    {
        public int id;
        public List<Vector2Int> slots;
        public GameObject cube;
    }

    public List<ClusterData> currentClusters = new List<ClusterData>();

    public void Initialize(List<int> initialNumbers)
    {
        for (int x = 0; x < SIZE; x++)
            for (int z = 0; z < SIZE; z++) _matrix[x, z] = -1;

        bool fillHoriz = Random.value > 0.5f;
        for (int i = 0; i < Mathf.Min(initialNumbers.Count, 4); i++)
        {
            if (fillHoriz) _matrix[i % SIZE, i / SIZE] = initialNumbers[i];
            else _matrix[i / SIZE, i % SIZE] = initialNumbers[i];
        }
        
        ApplyExpansionLogic(); // Tự lấp đầy lúc mới sinh
        RefreshVisuals(false).Forget();
    }

    // Manager gọi hàm này khi có màu bị xóa
    public async UniTask UpdateFromLogic(int[,] clearedData)
    {
        _matrix = clearedData;
        
        // Tự mình tính toán việc nở ra (Expand)
        ApplyExpansionLogic();

        // Gửi dữ liệu sau khi nở lại cho Manager để đồng bộ Global Matrix
        GridManager.Instance.SyncBackFromCell(coord, _matrix);

        await RefreshVisuals(true);
    }

    private void ApplyExpansionLogic()
    {
        // 1. Phá thế caro (Đảm bảo không bao giờ có 2 màu nằm chéo nhau)
        HashSet<int> uniqueIDs = new HashSet<int>();
        foreach (int id in _matrix) if (id != -1) uniqueIDs.Add(id);

        if (uniqueIDs.Count < 4)
        {
            if (_matrix[0, 0] != -1 && _matrix[0, 0] == _matrix[1, 1]) _matrix[1, 1] = -1;
            if (_matrix[0, 1] != -1 && _matrix[0, 1] == _matrix[1, 0]) _matrix[1, 0] = -1;
        }

        // 2. Lấp đầy ô trống theo nguyên tắc CÂN BẰNG
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int x = 0; x < SIZE; x++)
            {
                for (int z = 0; z < SIZE; z++)
                {
                    if (_matrix[x, z] == -1)
                    {
                        // Tìm ID có số lượng ÍT NHẤT trong 4 ô để lấp vào
                        int id = GetBalancedNeighborID(x, z);
                        if (id != -1)
                        {
                            _matrix[x, z] = id;
                            changed = true;
                        }
                    }
                }
            }
        }
    }
    public GameObject ExtractCubeAt(int lx, int lz)
    {
        
        var cluster = currentClusters.FirstOrDefault(c => c.slots.Any(s => s.x == lx && s.y == lz));
        if (cluster != null && cluster.cube != null)
        {
            GameObject cubeObj = cluster.cube;
            
            cluster.cube = null; 
            currentClusters.Remove(cluster);
            
            cubeObj.transform.SetParent(null);
            return cubeObj;
        }
        return null;
    }

    private int GetBalancedNeighborID(int x, int z)
    {
        // Tìm tất cả các ID hàng xóm xung quanh ô (x,z)
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Dictionary<int, int> candidates = new Dictionary<int, int>();

        foreach (var d in dirs)
        {
            int nx = x + d.x;
            int nz = z + d.y;
            if (nx >= 0 && nx < SIZE && nz >= 0 && nz < SIZE)
            {
                int id = _matrix[nx, nz];
                if (id != -1 && !candidates.ContainsKey(id))
                {
                    // Đếm xem ID này đang chiếm bao nhiêu ô trong tổng số 4 ô
                    candidates[id] = GetTotalIDCount(id);
                }
            }
        }

        if (candidates.Count == 0) return -1;

        // SẮP XẾP: Thằng nào có số lượng ô (Count) nhỏ nhất thì chọn thằng đó
        // Ví dụ: Màu Tím có 2 ô, màu Xanh có 1 ô -> Chọn màu Xanh để lấp vào ô trống.
        // Điều này ngăn chặn màu Tím nhảy lên 3 ô.
        return candidates.OrderBy(kvp => kvp.Value).First().Key;
    }

    private int GetTotalIDCount(int id)
    {
        int count = 0;
        foreach (int val in _matrix) if (val == id) count++;
        return count;
    }
    // --- CÁC HÀM VISUAL GIỮ NGUYÊN ---
    private async UniTask RefreshVisuals(bool animate)
    {
        var clusters = FindLocalClusters();
        List<ClusterData> nextClusters = new List<ClusterData>();
        CancellationToken ct = this.GetCancellationTokenOnDestroy();

        foreach (var nc in clusters)
        {
            float minX = nc.slots.Min(s => s.x), maxX = nc.slots.Max(s => s.x);
            float minZ = nc.slots.Min(s => s.y), maxZ = nc.slots.Max(s => s.y);
            
            Vector3 targetScale = new Vector3((maxX - minX + 1) * 0.96f, 0.8f, (maxZ - minZ + 1) * 0.96f);
            Vector3 targetPos = new Vector3(((minX + maxX) / 2f) - 0.5f, 0, ((minZ + maxZ) / 2f) - 0.5f);

            var existing = currentClusters.FirstOrDefault(c => c.id == nc.id && c.slots.Any(s => nc.slots.Contains(s)));
            
            if (existing != null)
            {
                existing.slots = nc.slots;
                if (animate) AnimateVisual(existing.cube.transform, targetPos, targetScale, ct).Forget();
                else { existing.cube.transform.localPosition = targetPos; existing.cube.transform.localScale = targetScale; }
                nextClusters.Add(existing);
                currentClusters.Remove(existing);
            }
            else
            {
                GameObject cubeObj = CubePool.Instance.Get(transform);
                cubeObj.GetComponent<Renderer>().material.color = GridManager.Instance.GetColorFromID(nc.id);
                
                cubeObj.transform.localPosition = targetPos;
                cubeObj.transform.localScale = Vector3.zero; // Spawn từ 0
                
                AnimateVisual(cubeObj.transform, targetPos, targetScale, ct).Forget();
                nextClusters.Add(new ClusterData { id = nc.id, slots = nc.slots, cube = cubeObj });
            }
        }

        foreach (var old in currentClusters) if (old.cube) CubePool.Instance.ReturnToPool(old.cube);
        currentClusters = nextClusters;
    }

    private List<ClusterData> FindLocalClusters()
    {
        List<ClusterData> results = new List<ClusterData>();
        bool[,] visited = new bool[SIZE, SIZE];
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                if (visited[x, z] || _matrix[x, z] == -1) continue;
                int id = _matrix[x, z];
                List<Vector2Int> slots = new List<Vector2Int>();
                Queue<Vector2Int> q = new Queue<Vector2Int>();
                q.Enqueue(new Vector2Int(x, z));
                visited[x, z] = true;
                while (q.Count > 0)
                {
                    var curr = q.Dequeue();
                    slots.Add(curr);
                    foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    {
                        int nx = curr.x + d.x, nz = curr.y + d.y;
                        if (nx >= 0 && nx < SIZE && nz >= 0 && nz < SIZE && !visited[nx, nz] && _matrix[nx, nz] == id)
                        {
                            visited[nx, nz] = true;
                            q.Enqueue(new Vector2Int(nx, nz));
                        }
                    }
                }
                results.Add(new ClusterData { id = id, slots = slots });
            }
        }
        return results;
    }

    // Trong GridCell.cs

    private async UniTask AnimateVisual(Transform t, Vector3 tPos, Vector3 tScale, CancellationToken ct)
    {
        if (t == null) return;

        Vector3 sPos = t.localPosition;
        Vector3 sScale = t.localScale;
        float elapsed = 0;
        float duration = GridManager.Instance.animDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Lấy giá trị từ Curve (nên dùng curve có overshoot để tạo cảm giác jelly nảy)
            float p = GridManager.Instance.expandCurve.Evaluate(elapsed / duration);

            // Sử dụng LerpUnclamped để cho phép khối "nở" quá kích cỡ rồi co lại (nếu curve > 1)
            t.localPosition = Vector3.LerpUnclamped(sPos, tPos, p);
            t.localScale = Vector3.LerpUnclamped(sScale, tScale, p);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
            if (ct.IsCancellationRequested) return;
        }

        t.localPosition = tPos;
        t.localScale = tScale;
    }

    public void SelfDestruct()
    {
        foreach (var c in currentClusters) if (c.cube) CubePool.Instance.ReturnToPool(c.cube);
        Destroy(gameObject);
    }
    public int GetIDAtLocal(int x, int z) => _matrix[x, z];
}