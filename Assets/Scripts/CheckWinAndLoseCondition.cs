using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CheckWinAndLoseCondition : MonoBehaviour
{
    public static CheckWinAndLoseCondition Instance { get; private set; }

    private Dictionary<int, int> _currentProgress = new Dictionary<int, int>();
    private LevelConfigData _config;
    private bool _isGameOver = false;

    void Awake() => Instance = this;

    public void Setup(LevelConfigData config)
    {
        _config = config;
        _currentProgress.Clear();
        _isGameOver = false;

        // Khởi tạo tiến độ cho từng mục tiêu
        foreach (var goal in config.missionGoals)
        {
            _currentProgress[goal.cubeId] = 0;
            Debug.Log($"Mission: Collect {goal.targetAmount} cubes of ID {goal.cubeId}");
        }
    }

    public void NotifyCubesCollected(int cubeId, int amount)
    {
        if (_isGameOver || !_currentProgress.ContainsKey(cubeId)) return;

        _currentProgress[cubeId] += amount;
        
        // Log tiến độ (Sau này bạn thay bằng cập nhật UI)
        int target = _config.missionGoals.First(g => g.cubeId == cubeId).targetAmount;
        Debug.Log($"Progress ID {cubeId}: {_currentProgress[cubeId]}/{target}");

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        bool isAllFinished = true;
        foreach (var goal in _config.missionGoals)
        {
            if (_currentProgress[goal.cubeId] < goal.targetAmount)
            {
                isAllFinished = false;
                break;
            }
        }

        if (isAllFinished)
        {
            _isGameOver = true;
            Debug.Log("<color=green>LEVEL WIN!</color>");
            // Trigger UI Win Panel ở đây
        }
    }

    public void CheckLoseCondition(GridCell[,] grid, int w, int h)
    {
        if (_isGameOver) return;

        bool hasEmptySpace = false;
        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                // Chỉ kiểm tra những ô KHÔNG bị Disable
                if (_config.startMatrix[z][x] != LevelConfigData.GridCellStartStatus.Disable)
                {
                    if (grid[x, z] == null)
                    {
                        hasEmptySpace = true;
                        break;
                    }
                }
            }
        }

        if (!hasEmptySpace)
        {
            _isGameOver = true;
            Debug.Log("<color=red>GAME OVER: NO MORE MOVES!</color>");
            // Trigger UI Lose Panel ở đây
        }
    }
}