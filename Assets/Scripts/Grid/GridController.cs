using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public enum GameState { Locked, Idle, Processing }

public class GridController : MonoBehaviour
{
    public static GridController Instance { get; private set; }

    [Header("Settings")]
    public CubeDataConfig cubeConfig;
    [Range(0, 1)] public float randomSpawnChance = 0.3f;

    private GridData _model;
    private GridView _view;
    private GridCell[,] _cells;
    private LevelConfigData _currentLevel;
    private GameState _state = GameState.Locked;
    public List<int> CurrentLevelPalette => _currentLevel != null ? _currentLevel.availableCubeIds : new List<int>();

    void Awake() => Instance = this;

    public void InitializeLevel(LevelConfigData level)
    {
        if (level == null)
        {
            Debug.LogError("GridController: Level data truyền vào bị null!");
            return;
        }

        _state = GameState.Locked;
        _currentLevel = level;

        // QUAN TRỌNG: Phải LoadMatrix trước khi dùng startMatrix
        _currentLevel.LoadMatrix(); 

        _model = new GridData(level.width, level.height);
        _cells = new GridCell[level.width, level.height];

        // Đảm bảo lấy được View
        if (_view == null) _view = GetComponent<GridView>();
        
        if (_view == null)
        {
            Debug.LogError("GridController: Không tìm thấy GridView trên cùng GameObject!");
            return;
        }

        _view.ClearBoard();
        SetupBoard().Forget();
    }

    private async UniTaskVoid SetupBoard()
    {
        // Kiểm tra an toàn một lần nữa trước khi vào loop
        if (_currentLevel == null || _currentLevel.startMatrix == null)
        {
            Debug.LogError("SetupBoard: startMatrix chưa được khởi tạo!");
            return;
        }

        for (int z = 0; z < _currentLevel.height; z++)
        {
            for (int x = 0; x < _currentLevel.width; x++)
            {
                // Kiểm tra chỉ số mảng (Dòng 43 thường nằm ở đây)
                if (z >= _currentLevel.startMatrix.Length || x >= _currentLevel.startMatrix[z].Length)
                {
                    Debug.LogWarning($"SetupBoard: Tọa độ {x},{z} nằm ngoài ma trận!");
                    continue;
                }

                var status = _currentLevel.startMatrix[z][x];
                if (status == LevelConfigData.GridCellStartStatus.Disable) continue;

                _view.SpawnBackground(x, z);

                if (status == LevelConfigData.GridCellStartStatus.AlwaysHasBlockInit || 
                (status == LevelConfigData.GridCellStartStatus.AllowToSpawn && Random.value < randomSpawnChance))
                {
                    SpawnCellAt(x, z);
                }
            }
        }
        await ProcessChainReaction();
        _state = GameState.Idle;
    }

    private void SpawnCellAt(int x, int z)
    {
        List<int> safeIds = GetSafeIds(x, z);
        var cell = _view.CreateCell(x, z, this, cubeConfig, safeIds);
        _cells[x, z] = cell;
        
        // Sync to model
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                _model.SetValue(x * 2 + i, z * 2 + j, cell.GetIDAtLocal(i, j));
    }

    private List<int> GetSafeIds(int x, int z)
    {
        // 1. Lấy danh sách màu từ Level hiện tại
        var palette = _currentLevel.availableCubeIds;

        // KIỂM TRA: Nếu Level chưa được gán màu nào, báo lỗi và lấy tạm từ Config tổng
        if (palette == null || palette.Count == 0)
        {
            Debug.LogError($"[GridController] Level '{_currentLevel.name}' chưa có availableCubeIds! Hãy kiểm tra trong Inspector.");
            // Fallback: Lấy tất cả ID từ config nếu level bị trống
            palette = cubeConfig.configCubeData.ConvertAll(c => c.id);
        }

        var forbidden = new HashSet<int>();

        // 2. Kiểm tra hàng xóm để tránh trùng màu cạnh nhau
        // Lấy giá trị từ Model (Global Matrix)
        forbidden.Add(_model.GetValue((x - 1) * 2 + 1, z * 2));     // Bên trái dưới
        forbidden.Add(_model.GetValue((x - 1) * 2 + 1, z * 2 + 1)); // Bên trái trên
        forbidden.Add(_model.GetValue(x * 2, (z - 1) * 2 + 1));     // Bên dưới trái
        forbidden.Add(_model.GetValue(x * 2 + 1, (z - 1) * 2 + 1)); // Bên dưới phải

        // 3. Lọc danh sách màu "an toàn" (không nằm trong forbidden)
        var safe = new List<int>();
        foreach (var id in palette)
        {
            if (!forbidden.Contains(id)) safe.Add(id);
        }

        // 4. Nếu tất cả màu đều bị cấm (hiếm gặp), quay lại dùng toàn bộ palette
        if (safe.Count == 0) safe = palette;

        // 5. Kiểm tra cuối cùng để tránh lỗi Index out of range
        if (safe.Count == 0)
        {
            Debug.LogError("[GridController] Không tìm thấy màu nào khả dụng! Hãy kiểm tra CubeDataConfig.");
            return new List<int> { 0 }; // Trả về ID mặc định để không sập game
        }

        // 6. Chọn ngẫu nhiên từ 2 đến 4 màu
        int count = Random.Range(2, 5);
        var result = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, safe.Count);
            result.Add(safe[randomIndex]);
        }

        return result;
    }

    public async UniTask ProcessChainReaction()
    {
        bool hasChanges = true;
        while (hasChanges)
        {
            hasChanges = false;
            var matches = _model.FindMatches(out int targetID);
            if (matches.Count > 0)
            {
                List<GameObject> cubes = new List<GameObject>();
                foreach (var p in matches)
                {
                    var cube = _cells[p.x / 2, p.y / 2].ExtractCubeAt(p.x % 2, p.y % 2);
                    if (cube != null) cubes.Add(cube);
                    _model.SetValue(p.x, p.y, -1);
                }

                await _view.PlayMergeAnimation(cubes);
                await SyncAllCells();
                await UniTask.Delay(500);
                hasChanges = true;

            }
        }
    }
    public void SyncToModel(Vector2Int cellCoord, int localX, int localZ, int id)
    {
        if (_model == null) return;

        // Chuyển đổi tọa độ cục bộ của Cell (0,1) thành tọa độ toàn cầu của ma trận
        int globalX = cellCoord.x * 2 + localX;
        int globalZ = cellCoord.y * 2 + localZ;

        // Cập nhật giá trị vào Model
        _model.SetValue(globalX, globalZ, id);
    }

    public async UniTask<bool> TryPlaceCell(GridCell cell)
    {
        if (_state != GameState.Idle) return false;

        // Tính toán tọa độ lưới dựa trên vị trí thế giới
        // (Giả sử bạn để GridView quản lý spacing)
        float spacing = 2.4f; // Nên lấy từ GridView.cellSpacing
        int cx = Mathf.RoundToInt(cell.transform.position.x / spacing);
        int cz = Mathf.RoundToInt(cell.transform.position.z / spacing);

        // 1. Kiểm tra biên
        if (cx < 0 || cz < 0 || cx >= _currentLevel.width || cz >= _currentLevel.height) return false;
        
        // 2. Kiểm tra ô trống và ô bị Disable trong Level Design
        if (_currentLevel.startMatrix[cz][cx] == LevelConfigData.GridCellStartStatus.Disable) return false;
        if (_cells[cx, cz] != null) return false;

        // Chấp nhận đặt cell
        _state = GameState.Processing;
        
        // Đưa cell về đúng vị trí snap
        cell.transform.SetParent(_view.transform);
        cell.transform.position = _view.GetWorldPos(cx, cz);
        
        // Cập nhật tọa độ và tham chiếu
        cell.UpdateCoordinate(new Vector2Int(cx, cz));
        _cells[cx, cz] = cell;

        // Đồng bộ dữ liệu vào Model (Global Matrix)
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                _model.SetValue(cx * 2 + i, cz * 2 + j, cell.GetIDAtLocal(i, j));

        // Chạy chuỗi phản ứng (Merge, Expand...)
        await ProcessChainReaction();

        _state = GameState.Idle;
        return true;
    }

    private async UniTask SyncAllCells()
    {
        var tasks = new List<UniTask>();
        for (int x = 0; x < _currentLevel.width; x++)
        {
            for (int z = 0; z < _currentLevel.height; z++)
            {
                if (_cells[x, z] == null) continue;
                
                int[,] local = new int[2, 2];
                bool isEmpty = true;
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++) {
                        local[i, j] = _model.GetValue(x * 2 + i, z * 2 + j);
                        if (local[i, j] != -1) isEmpty = false;
                    }

                if (isEmpty) { _cells[x, z].SelfDestruct(); _cells[x, z] = null; }
                else tasks.Add(_cells[x, z].UpdateFromLogic(local));
            }
        }
        await UniTask.WhenAll(tasks);
    }

    
}