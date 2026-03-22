using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private List<LevelConfigData> levels;
    private int _currentLevelIndex = 0;

    void Awake() => Instance = this;

    public void Start()
    {
        StartLevel(0);
    }

    public void LoadNextLevel()
    {
        _currentLevelIndex++;
        if (_currentLevelIndex < levels.Count) StartLevel(_currentLevelIndex);
    }

    public void StartLevel(int index)
    {
        _currentLevelIndex = index;
        GridController.Instance.InitializeLevel(levels[index]);
    }
}