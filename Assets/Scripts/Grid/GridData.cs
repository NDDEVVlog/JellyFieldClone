using System.Collections.Generic;
using UnityEngine;

public class GridData
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    private int[,] _matrix; 

    public GridData(int w, int h)
    {
        Width = w;
        Height = h;
        _matrix = new int[w * 2, h * 2];
        for (int i = 0; i < w * 2; i++)
            for (int j = 0; j < h * 2; j++) _matrix[i, j] = -1;
    }

    public void SetValue(int gx, int gz, int id) => _matrix[gx, gz] = id;
    public int GetValue(int gx, int gz) => (gx < 0 || gz < 0 || gx >= Width * 2 || gz >= Height * 2) ? -1 : _matrix[gx, gz];

    public List<Vector2Int> FindMatches(out int matchedID)
    {
        matchedID = -1;
        int fullW = Width * 2, fullH = Height * 2;
        bool[,] visited = new bool[fullW, fullH];

        for (int x = 0; x < fullW; x++)
        {
            for (int z = 0; z < fullH; z++)
            {
                if (visited[x, z] || _matrix[x, z] == -1) continue;

                int id = _matrix[x, z];
                var cluster = new List<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(new Vector2Int(x, z));
                visited[x, z] = true;

                bool isCrossCell = false;
                Vector2Int originCell = new Vector2Int(x / 2, z / 2);

                while (queue.Count > 0)
                {
                    var curr = queue.Dequeue();
                    cluster.Add(curr);
                    if (curr.x / 2 != originCell.x || curr.y / 2 != originCell.y) isCrossCell = true;

                    foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    {
                        int nx = curr.x + d.x, nz = curr.y + d.y;
                        if (nx >= 0 && nx < fullW && nz >= 0 && nz < fullH && !visited[nx, nz] && _matrix[nx, nz] == id)
                        {
                            visited[nx, nz] = true;
                            queue.Enqueue(new Vector2Int(nx, nz));
                        }
                    }
                }

                if (isCrossCell) { matchedID = id; return cluster; }
            }
        }
        return new List<Vector2Int>();
    }
}