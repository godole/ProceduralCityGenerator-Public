using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room 
{
    public static int MinSize;

    Vector2Int _origin;
    Vector2Int _size;

    Room _child1;
    Room _child2;

    bool _isLeaf = true;

    List<Edge> _neighborEdges = new List<Edge>();
    List<Room> _neighbors = new List<Room>();

    public bool IsLeaf { get => _isLeaf; }
    public Vector2Int Origin { get => _origin; set => _origin = value; }
    public Vector2Int Size { get => _size; set => _size = value; }
    public Vector2Int Center
    {
        get
        {
            return new Vector2Int(Origin.x + Size.x / 2, Origin.y + Size.y / 2);
        }
    }
    public Vector3 WorldCenter
    {
        get
        {
            Vector2Int center = Center;

            return new Vector3(center.x, 0.0f, center.y);
        }
    }

    public List<Room> Neighbors { get => _neighbors; }

    public Room(Vector2Int origin, Vector2Int size)
    {
        this._origin = origin;
        this._size = size;
    }

    public void Seperate()
    {
        if(_size.x < MinSize || _size.y < MinSize)
        {
            return;
        }

        if (_size.x >= _size.y * 2)
        {
            SeperateVertical();
            return;
        }

        if (_size.y >= _size.x * 2)
        {
            SeperateHorizontal();
            return;
        }

        int randomSeperateDir = Random.Range(0, 2);

        if(randomSeperateDir == 0)
        {
            SeperateHorizontal();
        }

        else
        {
            SeperateVertical();
        }
    }

    void SeperateHorizontal()
    {
        float randomRatio = Random.Range(0.1f, 0.9f);

        Vector2Int bottomRoomOrigin = _origin + Vector2Int.one;
        Vector2Int bottomRoomSize = new Vector2Int(_size.x, Mathf.RoundToInt(_size.y * randomRatio)) - (Vector2Int.one * 2);
        
        Vector2Int topRoomOrigin = new Vector2Int(_origin.x, _origin.y + bottomRoomSize.y) + Vector2Int.one;
        Vector2Int topRoomSize = new Vector2Int(_size.x, _size.y - bottomRoomSize.y) - (Vector2Int.one * 2);

        _child1 = new Room(bottomRoomOrigin, bottomRoomSize);
        _child2 = new Room(topRoomOrigin, topRoomSize);

        _child1.Seperate();
        _child2.Seperate();

        _isLeaf = false;
    }

    public void FillWithRandomWalk(int[,] cells)
    {
        int remainTileCount = (_size.x * _size.y * 7) / 10;
        remainTileCount = Mathf.Clamp(remainTileCount, 0, 1000);
        

        int walkCount = Mathf.Max(_size.x, _size.y);

        for (int x = Origin.x; x < Origin.x + _size.x; x++)
        {
            for (int y = Origin.y; y < Origin.y + _size.y; y++)
            {
                cells[x, y] = 2;
            }
        }

        // while (remainTileCount > 0)
        // {
        //     Vector2Int curPos = new Vector2Int(_size.x / 2, _size.y / 2) + Origin;
        //     Vector2Int nextPos = curPos;
        //
        //     for (int generateCount = 0; generateCount < walkCount; generateCount++)
        //     {
        //         int randomDir = UnityEngine.Random.Range(0, 4);
        //         Vector2Int dir = Vector2Int.zero;
        //
        //         switch (randomDir)
        //         {
        //             //up
        //             case 0:
        //                 dir.y = 1;
        //                 break;
        //             //down
        //             case 1:
        //                 dir.y = -1;
        //                 break;
        //             //left
        //             case 2:
        //                 dir.x = -1;
        //                 break;
        //             //right
        //             case 3:
        //                 dir.x = 1;
        //                 break;
        //             default:
        //                 break;
        //         }
        //
        //         nextPos = curPos + dir;
        //         try
        //         {
        //             if(nextPos.x < Origin.x || nextPos.x > Origin.x + Size.x ||
        //                 nextPos.y < Origin.y || nextPos.y > Origin.y + Size.y)
        //             {
        //                 continue;
        //             }
        //
        //             if (cells[nextPos.x, nextPos.y] != 1)
        //             {
        //                 remainTileCount--;
        //             }
        //             cells[nextPos.x, nextPos.y] = 1;
        //         }
        //         catch (System.IndexOutOfRangeException ext)
        //         {
        //             break;
        //         }
        //
        //         curPos = nextPos;
        //     }
        // }
    }

    void SeperateVertical()
    {
        float randomRatio = Random.Range(0.1f, 0.9f);

        Vector2Int leftRoomOrigin = _origin + Vector2Int.one;
        Vector2Int leftRoomSize = new Vector2Int(Mathf.RoundToInt(_size.x * randomRatio), _size.y) - (Vector2Int.one * 2);
        Vector2Int rightRoomOrigin = new Vector2Int(_origin.x + leftRoomSize.x, _origin.y) + Vector2Int.one;
        Vector2Int rightRoomSize = new Vector2Int(_size.x - leftRoomSize.x, _size.y) - (Vector2Int.one * 2);

        _child1 = new Room(leftRoomOrigin, leftRoomSize);
        _child2 = new Room(rightRoomOrigin, rightRoomSize);

        _child1.Seperate();
        _child2.Seperate();

        _isLeaf = false;
    }

    public void GetChildRoom(List<Room> rooms)
    {
        if(_child1 != null)
        {
            if(_child1.IsLeaf)
            {
                rooms.Add(_child1);
            }
            
            _child1.GetChildRoom(rooms);
        }

        if(_child2 != null)
        {
            if (_child2.IsLeaf)
            {
                rooms.Add(_child2);
            }
            _child2.GetChildRoom(rooms);
        }
    }

    public void AddNeighbor(Room room, Edge edge)
    {
        if(_neighbors.Contains(room))
        {
            return;
        }

        _neighbors.Add(room);
        _neighborEdges.Add(edge);
    }

    public void Travel(List<Room> traveledRoom, List<Edge> traveledEdges)
    {
        for(int i = 0; i < _neighbors.Count; i++)
        {
            Room currentTravelRoom = _neighbors[i];
            Edge currentTravelEdge = _neighborEdges[i];

            if (traveledRoom.Contains(currentTravelRoom))
            {
                continue;
            }

            traveledRoom.Add(currentTravelRoom);
            traveledEdges.Add(currentTravelEdge);

            currentTravelRoom.Travel(traveledRoom, traveledEdges);
        }
    }
}
