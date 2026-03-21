// using UnityEngine;
// using UnityEditor;

// [CustomEditor(typeof(GridManager))]
// public class GridEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         DrawDefaultInspector();
//         GridManager gm = (GridManager)target;
//         GUILayout.Space(10);
//         for (int z = gm.height - 1; z >= 0; z--)
//         {
//             EditorGUILayout.BeginHorizontal();
//             for (int x = 0; x < gm.width; x++)
//             {
//                 Vector2Int c = new Vector2Int(x, z);
//                 bool dis = gm.disabledCells.Exists(idx => idx == c);
//                 GUI.color = dis ? Color.red : Color.green;
//                 if (GUILayout.Button($"{x},{z}", GUILayout.Width(35), GUILayout.Height(35)))
//                 {
//                     Undo.RecordObject(gm, "Toggle Cell");
//                     if (dis) gm.disabledCells.Remove(c); else gm.disabledCells.Add(c);
//                 }
//             }
//             EditorGUILayout.EndHorizontal();
//         }
//         GUI.color = Color.white;
//     }
// }