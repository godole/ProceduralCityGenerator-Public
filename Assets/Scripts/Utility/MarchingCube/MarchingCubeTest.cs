using System.Collections.Generic;
using UnityEngine;

namespace Utility.MarchingCube
{
    public class MarchingCubeTest : MonoBehaviour
    {
        [SerializeField] private MarchingCubeData _marchingCubeData;
    
        private int[,] _tilemap;
        private int[,] _marchingMap;

        private void Start()
        {
            _CalculateMarchingEdge();
            Vector2Int tilemapSize = Vector2Int.zero;
            var tilePositionList = new List<Vector2Int>();
        
            Transform tilemapParent = transform.GetChild(0);

            for (int i = 0; i < tilemapParent.childCount; i++)
            {
                Transform tile = tilemapParent.GetChild(i);

                Vector3 position = tile.position;
                Vector2Int tilemapPosition = new Vector2Int(Mathf.RoundToInt(position.x),
                    Mathf.RoundToInt(position.z));
            
                tilePositionList.Add(tilemapPosition);
            
                tilemapSize.x = Mathf.Max(tilemapSize.x, tilemapPosition.x);
                tilemapSize.y = Mathf.Max(tilemapSize.y, tilemapPosition.y);
            }

            _tilemap = new int[tilemapSize.x + 2, tilemapSize.y + 2];

            foreach (Vector2Int tilePosition in tilePositionList)
            {
                _tilemap[tilePosition.x, tilePosition.y] = 1;
            }

            MarchingCube marchingCube = new();
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
                MarchingCubeData.EdgePositions edgePositionData = new()
                {
                    Positions = new List<Vector3>()
                };
            
                _marchingCubeData.EdgePositionsList.Add(edgePositionData);
            
                GameObject segment = Instantiate(lookupSegment);
                LineRenderer segmentLineRenderer = segment.GetComponent<LineRenderer>();

                if (segmentLineRenderer == null)
                {
                    continue;
                }

                for (int i = 0; i < segmentLineRenderer.positionCount; i++)
                {
                    edgePositionData.Positions.Add(segmentLineRenderer.GetPosition(i) * 10.0f);
                }

                for (int i = 0; i < segment.transform.childCount; i++)
                {
                    LineRenderer childRenderer = segment.transform.GetChild(i).GetComponent<LineRenderer>();
                
                    for (int j = 0; j < childRenderer.positionCount; j++)
                    {
                        edgePositionData.Positions.Add(childRenderer.GetPosition(j) * 10.0f);
                    }
                }
            }
        }
    }
}
