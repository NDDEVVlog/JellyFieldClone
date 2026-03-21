using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelConfigData))]
public class LevelEditor : Editor
{
    SerializedProperty missionGoalsProp;

    private void OnEnable()
    {
        missionGoalsProp = serializedObject.FindProperty("missionGoals");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // Cập nhật dữ liệu từ file
        LevelConfigData data = (LevelConfigData)target;

        // --- PHẦN 1: VẼ MISSION GOALS ---
        EditorGUILayout.LabelField("MISSION GOALS", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(missionGoalsProp, true); 
        EditorGUILayout.Space(15);

        // --- PHẦN 2: CẤU HÌNH GRID ---
        EditorGUILayout.LabelField("GRID SETTINGS", EditorStyles.boldLabel);
        data.width = EditorGUILayout.IntField("Width", data.width);
        data.height = EditorGUILayout.IntField("Height", data.height);

        // Logic nạp Matrix
        if (data.startMatrix == null || data.startMatrix.Length != data.height || 
           (data.startMatrix.Length > 0 && data.startMatrix[0].Length != data.width))
        {
            data.LoadMatrix();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("GRID DESIGNER (Click to toggle)", EditorStyles.miniBoldLabel);

        // --- PHẦN 3: VẼ GRID EDITOR ---
        for (int y = data.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < data.width; x++)
            {
                var status = data.startMatrix[y][x];
                GUI.color = GetColor(status);

                if (GUILayout.Button("", GUILayout.Width(35), GUILayout.Height(35)))
                {
                    Undo.RecordObject(data, "Change Cell Status"); // Cho phép Ctrl+Z
                    data.startMatrix[y][x] = (LevelConfigData.GridCellStartStatus)(((int)status + 1) % 3);
                    data.SaveMatrix();
                    EditorUtility.SetDirty(data);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUI.color = Color.white;
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Reset Grid Layout"))
        {
            if (EditorUtility.DisplayDialog("Reset Grid", "Are you sure?", "Yes", "No"))
            {
                data.InitializeMatrix();
                data.SaveMatrix();
                EditorUtility.SetDirty(data);
            }
        }

        serializedObject.ApplyModifiedProperties(); // Lưu thay đổi của Mission Goals
    }

    private Color GetColor(LevelConfigData.GridCellStartStatus status)
    {
        return status switch
        {
            LevelConfigData.GridCellStartStatus.AllowToSpawn => new Color(0.4f, 1f, 0.4f), // Xanh lá
            LevelConfigData.GridCellStartStatus.Disable => new Color(1f, 0.4f, 0.4f),      // Đỏ
            LevelConfigData.GridCellStartStatus.LeftEmptyAtStart => new Color(1f, 1f, 0.4f), // Vàng
            LevelConfigData.GridCellStartStatus.AlwaysHasBlockInit => new Color(0.4f, 0.8f, 1f), // Xanh dương
            _ => Color.white
        };
    }
}