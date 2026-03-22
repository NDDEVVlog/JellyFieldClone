using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

public enum GameState { Locked, Idle, Processing }

public class GridController : MonoBehaviour
{
    public static GridController Instance { get; private set; }

    [Header("Settings")]
    private CubeDataConfig cubeConfig;
    [Range(0, 1)] public float randomSpawnChance = 0.3f;

    private GridData _model;
    private GridView _view;
    private GridCell[,] _cells;
    private LevelConfigData _currentLevel;
    private GameState _state = GameState.Locked;
    public List<int> CurrentLevelPalette => _currentLevel != null ? _currentLevel.availableCubeIds : new List<int>();


    [Header("Auto Spawn Settings")]
    [SerializeField] private int minCellThreshold = 4; 
    [SerializeField] private int spawnAmount = 2;      

    void Awake() => Instance = this;

    void Start()
    {
        LevelManager.Instance.BeforeGameStart+= BeforeGameStart;
    }

    public void BeforeGameStart(CubeDataConfig cubeDataConfig, LevelConfigData levelConfigData)
    {
        cubeConfig = cubeDataConfig;
        _currentLevel = levelConfigData;
        InitializeLevel(levelConfigData);
    }

    public void InitializeLevel(LevelConfigData level)
    {
        if (level == null)
        {
            Debug.LogError("GridController: Level data truyền vào bị null!");
            return;
        }

        _state = GameState.Locked;
        _currentLevel = level;

        
        _currentLevel.LoadMatrix(); 

        _model = new GridData(level.width, level.height);
        _cells = new GridCell[level.width, level.height];

        
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
        
        if (_currentLevel == null || _currentLevel.startMatrix == null)
        {
            Debug.LogError("SetupBoard: startMatrix chưa được khởi tạo!");
            return;
        }

        for (int z = 0; z < _currentLevel.height; z++)
        {
            for (int x = 0; x < _currentLevel.width; x++)
            {
                
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


         var cameraFit = Camera.main.GetComponent<CameraAutoFit>();
        if (cameraFit != null)
        {
            cameraFit.FitCamera(_currentLevel.width, _currentLevel.height, 2.4f);
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
        
        var palette = _currentLevel.availableCubeIds;

        
        if (palette == null || palette.Count == 0)
        {
            Debug.LogError($"[GridController] Level '{_currentLevel.name}' chưa có availableCubeIds! Hãy kiểm tra trong Inspector.");
            
            palette = cubeConfig.configCubeData.ConvertAll(c => c.id);
        }

        var forbidden = new HashSet<int>();

        
        forbidden.Add(_model.GetValue((x - 1) * 2 + 1, z * 2));     // Bên trái dưới
        forbidden.Add(_model.GetValue((x - 1) * 2 + 1, z * 2 + 1)); // Bên trái trên
        forbidden.Add(_model.GetValue(x * 2, (z - 1) * 2 + 1));     // Bên dưới trái
        forbidden.Add(_model.GetValue(x * 2 + 1, (z - 1) * 2 + 1)); // Bên dưới phải

        
        var safe = new List<int>();
        foreach (var id in palette)
        {
            if (!forbidden.Contains(id)) safe.Add(id);
        }

        
        if (safe.Count == 0) safe = palette;

        
        if (safe.Count == 0)
        {
            Debug.LogError("[GridController] Không tìm thấy màu nào khả dụng! Hãy kiểm tra CubeDataConfig.");
            return new List<int> { 0 }; 
        }

        
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
                
                CheckWinAndLoseCondition.Instance.NotifyCubesCollected(targetID,cubes.Count);

                await _view.PlayMergeAnimation(cubes,targetID);
                await SyncAllCells();
                await UniTask.Delay(500);
                hasChanges = true;

            }
            else
            {
                if (CheckAndRefillGrid())
                {
                    hasChanges = true; 
                    await UniTask.Delay(200); 
                }
            }
        }
    }
    public void SyncToModel(Vector2Int cellCoord, int localX, int localZ, int id)
    {
        if (_model == null) return;

        
        int globalX = cellCoord.x * 2 + localX;
        int globalZ = cellCoord.y * 2 + localZ;

        
        _model.SetValue(globalX, globalZ, id);
    }

    public async UniTask<bool> TryPlaceCell(GridCell cell)
    {
        if (_state != GameState.Idle) return false;

        
        float spacing = 2.4f; 
        int cx = Mathf.RoundToInt(cell.transform.position.x / spacing);
        int cz = Mathf.RoundToInt(cell.transform.position.z / spacing);

        
        if (cx < 0 || cz < 0 || cx >= _currentLevel.width || cz >= _currentLevel.height) return false;
        
       
        if (_currentLevel.startMatrix[cz][cx] == LevelConfigData.GridCellStartStatus.Disable) return false;
        if (_cells[cx, cz] != null) return false;

        // Chấp nhận đặt cell
        _state = GameState.Processing;
        

        cell.transform.SetParent(_view.transform);
        cell.transform.position = _view.GetWorldPos(cx, cz);
        

        cell.UpdateCoordinate(new Vector2Int(cx, cz));
        _cells[cx, cz] = cell;

        
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                _model.SetValue(cx * 2 + i, cz * 2 + j, cell.GetIDAtLocal(i, j));

        
        await ProcessChainReaction();

        _state = GameState.Idle;
        return true;
    }
    private bool CheckAndRefillGrid()
    {
        int currentCellCount = 0;
        List<Vector2Int> emptyPositions = new List<Vector2Int>();

        // 1. Đếm số lượng cell hiện có và tìm các ô trống hợp lệ
        for (int x = 0; x < _currentLevel.width; x++)
        {
            for (int z = 0; z < _currentLevel.height; z++)
            {
                if (_cells[x, z] != null)
                {
                    currentCellCount++;
                }
                else if (_currentLevel.startMatrix[z][x] != LevelConfigData.GridCellStartStatus.Disable)
                {
                    emptyPositions.Add(new Vector2Int(x, z));
                }
            }
        }

        // 2. Nếu số lượng thấp hơn ngưỡng yêu cầu
        if (currentCellCount < minCellThreshold && emptyPositions.Count > 0)
        {
            // Tính toán số lượng cần spawn (không vượt quá số ô trống)
            int actualSpawnCount = Mathf.Min(Random.Range(1, spawnAmount + 1), emptyPositions.Count);

            for (int i = 0; i < actualSpawnCount; i++)
            {
                int randomIndex = Random.Range(0, emptyPositions.Count);
                Vector2Int pos = emptyPositions[randomIndex];

                SpawnCellAt(pos.x, pos.y);
                
                emptyPositions.RemoveAt(randomIndex);
                Debug.Log($"<color=yellow>Auto Refill:</color> Spawning cell at {pos}");
            }

            return true; 
        }

        return false; 
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