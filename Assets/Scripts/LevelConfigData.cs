using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "LatteGames/LevelConfig")]
public class LevelConfigData : ScriptableObject
{
    [System.Serializable]
    public struct MissionGoal
    {
        public int cubeId;
        public int targetAmount;
    }

    public enum GridCellStartStatus { AllowToSpawn, Disable, LeftEmptyAtStart,AlwaysHasBlockInit}

    [Header("Missions")]
    public List<MissionGoal> missionGoals = new List<MissionGoal>();

    [Header("Grid Layout")]
    public int width = 4;
    public int height = 4;

    public GridCellStartStatus[][] startMatrix;

    [HideInInspector] [SerializeField] private GridCellStartStatus[] serializedData;

    public void InitializeMatrix()
    {
        startMatrix = new GridCellStartStatus[height][];
        for (int i = 0; i < height; i++)
            startMatrix[i] = new GridCellStartStatus[width];
    }

    public void LoadMatrix()
    {
        InitializeMatrix();
        if (serializedData == null || serializedData.Length != width * height) return;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                startMatrix[y][x] = serializedData[y * width + x];
    }

    public void SaveMatrix()
    {
        serializedData = new GridCellStartStatus[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                serializedData[y * width + x] = startMatrix[y][x];
    }
}