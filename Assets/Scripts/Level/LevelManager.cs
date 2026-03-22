using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private List<LevelConfigData> levels;
    [SerializeField] private CubeDataConfig cubeDataConfig;
    public int _currentLevelIndex = 0;

    public Action<CubeDataConfig,LevelConfigData> BeforeGameStart;

    public UnityEvent OnStartLevel;

    void Awake() => Instance = this;

    public void Start()
    {
        BeforeGameStart?.Invoke(cubeDataConfig,levels[_currentLevelIndex]);
        OnStartLevel?.Invoke();
    }
    public void OnInvokeEvent()
    {
        OnStartLevel?.Invoke();
    }


    public async void LoadNextLevel()
    {   
        await UniTask.WaitForSeconds(1f);
        _currentLevelIndex++;
        if (_currentLevelIndex < levels.Count) StartLevel(_currentLevelIndex);
    }

    public void StartLevel(int index)
    {
        _currentLevelIndex = index;
        BeforeGameStart?.Invoke(cubeDataConfig,levels[_currentLevelIndex]);
    }

    public async void Retry()
    {
        await UniTask.WaitForSeconds(1f);
        BeforeGameStart?.Invoke(cubeDataConfig,levels[_currentLevelIndex]);
    }

    public CubeDataConfig GetCubeData() => cubeDataConfig;

}