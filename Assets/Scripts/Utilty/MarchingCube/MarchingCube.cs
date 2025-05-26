using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;

public class MarchingCube
{
    public int[,] ConvertMarching(List<Vector2Int> positions)
    {
        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int max = Vector2Int.zero;
        
        foreach (var position in positions)
        {
            min = Vector2Int.Min(position, min);
            max = Vector2Int.Max(position, max);
        }

        min -= Vector2Int.one;
        max += Vector2Int.one * 2;

        int[,] tempTilemap = new int[max.x - min.x, max.y - min.y];

        foreach (var position in positions)
        {
            tempTilemap[position.x - min.x, position.y - min.y] = 1;
        }

        return ConvertMarching(tempTilemap, Vector2Int.zero, max - min);
    }
    public int[,] ConvertMarching(int[,] tilemap)
    {
        int xMax = tilemap.GetLength(0) - 1;
        int yMax = tilemap.GetLength(1) - 1;

        return ConvertMarching(tilemap, Vector2Int.zero, new Vector2Int(xMax, yMax));
    }
    public int[,] ConvertMarching(int[,] tilemap, Vector2Int max)
    {
        return ConvertMarching(tilemap, Vector2Int.zero, max);
    }
    public int[,] ConvertMarching(int[,] tilemap, Vector2Int min, Vector2Int max)
    {
        int[,] result = new int[max.x, max.y];

        for (int x = min.x; x < max.x - 1; x++)
        {
            for (int y = min.y; y < max.y - 1; y++)
            {
                int marchingIndex = 0;

                marchingIndex += tilemap[x, y + 1];
                marchingIndex += tilemap[x + 1, y + 1] << 1;
                marchingIndex += tilemap[x + 1, y] << 2;
                marchingIndex += tilemap[x, y] << 3;

                result[x - min.x, y - min.y] = marchingIndex;
            }
        }

        return result;
    }
}
