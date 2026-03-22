using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct CubeData
{
    public int id;
    public Color color;
}
[CreateAssetMenu(fileName = "Default", menuName = "Jelly/CubeDataConfig")]

public class CubeDataConfig : ScriptableObject
{
    public List<CubeData> configCubeData;


    public Color GetColor(int id)
    {
        var data = configCubeData.Find(x => x.id == id);
        return data.id == id ? data.color : Color.white;
    }
}
