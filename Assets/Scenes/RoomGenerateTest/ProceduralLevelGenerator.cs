using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ProceduralLevelGenerator : MonoBehaviour
{
    [FormerlySerializedAs("Prefab")] [SerializeField] GameObject prefab;
    [FormerlySerializedAs("WallPrefab")] [SerializeField] GameObject wallPrefab;
    [SerializeField] Vector2Int mapSize;
    [SerializeField] int tileCount;
    [SerializeField] int iterationCount;

    int[,] _cells;
    GameObject[,] _cellObjects;

    int _remainTileCount = 0;


    // Start is called before the first frame update
    void Start()
    {
        _cells = new int[mapSize.x, mapSize.y];
        _cellObjects = new GameObject[mapSize.x, mapSize.y];

        //_randomWalk();

        for(int x = 0; x < mapSize.x; x++)
        {
            for(int y = 0; y < mapSize.y; y++)
            {
                GameObject cell = Instantiate(prefab);
                cell.transform.position = new Vector3(x, 0, y);
                cell.gameObject.SetActive(false);
                _cellObjects[x, y] = cell;
            }
        }

        _remainTileCount = tileCount;

        while(_remainTileCount > 0)
        {
            Iteration();
        }

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                _cellObjects[x, y].SetActive(_cells[x, y] == 1);
            }
        }
    }

    void _addWall(int x, int y)
    {
        if (x < 0 || x >= mapSize.x ||
                y < 0 || y >= mapSize.y)
        {
            return;
        }

        if (_cells[x, y] == 1)
            return;

        _cells[x, y] = 2;
    }

    public void Iteration()
    {
        Vector2Int curPos = new Vector2Int(mapSize.x / 2, mapSize.y / 2);
        Vector2Int nextPos = curPos;

        for (int generateCount = 0; generateCount < mapSize.x; generateCount++)
        {
            int randomDir = UnityEngine.Random.Range(0, 4);
            Vector2Int dir = Vector2Int.zero;

            switch (randomDir)
            {
                //up
                case 0:
                    dir.y = 1;
                    break;
                //down
                case 1:
                    dir.y = -1;
                    break;
                //left
                case 2:
                    dir.x = -1;
                    break;
                //right
                case 3:
                    dir.x = 1;
                    break;
                default:
                    break;
            }

            nextPos += dir;
            try
            {
                if (_cells[nextPos.x, nextPos.y] != 1)
                {
                    _remainTileCount--;
                }
                _cells[nextPos.x, nextPos.y] = 1;
            }
            catch(IndexOutOfRangeException)
            {
                break;
            }
        }

        
    }
}
