using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class GridCell : MonoBehaviour
{
    private const int SIZE = 2;
    private int[,] _matrix = new int[SIZE, SIZE];
    
    // Dependencies (Truyền vào qua Initialize)
    private GridController _controller;
    private CubeDataConfig _config;
    public Vector2Int coord;

    [System.Serializable]
    public class ClusterData
    {
        public int id;
        public List<Vector2Int> slots;
        public GameObject cube;
    }

    public List<ClusterData> currentClusters = new List<ClusterData>();

    public void Initialize(GridController controller, CubeDataConfig config, List<int> initialNumbers, Vector2Int coord)
    {
        _controller = controller;
        _config = config;
        this.coord = coord;

        for (int x = 0; x < SIZE; x++)
            for (int z = 0; z < SIZE; z++) _matrix[x, z] = -1;

        // Đổ dữ liệu ban đầu (Giữ nguyên logic gốc của bạn)
        bool fillHoriz = UnityEngine.Random.value > 0.5f;
        for (int i = 0; i < Mathf.Min(initialNumbers.Count, 4); i++)
        {
            if (fillHoriz) _matrix[i % SIZE, i / SIZE] = initialNumbers[i];
            else _matrix[i / SIZE, i % SIZE] = initialNumbers[i];
        }
        
        ApplyExpansionLogic(); 
        RefreshVisuals(false).Forget();
    }

    public async UniTask UpdateFromLogic(int[,] clearedData)
    {
        _matrix = clearedData;
        ApplyExpansionLogic();

        // Đồng bộ ngược lại cho Model toàn cục
        for (int x = 0; x < SIZE; x++)
            for (int z = 0; z < SIZE; z++)
                _controller.SyncToModel(coord, x, z, _matrix[x, z]);

        await RefreshVisuals(true);
    }

    private void ApplyExpansionLogic()
    {
        // 1. PHÁ THẾ CARO (Logic gốc của bạn)
        HashSet<int> uniqueIDs = new HashSet<int>();
        foreach (int id in _matrix) if (id != -1) uniqueIDs.Add(id);

        if (uniqueIDs.Count < 4)
        {
            if (_matrix[0, 0] != -1 && _matrix[0, 0] == _matrix[1, 1]) _matrix[1, 1] = -1;
            if (_matrix[0, 1] != -1 && _matrix[0, 1] == _matrix[1, 0]) _matrix[1, 0] = -1;
        }

        // 2. LẤP ĐẦY THÔNG MINH (Cải tiến để không bao giờ bị rỗng)
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
                        // Thử lấy ID từ hàng xóm (Ưu tiên màu đang có ít nhất)
                        int id = GetBalancedNeighborID(x, z);
                        
                        // Nếu không có hàng xóm nào (ví dụ ô đầu tiên bị rỗng), lấy màu từ palette
                        if (id == -1) id = GetRandomFromPalette();

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

    private int GetBalancedNeighborID(int x, int z)
    {
        Span<int> neighbors = stackalloc int[4];
        int neighborCount = 0;

        if (x > 0) neighbors[neighborCount++] = _matrix[x - 1, z];
        if (x < SIZE - 1) neighbors[neighborCount++] = _matrix[x + 1, z];
        if (z > 0) neighbors[neighborCount++] = _matrix[x, z - 1];
        if (z < SIZE - 1) neighbors[neighborCount++] = _matrix[x, z + 1];

        int bestId = -1;
        int minCount = int.MaxValue;

        for (int i = 0; i < neighborCount; i++)
        {
            int id = neighbors[i];
            if (id == -1) continue;

            int count = GetTotalIDCount(id);
            if (count < minCount)
            {
                minCount = count;
                bestId = id;
            }
        }
        return bestId;
    }

    private int GetRandomFromPalette()
    {
        var palette = _controller.CurrentLevelPalette;
        if (palette == null || palette.Count == 0) return -1;
        
        // Ưu tiên lấy màu chưa xuất hiện trong Cell này để phá thế caro/trùng lặp
        foreach (var id in palette)
        {
            if (GetTotalIDCount(id) == 0) return id;
        }
        return palette[UnityEngine.Random.Range(0, palette.Count)];
    }

    private int GetTotalIDCount(int id)
    {
        int count = 0;
        for (int x = 0; x < SIZE; x++)
            for (int z = 0; z < SIZE; z++)
                if (_matrix[x, z] == id) count++;
        return count;
    }

    // --- CÁC HÀM VISUAL & POOLING ---
    private async UniTask RefreshVisuals(bool animate)
    {
        var clusters = FindLocalClusters();
        List<ClusterData> nextClusters = new List<ClusterData>();
        CancellationToken ct = this.GetCancellationTokenOnDestroy();

        foreach (var nc in clusters)
        {
            float minX = nc.slots.Min(s => s.x), maxX = nc.slots.Max(s => s.x);
            float minZ = nc.slots.Min(s => s.y), maxZ = nc.slots.Max(s => s.y);
            
            Vector3 targetScale = new Vector3((maxX - minX + 1) * 0.95f, 0.8f, (maxZ - minZ + 1) * 0.95f);
            Vector3 targetPos = new Vector3(((minX + maxX) / 2f) - 0.5f, 0, ((minZ + maxZ) / 2f) - 0.5f);

            var existing = currentClusters.FirstOrDefault(c => c.id == nc.id && c.slots.Any(s => nc.slots.Contains(s)));
            
            if (existing != null && existing.cube != null)
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
                cubeObj.GetComponent<Renderer>().material.color = _config.GetColor(nc.id);
                cubeObj.transform.localPosition = targetPos;
                cubeObj.transform.localScale = animate ? Vector3.zero : targetScale;
                
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

    private async UniTask AnimateVisual(Transform t, Vector3 tPos, Vector3 tScale, CancellationToken ct)
    {
        if (t == null) return;
        float elapsed = 0;
        float duration = 0.25f;
        Vector3 sPos = t.localPosition;
        Vector3 sScale = t.localScale;

        while (elapsed < duration)
        {
            if (t == null || ct.IsCancellationRequested) return;
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            float curve = Mathf.Sin(p * Mathf.PI * 0.5f); 

            t.localPosition = Vector3.Lerp(sPos, tPos, curve);
            t.localScale = Vector3.Lerp(sScale, tScale, curve);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        if (t != null) { t.localPosition = tPos; t.localScale = tScale; }
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

    public void SelfDestruct()
    {
        foreach (var c in currentClusters) if (c.cube) CubePool.Instance.ReturnToPool(c.cube);
        Destroy(gameObject);
    }
    public int GetIDAtLocal(int x, int z) => _matrix[x, z];
    public void UpdateCoordinate(Vector2Int newCoord)
    {
        this.coord = newCoord;
    }
}