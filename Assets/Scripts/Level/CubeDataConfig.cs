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
        // Sử dụng Find để tìm ID (tối ưu cho list nhỏ)
        // Nếu list có hàng trăm màu, hãy dùng Dictionary để cache
        var data = configCubeData.Find(x => x.id == id);
        
        // Nếu tìm thấy thì trả về màu, không thì trả về màu trắng mặc định
        return data.id == id ? data.color : Color.white;
    }
}
