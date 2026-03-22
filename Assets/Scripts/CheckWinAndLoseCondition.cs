using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class CheckWinAndLoseCondition : MonoBehaviour
{
    public static CheckWinAndLoseCondition Instance { get; private set; }

    private Dictionary<int, int> _currentProgress = new Dictionary<int, int>();
    private bool _isGameOver = false;
    private LevelConfigData _currentLevel;

    public UnityEvent<int,int> onCollect;

    public UnityEvent winEvent;
    public UnityEvent loseEvent;

    void Awake() => Instance = this;

    void Start()
    {
        LevelManager.Instance.BeforeGameStart += BeforeGameStart;
    }

    public void BeforeGameStart(CubeDataConfig cubeDataConfig, LevelConfigData levelConfigData)
    {
        _currentLevel = levelConfigData;
        _currentProgress.Clear();
        _isGameOver = false;

        foreach (var goal in _currentLevel.missionGoals)
        {
            _currentProgress[goal.cubeId] = 0;
        }
    }

    public void NotifyCubesCollected(int cubeId, int amount)
    {   
        Debug.Log("collect id :" +cubeId+ "with amount:"+amount);
        if (_isGameOver || !_currentProgress.ContainsKey(cubeId)) return;

        _currentProgress[cubeId] += amount;
        
        // Find the target for this specific ID
        int target = _currentLevel.missionGoals.First(g => g.cubeId == cubeId).targetAmount;
        int remaining = target - _currentProgress[cubeId];

        // Update the UI
        //BlockCountManager.Instance.UpdateGoalUI(cubeId, remaining);
        onCollect?.Invoke(cubeId,amount);

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        bool isAllFinished = true;
        foreach (var goal in _currentLevel.missionGoals)
        {
            if (_currentProgress[goal.cubeId] < goal.targetAmount)
            {
                isAllFinished = false;
                break;
            }
        }

        if (isAllFinished)
        {   
            winEvent?.Invoke();
            _isGameOver = true;
            Debug.Log("<color=green>LEVEL WIN!</color>");
            // LevelManager.Instance.LoadNextLevel(); // Call this when user clicks a button
        }
    }

    // Call this from your Grid Logic whenever a move is made
    public void CheckLoseCondition(int emptySpacesCount)
    {
        if (_isGameOver) return;

        if (emptySpacesCount <= 0)
        {   loseEvent?.Invoke();
            _isGameOver = true;
            Debug.Log("<color=red>GAME OVER: NO MORE MOVES!</color>");
        }
    }
}