// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;
// using Cysharp.Threading.Tasks;


// public class GridManager : MonoBehaviour
// {
//     public static GridManager Instance;

//     [Header("Level Configuration")]
//     public LevelConfigData currentLevel; 
//     public GameObject backgroundPrefab;
//     public GameObject cellPrefab;
//     [Range(0, 1)] public float randomSpawnChance = 0.3f;
//     public float cellSpacing = 2.4f;

//     [Header("Visual Settings")]
//     public List<CubeData> cubeSettings;
//     public AnimationCurve expandCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
//     public float animDuration = 0.3f;

//     private GridCell[,] _gridCells; 
//     private int[,] _globalMatrix;   
//     private int _width, _height;
//     private bool _isProcessing;

//     void Awake()
//     {
//         Instance = this;
        
//     }


//     public void InitializeLevel(LevelConfigData levelData)
//     {
//         if (levelData == null) return;
        
//         // Gán level hiện tại
//         currentLevel = levelData; 
        
//         currentLevel.LoadMatrix();
//         _width = currentLevel.width;
//         _height = currentLevel.height;

//         // Reset lại mảng nếu đây là lần load màn tiếp theo
//         _gridCells = new GridCell[_width, _height];
//         _globalMatrix = new int[_width * 2, _height * 2];

//         for (int i = 0; i < _width * 2; i++)
//             for (int j = 0; j < _height * 2; j++) _globalMatrix[i, j] = -1;

//         if (CheckWinAndLoseCondition.Instance != null)
//             CheckWinAndLoseCondition.Instance.Setup(currentLevel);

//         // Xóa các object cũ nếu có (trường hợp chơi lại hoặc qua màn)
//         foreach (Transform child in transform) {
//             Destroy(child.gameObject);
//         }

//         SpawnBoard();
//         SpawnInitialCells();
//     }


//     private void SpawnBoard()
//     {
//         for (int z = 0; z < _height; z++)
//         {
//             for (int x = 0; x < _width; x++)
//             {
//                 var status = currentLevel.startMatrix[z][x];
//                 if (status == LevelConfigData.GridCellStartStatus.Disable) continue;

//                 Instantiate(backgroundPrefab, GetWorldPos(x, z), Quaternion.Euler(90, 0, 0), transform);
//             }
//         }
//     }

//     private void SpawnInitialCells()
//     {
//         for (int z = 0; z < _height; z++)
//         {
//             for (int x = 0; x < _width; x++)
//             {
//                 var status = currentLevel.startMatrix[z][x];
//                 bool shouldSpawn = false;

//                 // 1. Nếu AlwaysHasBlockInit: Chắc chắn spawn
//                 if (status == LevelConfigData.GridCellStartStatus.AlwaysHasBlockInit) 
//                 {
//                     shouldSpawn = true;
//                 }
//                 // 2. Nếu AllowToSpawn: Spawn dựa trên tỉ lệ ngẫu nhiên
//                 else if (status == LevelConfigData.GridCellStartStatus.AllowToSpawn)
//                 {
//                     if (Random.value < randomSpawnChance) shouldSpawn = true;
//                 }

//                 if (shouldSpawn)
//                 {
//                     CreateInitialCellAt(x, z);
//                 }
//             }
//         }
        
//         ProcessChainReaction().Forget(); 
//     }
//     private void CreateInitialCellAt(int x, int z)
//     {
//         GameObject cellObj = Instantiate(cellPrefab, GetWorldPos(x, z), Quaternion.identity, transform);
//         GridCell cell = cellObj.GetComponent<GridCell>();
        
//         cell.Initialize(GenerateRandomIDsForInit());
//         cell.coord = new Vector2Int(x, z);
//         _gridCells[x, z] = cell;

//         for (int lx = 0; lx < 2; lx++)
//             for (int lz = 0; lz < 2; lz++)
//                 _globalMatrix[x * 2 + lx, z * 2 + lz] = cell.GetIDAtLocal(lx, lz);
//     }

//     private List<int> GenerateRandomIDsForInit()
//     {
//         var allIds = cubeSettings.ConvertAll(s => s.id);
//         List<int> result = new List<int>();
//         int count = Random.Range(2, 5);
//         for (int i = 0; i < count; i++) result.Add(allIds[Random.Range(0, allIds.Count)]);
//         return result;
//     }

//     private List<int> GetSafeRandomIDs(int x, int z)
//     {
//         List<int> pool = new List<int>(currentLevel.availableCubeIds);
//         HashSet<int> forbiddenIds = new HashSet<int>();

//         // Kiểm tra cạnh phải của Cell bên trái
//         if (x > 0)
//         {
//             forbiddenIds.Add(_globalMatrix[(x - 1) * 2 + 1, z * 2]);
//             forbiddenIds.Add(_globalMatrix[(x - 1) * 2 + 1, z * 2 + 1]);
//         }

//         // Kiểm tra cạnh trên của Cell bên dưới
//         if (z > 0)
//         {
//             forbiddenIds.Add(_globalMatrix[x * 2, (z - 1) * 2 + 1]);
//             forbiddenIds.Add(_globalMatrix[x * 2 + 1, (z - 1) * 2 + 1]);
//         }

//         // Lọc bỏ các ID bị cấm
//         var safeIds = pool.Where(id => !forbiddenIds.Contains(id)).ToList();
        
//         // Nếu bị cấm hết (hiếm), lấy đại 1 cái trong pool để tránh lỗi
//         if (safeIds.Count == 0) safeIds = pool; 

//         List<int> result = new List<int>();
//         int count = Random.Range(2, 5);
//         for (int i = 0; i < count; i++) 
//             result.Add(safeIds[Random.Range(0, safeIds.Count)]);
            
//         return result;
//     }

//     public Vector3 GetWorldPos(int x, int z) => new Vector3(x * cellSpacing, 0, z * cellSpacing);

//     public void SyncBackFromCell(Vector2Int cellCoord, int[,] localMatrix)
//     {
//         for (int i = 0; i < 2; i++)
//             for (int j = 0; j < 2; j++)
//                 _globalMatrix[cellCoord.x * 2 + i, cellCoord.y * 2 + j] = localMatrix[i, j];
//     }

//     public async UniTask<bool> TryPlaceCell(GridCell cell)
//     {
//         if (_isProcessing) return false;

//         int cx = Mathf.RoundToInt(cell.transform.position.x / cellSpacing);
//         int cz = Mathf.RoundToInt(cell.transform.position.z / cellSpacing);

//         if (cx < 0 || cz < 0 || cx >= _width || cz >= _height) return false;
//         if (currentLevel.startMatrix[cz][cx] == LevelConfigData.GridCellStartStatus.Disable) return false;
//         if (_gridCells[cx, cz] != null) return false;

//         _isProcessing = true;
//         cell.transform.SetParent(transform);
//         cell.transform.position = GetWorldPos(cx, cz);
//         cell.coord = new Vector2Int(cx, cz);
//         _gridCells[cx, cz] = cell;

//         for (int lx = 0; lx < 2; lx++)
//             for (int lz = 0; lz < 2; lz++)
//                 _globalMatrix[cx * 2 + lx, cz * 2 + lz] = cell.GetIDAtLocal(lx, lz);

//         await ProcessChainReaction();

//         if (CheckWinAndLoseCondition.Instance != null)
//             CheckWinAndLoseCondition.Instance.CheckLoseCondition(_gridCells, _width, _height);

//         _isProcessing = false;
//         return true;
//     }

//     private async UniTask ProcessChainReaction()
//     {
//         bool hasChanges = true;
//         while (hasChanges)
//         {
//             hasChanges = false;
//             List<Vector2Int> pointsToClear = FindAllMatchesBFS(out int targetID);

//             if (pointsToClear.Count > 0)
//             {
//                 List<GameObject> cubesToAnimate = new List<GameObject>();
//                 foreach (var p in pointsToClear)
//                 {
//                     int cx = p.x / 2, cz = p.y / 2;
//                     if (_gridCells[cx, cz] != null)
//                     {
//                         GameObject cube = _gridCells[cx, cz].ExtractCubeAt(p.x % 2, p.y % 2);
//                         if (cube != null && !cubesToAnimate.Contains(cube)) cubesToAnimate.Add(cube);
//                     }
//                     _globalMatrix[p.x, p.y] = -1;
//                 }

//                 if (MergeAnimator.Instance != null) await MergeAnimator.Instance.PlayMergeAnimation(cubesToAnimate);
//                 if (CheckWinAndLoseCondition.Instance != null) CheckWinAndLoseCondition.Instance.NotifyCubesCollected(targetID, pointsToClear.Count);

//                 await SyncAllCells();
//                 hasChanges = true;
//                 await UniTask.Delay(200);
//             }
//         }
//     }

//     private List<Vector2Int> FindAllMatchesBFS(out int matchedID)
//     {
//         matchedID = -1;
//         int fullW = _width * 2, fullH = _height * 2;
//         bool[,] visited = new bool[fullW, fullH];

//         for (int x = 0; x < fullW; x++)
//         {
//             for (int z = 0; z < fullH; z++)
//             {
//                 if (visited[x, z] || _globalMatrix[x, z] == -1) continue;

//                 int id = _globalMatrix[x, z];
//                 List<Vector2Int> cluster = new List<Vector2Int>();
//                 Queue<Vector2Int> q = new Queue<Vector2Int>();
//                 q.Enqueue(new Vector2Int(x, z));
//                 visited[x, z] = true;

//                 bool isCrossCell = false;
//                 Vector2Int origin = new Vector2Int(x / 2, z / 2);

//                 while (q.Count > 0)
//                 {
//                     Vector2Int curr = q.Dequeue();
//                     cluster.Add(curr);
//                     if (curr.x / 2 != origin.x || curr.y / 2 != origin.y) isCrossCell = true;

//                     foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
//                     {
//                         int nx = curr.x + d.x, nz = curr.y + d.y;
//                         if (nx >= 0 && nx < fullW && nz >= 0 && nz < fullH && !visited[nx, nz] && _globalMatrix[nx, nz] == id)
//                         {
//                             visited[nx, nz] = true;
//                             q.Enqueue(new Vector2Int(nx, nz));
//                         }
//                     }
//                 }

//                 if (isCrossCell) { matchedID = id; return cluster; }
//             }
//         }
//         return new List<Vector2Int>();
//     }

//     private async UniTask SyncAllCells()
//     {
//         List<UniTask> tasks = new List<UniTask>();
//         for (int x = 0; x < _width; x++)
//         {
//             for (int z = 0; z < _height; z++)
//             {
//                 if (_gridCells[x, z] == null) continue;
//                 int[,] local = new int[2, 2];
//                 bool empty = true;
//                 for (int i = 0; i < 2; i++)
//                     for (int j = 0; j < 2; j++) {
//                         local[i, j] = _globalMatrix[x * 2 + i, z * 2 + j];
//                         if (local[i, j] != -1) empty = false;
//                     }

//                 if (empty) { _gridCells[x, z].SelfDestruct(); _gridCells[x, z] = null; }
//                 else tasks.Add(_gridCells[x, z].UpdateFromLogic(local));
//             }
//         }
//         await UniTask.WhenAll(tasks);
//     }

//     public Color GetColorFromID(int id) => cubeSettings.Find(c => c.id == id).color;
// }