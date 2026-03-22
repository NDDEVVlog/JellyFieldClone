using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelConfigData))]
public class LevelEditor : Editor
{
    SerializedProperty missionGoalsProp;
    SerializedProperty availableCubeIdsProp; 

    private void OnEnable()
    {
        missionGoalsProp = serializedObject.FindProperty("missionGoals");
        availableCubeIdsProp = serializedObject.FindProperty("availableCubeIds"); // Gán nó ở đây
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update(); 
        LevelConfigData data = (LevelConfigData)target;

        // --- PHẦN 1: CẤU HÌNH MÀU SẮC (PALETTE) ---
        EditorGUILayout.LabelField("LEVEL COLOR PALETTE", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Nhập các ID màu sẽ xuất hiện trong màn chơi này.", MessageType.Info);
        EditorGUILayout.PropertyField(availableCubeIdsProp, true); // VẼ DANH SÁCH MÀU Ở ĐÂY
        EditorGUILayout.Space(15);

        // --- PHẦN 2: VẼ MISSION GOALS ---
        EditorGUILayout.LabelField("MISSION GOALS", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(missionGoalsProp, true); 
        EditorGUILayout.Space(15);

        // --- PHẦN 3: CẤU HÌNH GRID ---
        EditorGUILayout.LabelField("GRID SETTINGS", EditorStyles.boldLabel);
        data.width = EditorGUILayout.IntField("Width", data.width);
        data.height = EditorGUILayout.IntField("Height", data.height);

        // Đảm bảo ma trận được nạp đúng kích thước
        if (data.startMatrix == null || data.startMatrix.Length != data.height || 
           (data.startMatrix.Length > 0 && data.startMatrix[0].Length != data.width))
        {
            data.LoadMatrix();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("GRID DESIGNER (Click to toggle)", EditorStyles.miniBoldLabel);

        // --- PHẦN 4: VẼ GRID EDITOR ---
        for (int y = data.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < data.width; x++)
            {
                var status = data.startMatrix[y][x];
                GUI.color = GetColor(status);

                if (GUILayout.Button("", GUILayout.Width(35), GUILayout.Height(35)))
                {
                    Undo.RecordObject(data, "Change Cell Status");
                    data.startMatrix[y][x] = (LevelConfigData.GridCellStartStatus)(((int)status + 1) % 4);
                    data.SaveMatrix();
                    EditorUtility.SetDirty(data);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUI.color = Color.white;
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Reset Grid Layout", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Reset Grid", "Bạn có chắc chắn muốn xóa toàn bộ thiết kế Grid?", "Yes", "No"))
            {
                data.InitializeMatrix();
                data.SaveMatrix();
                EditorUtility.SetDirty(data);
            }
        }

        serializedObject.ApplyModifiedProperties(); 
    }

    private Color GetColor(LevelConfigData.GridCellStartStatus status)
    {
        return status switch
        {
            LevelConfigData.GridCellStartStatus.AllowToSpawn => new Color(0.4f, 1f, 0.4f), 
            LevelConfigData.GridCellStartStatus.Disable => new Color(1f, 0.4f, 0.4f),      
            LevelConfigData.GridCellStartStatus.LeftEmptyAtStart => new Color(1f, 1f, 0.4f), 
            LevelConfigData.GridCellStartStatus.AlwaysHasBlockInit => new Color(0.4f, 0.8f, 1f),
            _ => Color.white
        };
    }
}   