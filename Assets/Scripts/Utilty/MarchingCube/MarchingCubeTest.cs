using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubeTest : MonoBehaviour
{
    [SerializeField] private MarchingCubeData _marchingCubeData;
    
    private int[,] _tilemap;
    private int[,] _marchingMap;
    
    // Start is called before the first frame update
    void Start()
    {
        _CalculateMarchingEdge();
        Vector2Int tilemapSize = Vector2Int.zero;
        List<Vector2Int> tilePositionList = new List<Vector2Int>();
        
        var tilemapParent = transform.GetChild(0);

        for (int i = 0; i < tilemapParent.childCount; i++)
        {
            var tile = tilemapParent.GetChild(i);

            var position = tile.position;
            Vector2Int tilemapPosition = new Vector2Int(Mathf.RoundToInt(position.x),
                Mathf.RoundToInt(position.z));
            
            tilePositionList.Add(tilemapPosition);
            
            tilemapSize.x = Mathf.Max(tilemapSize.x, tilemapPosition.x);
            tilemapSize.y = Mathf.Max(tilemapSize.y, tilemapPosition.y);
        }

        _tilemap = new int[tilemapSize.x + 2, tilemapSize.y + 2];

        foreach (var tilePosition in tilePositionList)
        {
            _tilemap[tilePosition.x, tilePosition.y] = 1;
        }

        var marchingCube = new MarchingCube();
        _marchingMap = marchingCube.ConvertMarching(_tilemap);

        for (int x = 0; x < _marchingMap.GetLength(0); x++)
        {
            for (int y = 0; y < _marchingMap.GetLength(1); y++)
            {
                int marchingCubeIndex = _marchingMap[x, y];

                if (marchingCubeIndex >= _marchingCubeData.LookupTable.Count)
                {
                    continue;
                }
                
                var marchingCubeSegment = Instantiate(_marchingCubeData.LookupTable[marchingCubeIndex]);
                marchingCubeSegment.transform.position = new Vector3(x, 0.0f, y);
            }
        }
    }

    void _CalculateMarchingEdge()
    {
        foreach (var lookupSegment in _marchingCubeData.LookupTable)
        {
            var edgePositionData = new MarchingCubeData.EdgePositions
            {
                Positions = new List<Vector3>()
            };
            
            _marchingCubeData.EdgePositionsList.Add(edgePositionData);
            
            GameObject segment = Instantiate(lookupSegment);
            var renderer = segment.GetComponent<LineRenderer>();

            if (renderer == null)
            {
                continue;
            }

            for (int i = 0; i < renderer.positionCount; i++)
            {
                edgePositionData.Positions.Add(renderer.GetPosition(i) * 10.0f);
            }

            for (int i = 0; i < segment.transform.childCount; i++)
            {
                var childRenderer = segment.transform.GetChild(i).GetComponent<LineRenderer>();
                
                for (int j = 0; j < childRenderer.positionCount; j++)
                {
                    edgePositionData.Positions.Add(childRenderer.GetPosition(j) * 10.0f);
                }
            }
        }
    }
}
