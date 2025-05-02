using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BinarySeperateRoom : MonoBehaviour
{
    [SerializeField] private ProceduralBuildingGenerator _buildingGenerator;
    [SerializeField] Vector2Int mapSize;
    [SerializeField] int roomMinSize;
    [FormerlySerializedAs("RoomPreviewPrefab")] [SerializeField] GameObject roomPreviewPrefab;
    [SerializeField] Material[] previewMaterials;
    [FormerlySerializedAs("CellPrefab")] [SerializeField] GameObject cellPrefab;
    [FormerlySerializedAs("TrianglePrefab")] [SerializeField] GameObject trianglePrefab;
    [FormerlySerializedAs("CellPrefabs")] [SerializeField] CellPrefab cellPrefabs;

    int[,] _cells;
    // Start is called before the first frame update
    void Start()
    {
        ObjectPoolContainer.Instance.InitWithPoolData(_buildingGenerator._poolDatas);
        ObjectPoolContainer.Instance.ResetAll();
        
        Room.MinSize = roomMinSize;
        _cells = new int[mapSize.x, mapSize.y];
        List<Room> rooms = new List<Room>();
        List<Room> generatedRoom = new List<Room>();

        Room rootRoom = new Room(Vector2Int.zero, mapSize);

        rootRoom.Seperate();
        rootRoom.GetChildRoom(rooms);

        int randomRoomCount = rooms.Count * 3 / 10;

        List<int> randomRoomIndex = _getNonoverlappingList(randomRoomCount, rooms.Count);

        List<Vector3> collidorPoints = new List<Vector3>();

        for (int i = 0; i < randomRoomIndex.Count; i++)
        {
            Room currentRoom = rooms[randomRoomIndex[i]];
            generatedRoom.Add(currentRoom);

            Vector2Int center = currentRoom.Center;
            collidorPoints.Add(new Vector3(center.x, 0.0f, center.y));

            currentRoom.FillWithRandomWalk(_cells);
        }

        BowyerWatson bw = new BowyerWatson();
        //var collidors = bw.Calculate(generatedRoom);
        List<Edge> graphEdges = new List<Edge>();

        //foreach (var triangle in collidors)
        //{
        //    graphEdges.AddRange(triangle.Edges);
        //}

        // foreach (var edge in graphEdges)
        // {
        //     edge.ConnectedRoom1.AddNeighbor(edge.ConnectedRoom2, edge);
        //     edge.ConnectedRoom2.AddNeighbor(edge.ConnectedRoom1, edge);
        // }

        List<Edge> traveledEdges = new List<Edge>();
        List<Room> traveledRooms = new List<Room>();

        generatedRoom[0].Travel(traveledRooms, traveledEdges);

        for(int i = 0; i < traveledEdges.Count; i++)
        {
            _fillEdge(traveledEdges[i]);
        }

        foreach (var room in traveledRooms)
        {
            ProceduralBuildingGenerator.Mass mass = new ProceduralBuildingGenerator.Mass();

            mass.FacadeRule = _buildingGenerator._rootRule;
            mass.CornerRule = _buildingGenerator._cornerRule;
            
            mass.CreateWithPrimitiveData(new ProceduralBuildingGenerator.Mass.PrimitiveData
            {
                CornerCount = 4,
                Position = new Vector3(room.Center.x, 0.0f, room.Center.y),
                Radius = 0.0f,
                Size = new Vector3(room.Size.x * 0.7f, UnityEngine.Random.Range(30.0f, 60.0f), room.Size.y * 0.7f),
                Type = ProceduralBuildingGenerator.Mass.PrimitiveType.eRectangle
            });
            
            foreach (var facade in mass._childContexts)
            {
                facade.CreatePrimitive(new GameObject());
            }
        }

        //foreach (var room  in generatedRoom)
        //{
        //    for(int i = 0; i < room.Neighbors.Count; i++)
        //    {
        //        Vector3[] collidorPositions = new Vector3[2];

        //        collidorPositions[0] = room.WorldCenter;
        //        collidorPositions[1] = room.Neighbors[i].WorldCenter;

        //        GameObject collidor = Instantiate(TrianglePrefab);
        //        collidor.GetComponent<LineRenderer>().SetPositions(collidorPositions);
        //    }
            
        //}

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (_cells[x, y] != 1)
                {
                    continue;
                }

                GenerateCell(x, y);
            }
        }
    }

    void GenerateCell(int posx, int posy)
    {
        var ground = Instantiate(cellPrefabs.prefabs[1]);
        ground.transform.position = new Vector3(posx, 0.0f, posy);

        for (int x = posx - 1; x <= posx + 1; x++)
        {
            for(int y = posy - 1; y <= posy + 1; y++)
            {
                if(x < 0 || x >= mapSize.x || y < 0 || y >= mapSize.y || (x == y))
                {
                    continue;
                }

                if(_cells[x, y] == 1)
                {
                    continue;
                }

                var cell = Instantiate(cellPrefabs.prefabs[0]);
                cell.transform.position = new Vector3(x, 0.0f, y);
            }
        }
    }

    void _fillEdge(Edge e)
    {
        int remainCount = 0;
        
        Vector2Int dir = Vector2Int.zero;

        Vector2Int p1 = new Vector2Int((int)e.P1.x, (int)e.P1.z);
        Vector2Int p2 = new Vector2Int((int)e.P2.x, (int)e.P2.z);

        Vector2Int curPos = p1;

        if (p1.x > p2.x)
        {
            dir = Vector2Int.left;
            remainCount = p1.x - p2.x;
        }
        else
        {
            dir = Vector2Int.right;
            remainCount = p2.x - p1.x;
        }

        while(remainCount >= 0)
        {
            curPos += dir;
            _cells[curPos.x, curPos.y] = 1;
            remainCount--;
        }

        if (p1.y > p2.y)
        {
            dir = Vector2Int.down;
            remainCount = p1.y - p2.y;
        }
        else
        {
            dir = Vector2Int.up;
            remainCount = p2.y - p1.y;
        }

        while (remainCount >= 0)
        {
            curPos += dir;
            _cells[curPos.x, curPos.y] = 1;
            remainCount--;
        }
    }

    List<int> _getNonoverlappingList(int count, int max)
    {
        List<int> randomIndexes = new List<int>();

        while (randomIndexes.Count < count)
        {
            int randomIndex = Random.Range(0, max);

            if (randomIndexes.Contains(randomIndex))
            {
                continue;
            }

            randomIndexes.Add(randomIndex);
        }

        return randomIndexes;
    }

    void RoomSeperateTest(List<Room> rooms)
    {
        int curPrevMatIndex = 0;

        for (int i = 0; i < rooms.Count; i++)
        {
            var preview = Instantiate(roomPreviewPrefab);
            preview.transform.position = new Vector3(rooms[i].Origin.x, 0.0f, rooms[i].Origin.y);
            preview.transform.localScale = new Vector3(rooms[i].Size.x, 1.0f, rooms[i].Size.y);
            preview.transform.GetChild(0).GetComponent<MeshRenderer>().material = previewMaterials[curPrevMatIndex];
            curPrevMatIndex++;

            if (curPrevMatIndex >= previewMaterials.Length)
            {
                curPrevMatIndex = 0;
            }
        }
    }

    
}
