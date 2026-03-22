using UnityEngine;

public class UIManager : MonoBehaviour
{
    private CubeDataConfig cubeConfig;
    private LevelConfigData _currentLevel;

    public UIManager Instance { get; private set; }
    public BlockCountManager blockCountManager;
    
    void Start()
    {
        LevelManager.Instance.BeforeGameStart+= BeforeGameStart;
    }

    public void BeforeGameStart(CubeDataConfig cubeDataConfig, LevelConfigData levelConfigData)
    {
        cubeConfig = cubeDataConfig;
        _currentLevel = levelConfigData;

        blockCountManager.SpawnBlock( cubeDataConfig,  levelConfigData);
    }
}
