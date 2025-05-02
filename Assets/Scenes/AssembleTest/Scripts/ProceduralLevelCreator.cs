using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using WaveFunctionCollapse;
using Debug = UnityEngine.Debug;

public class ProceduralLevelCreator : MonoBehaviour
{
    class Area
    {
        public List<Vector2Int> Points = new List<Vector2Int>();
        public List<Vector2Int> edgePoints = new List<Vector2Int>();
        public List<Vector2Int> groundPoints = new List<Vector2Int>();
        public List<Vector3> WorldPoints = new List<Vector3>();
        public List<Vector3> edgeWorldPoints = new List<Vector3>();
        public List<Triangle> Triangles;
        public Vector2Int Origin;
        public Vector2Int Size;

        public bool ContainsPosition(Vector2Int position)
        {
            return Points.Contains(position);
        }
    }

    private const int Empty = 0;
    private const int Ground = 1;
    
    //common
    HashSet<Vector3> _points = new HashSet<Vector3>();
    HashSet<Vector3> _groundPoints = new HashSet<Vector3>();
    private int[,] outputCells;

    private List<Area> _areas = new List<Area>();

    //wfc
    PatternManager _patternManager;
    [SerializeField] int SamplingSize;
    WaveFunctionCollapse.WaveFunctionCollapse _waveFunctionCollapse;
    [SerializeField] private Texture2D _inputTexture;
    private Vector2Int _totalSamplingSize;
    [SerializeField] Vector2Int _outputSize;
    [SerializeField] GameObject _wfcOutputSampleObject;

    //voronoi;
    BowyerWatson _bowyerWatson = new BowyerWatson();
    [SerializeField] GameObject _simpleLineRenderer;
    
    //marching Square
    [SerializeField] private MarchingCubeData _marchingCubeData;
    
    //Building Generator
    [SerializeField] private ProceduralBuildingGenerator _buildingGenerator;

    private Vector2Int[] _nearPosition = new[]
    {
        new Vector2Int(-1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
    };

    // Start is called before the first frame update
    void Start()
    {
        ResetOutput();
        StartCoroutine(_cGenerateWorld());
    }

    IEnumerator _cGenerateWorld()
    {
        ObjectPoolContainer.Instance.InitWithPoolData(_buildingGenerator._poolDatas);
        ObjectPoolContainer.Instance.ResetAll();
        
        outputCells = new int[_outputSize.x, _outputSize.y];
        
         yield return _PropagateStep();
        

        for (int x = 0; x < _outputSize.x; x++)
        {
            for (int y = 0; y < _outputSize.y; y++)
            {
                if (outputCells[x, y] == Empty)
                {
                    continue;
                }
                
                _GenerateArea(new Vector2Int(x, y));
            } 
        }

        int areaIndex = 0;

        //var sampleArea = _areas[10];
        foreach (var sampleArea in _areas)
        {
            _groundPoints.AddRange(sampleArea.edgeWorldPoints);

            areaIndex++;
            _ShowArea(sampleArea,areaIndex);

            yield return null;
        }
    }

    void _ShowArea(Area area, int nameIndex = 0)
    {
        GameObject terrainParent = new GameObject($"Area : {nameIndex}");
        
        var triangles = _bowyerWatson.Calculate(area.edgeWorldPoints);
        _bowyerWatson.CalculateVoronoi(triangles);
        
        MarchingCube marchingCube = new MarchingCube();
        var marchingMap = marchingCube.ConvertMarching(area.Points);

        List<Edge> marchingEdges = new List<Edge>();

        for (int x = 0; x < marchingMap.GetLength(0); x++)
        {
            for (int y = 0; y < marchingMap.GetLength(1); y++)
            {
                int marchingIndex = marchingMap[x, y];

                if (marchingIndex is 0 or 15)
                {
                    continue;
                }
                
                Vector3 worldPosition = new Vector3(x * 10.0f + area.Origin.x * 10.0f - 10.0f, 0.0f, y * 10.0f + area.Origin.y * 10.0f - 10.0f);
                // var marchingSegment = Instantiate(_marchingCubeData.LookupTable[marchingIndex]);
                // marchingSegment.transform.position = worldPosition;

                var marchingEdgeData = _marchingCubeData.EdgePositionsList[marchingIndex];

                for (int i = 0; i < marchingEdgeData.Positions.Count; i += 2)
                {
                    marchingEdges.Add(new Edge()
                    {
                        P1 = marchingEdgeData.Positions[i] + worldPosition, 
                        P2 = marchingEdgeData.Positions[i + 1]+ worldPosition
                    });
                }
                
            }
        }

        var insideVoronoiPoints = new List<Vector3>();
        var voronoiEdges = GetVoronoiEdges(triangles);

        foreach (var voronoiEdge in voronoiEdges)
        {
            if (_IsInsidePoint(voronoiEdge.P1 + new Vector3(0.01f, 0.0f, 0.01f), marchingEdges))
            {
                insideVoronoiPoints.Add(voronoiEdge.P1);
            }
            
            if (_IsInsidePoint(voronoiEdge.P2 + new Vector3(0.01f, 0.0f, 0.01f), marchingEdges))
            {
                insideVoronoiPoints.Add(voronoiEdge.P2);
            }
        }
        //DrawVoronoi(triangles);

        List<Edge> finalRoadEdges = new List<Edge>();
        
        foreach (var voronoiEdge in voronoiEdges)
        {
            bool isCrossOther = false;
        
            if (!insideVoronoiPoints.Contains(voronoiEdge.P1) && !insideVoronoiPoints.Contains(voronoiEdge.P2))
            {
                continue;
            }
            
            foreach (var marchingEdge in marchingEdges)
            {
                if (MathUtil.IsCrossLine(voronoiEdge.P1, voronoiEdge.P2, marchingEdge.P1, marchingEdge.P2))
                {
                    isCrossOther = true;
                    var crossPoint = MathUtil.GetCrossPoint(voronoiEdge.P1, voronoiEdge.P2, marchingEdge.P1,
                        marchingEdge.P2);
        
                    Vector3 newPosition = insideVoronoiPoints.Contains(voronoiEdge.P1)
                        ? voronoiEdge.P1
                        : voronoiEdge.P2; 
                    
                    finalRoadEdges.Add(new Edge(){P1 = newPosition, P2 = crossPoint});
                    break;
                }
            }
        
            if (!isCrossOther)
            {
                finalRoadEdges.Add(voronoiEdge);
            }
        }

        HashSet<Vector3> buildingPoints = new HashSet<Vector3>();
        foreach (var triangle in triangles)
        {
            foreach (var trianglePoint in triangle.Points)
            {
                if (_IsInsidePoint(trianglePoint + new Vector3(0.001f, 0.0f, 0.001f), marchingEdges))
                {
                    buildingPoints.Add(trianglePoint);
                    
                }
            }
        }

        foreach (var buildingPoint in buildingPoints)
        {
            // if (nameIndex % 3 != 0)
            // {
            var testObj = Instantiate(_wfcOutputSampleObject);
            testObj.transform.position = buildingPoint;
            testObj.transform.localScale = Vector3.one * 1.0f;
            // }
            //
            // else
            {
                // ProceduralBuildingGenerator.Mass mass = new ProceduralBuildingGenerator.Mass();
                //
                // mass.FacadeRule = _buildingGenerator._rootRule;
                // mass.CornerRule = _buildingGenerator._cornerRule;
                //
                // mass.CreateWithPrimitiveData(new ProceduralBuildingGenerator.Mass.PrimitiveData
                // {
                //     CornerCount = 8,
                //     Position = buildingPoint,
                //     Radius = 20.0f,
                //     Size = new Vector3(5.0f, 20.0f, 5.0f),
                //     Type = ProceduralBuildingGenerator.Mass.PrimitiveType.eRectangle
                // });
            
                // foreach (var facade in mass._childContexts)
                // {
                //     facade.CreatePrimitive(new GameObject());
                // }
            }
            
        }
        
        DrawEdges(marchingEdges);
        DrawEdges(finalRoadEdges);
    }

    void _ShowWholeArea(Area area)
    {
        GameObject terrainParent = new GameObject("Area");

        foreach (var point in area.WorldPoints)
        {
            var outputSample = Instantiate(_wfcOutputSampleObject, terrainParent.transform, true);
            outputSample.transform.position = point;
        }
    }

    bool _IsInsidePoint(Vector3 point, List<Edge> polygon)
    {
        int crossCount = 0;
        
        foreach (var edge in polygon)
        {
            if (MathUtil.IsCrossLine(edge.P1, edge.P2, point, new Vector3(point.x + 1000.0f, 0.0f, point.z)))
            {
                crossCount++;
            }
        }
        
        return crossCount % 2 == 1;
    }

    List<Edge> GetVoronoiEdges(List<Triangle> triangles)
    {
        List<Edge> edges = new List<Edge>();
        
        foreach (var triangle in triangles)
        {
            foreach (var neighbor in triangle.ShareEdgeTriangles)
            {
                var edge = new Edge() { P1 = triangle.Circumcircle.Center, P2 = neighbor.Circumcircle.Center };

                if (edges.Contains(edge))
                {
                    continue;
                }
                
                edges.Add(edge);
            }
        }

        return edges;
    }

    void DrawEdges(List<Edge> edges)
    {
        foreach (var edge in edges)
        {
            if (!edge.P1.IsValid() || !edge.P2.IsValid())
            {
                continue;
            }
            
            GameObject triangleObject = Instantiate(_simpleLineRenderer);
            triangleObject.transform.position = Vector3.zero;
            //triangleObject.transform.parent = Voronoi.transform;
            var lineRenderer = triangleObject.GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] { edge.P1, edge.P2 });
        
            //yield return null;
        }
    }

    void DrawVoronoi(List<Triangle> triangles)
    {
        GameObject Voronoi = new GameObject("Voronoi");

        var edges = GetVoronoiEdges(triangles);

        DrawEdges(edges);
    }

    void DrawArea(List<Triangle> triangles)
    {
        GameObject Voronoi = new GameObject("Voronoi");
        
        List<Edge> edges = new List<Edge>();
        
        foreach (var triangle in triangles)
        {
            if (!_points.Contains(triangle.P1) || !_points.Contains(triangle.P2) || !_points.Contains(triangle.P3))
            {
                continue;
            }

            foreach (var edge in triangle.Edges)
            {
                if (edges.Find((e) => e.Equals(edge)) != null)
                {
                    edges.Remove(edge);
                }
                else
                {
                    edges.Add(edge);
                }
            }
        }

        foreach (var edge in edges)
        {
            if (!edge.P1.IsValid() || !edge.P2.IsValid())
            {
                continue;
            }
            
            GameObject triangleObject = Instantiate(_simpleLineRenderer);
            triangleObject.transform.position = Vector3.zero;
            triangleObject.transform.parent = Voronoi.transform;
            var lineRenderer = triangleObject.GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] { edge.P1, edge.P2 });
        
            //yield return null;
        }
    }

    IEnumerator _PropagateStep()
    {
        GameObject terrainParent = new GameObject("terrainParent");
        
        while (!_waveFunctionCollapse.IsCollapseComplete())
        {
            _waveFunctionCollapse.PropagationStep();

            foreach (var cell in _waveFunctionCollapse.GeneratedCells)
            {
                if (cell.CollapsedPattern == null)
                {
                    Debug.Log("strange");
                    continue;
                }
                
                var terrainPosition = new Vector3(cell.Position.x, 0.0f, cell.Position.y);

                if (cell.CollapsedPattern.SuperPositionIndex == Ground)
                {
                    _points.Add(terrainPosition);
                    
                }
                else
                {
                    _groundPoints.Add(terrainPosition);
                }

                outputCells[cell.Position.x, cell.Position.y] = cell.CollapsedPattern.SuperPositionIndex;
            }

            yield return null;
        }
    }

    void _GenerateArea(Vector2Int anchorPosition)
    {
        if (_GetArea(anchorPosition) != null)
        {
            return;
        }

        Area area = new Area();

        area.Origin = new Vector2Int(int.MaxValue, int.MaxValue);
        
        _areas.Add(area);
        
        List<Vector2Int> remainPositions = new List<Vector2Int>();

        Vector2Int curPos = anchorPosition;
        remainPositions.Add(curPos);
        
        while (remainPositions.Count != 0)
        {
            curPos = remainPositions[0];
            remainPositions.Remove(curPos);   
            
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2Int p = curPos + new Vector2Int(x, y);
                    
                    if (area.ContainsPosition(p) || remainPositions.Contains(p))
                    {
                        continue;
                    }
                    
                    if (!_IsValidatePosition(p))
                    {
                        continue;
                    }

                    if (outputCells[p.x, p.y] == Empty)
                    {
                        continue;
                    }

                    remainPositions.Add(p);
                    area.Points.Add(p);
                    area.WorldPoints.Add(new Vector3(p.x * 10.0f, 0.0f, p.y * 10.0f));

                    area.Origin.x = Mathf.Min(area.Origin.x, p.x);
                    area.Origin.y = Mathf.Min(area.Origin.y, p.y);
                    area.Size.x = Mathf.Max(area.Size.x, p.x);
                    area.Size.y = Mathf.Max(area.Size.y, p.y);
                }
            }
        }
        
        _CalculateAreaEdge(area);
    }

    void _CalculateAreaEdge(Area area)
    {
        foreach (var worldPoint in area.Points)
        {
            int containsGround = 0;
            bool isEdge = false;
            bool isContainsEdge = false;

            foreach (var deltaPos in _nearPosition)
            {
                Vector2Int checkPosition = worldPoint + deltaPos;
                bool isInvalidatePos = !_IsValidatePosition(checkPosition);
                bool isEmptyCell = false;

                if(!isInvalidatePos)
                {
                    isEmptyCell = outputCells[checkPosition.x, checkPosition.y] == Empty;
                }

                if(isInvalidatePos || isEmptyCell)
                {
                    isContainsEdge = true;
                    containsGround++;

                    if (containsGround >= 2)
                    {
                        isEdge = true;
                        break;
                    }
                }
            }

            if (isEdge)
            {
                area.edgePoints.Add(worldPoint);
                area.edgeWorldPoints.Add(new Vector3(worldPoint.x * 10.0f, 0.0f, worldPoint.y * 10.0f));
            }
            else if(isContainsEdge)
            {
                area.groundPoints.Add(worldPoint);
            }
        }

        List<Vector2Int> removeEdges = new List<Vector2Int>();
        
        foreach (var worldPoint in area.edgePoints)
        {
            int containsEmpty = 0;

            if (area.edgePoints.Contains(worldPoint + Vector2Int.up)) containsEmpty++;
            if (area.edgePoints.Contains(worldPoint + Vector2Int.down)) containsEmpty++;
            if (area.edgePoints.Contains(worldPoint + Vector2Int.left)) containsEmpty++;
            if (area.edgePoints.Contains(worldPoint + Vector2Int.right)) containsEmpty++;

            if (containsEmpty == 3)
            {
                removeEdges.Add(worldPoint);
            }
        }

        foreach (var removeEdge in removeEdges)
        {
            area.edgePoints.Remove(removeEdge);
            area.edgeWorldPoints.Remove(new Vector3(removeEdge.x, 0.0f, removeEdge.y));
        }

        foreach (var worldPoint in area.groundPoints)
        {
            void checkEdge(Vector2Int startPos, Vector2Int deltaPos)
            {
                int containsGround = 0;
                Vector2Int stp = startPos;

                for (int i = 0; i < 2; i++)
                {
                    if (area.edgePoints.Contains(stp))
                    {
                        containsGround++;
                    }

                    stp += deltaPos;
                }

                if (containsGround == 2 && !area.Points.Contains(startPos + deltaPos / 2))
                {
                    area.edgeWorldPoints.Add(new Vector3(worldPoint.x, 0.0f, worldPoint.y));
                }
            }
            //��
            checkEdge(new Vector2Int(worldPoint.x - 1, worldPoint.y + 1), Vector2Int.right * 2);
            checkEdge(new Vector2Int(worldPoint.x - 1, worldPoint.y - 1), Vector2Int.right * 2);
            checkEdge(new Vector2Int(worldPoint.x - 1, worldPoint.y - 1), Vector2Int.up * 2);
            checkEdge(new Vector2Int(worldPoint.x + 1, worldPoint.y - 1), Vector2Int.up * 2);
        }
    }

    Area _GetArea(Vector2Int anchorPosition)
    {
        Area area = null;

        foreach (var a in _areas)
        {
            if (a.ContainsPosition(anchorPosition))
            {
                area = a;
            }
        }

        return area;
    }

    bool _IsValidatePosition(Vector2Int position)
    {
        return 0 <= position.x && _outputSize.x > position.x &&
               0 <= position.y && _outputSize.y > position.y;
    }

    public void ResetOutput()
    {
        Pattern.Size = SamplingSize;
        _totalSamplingSize = Vector2Int.zero;

        _patternManager = new PatternManager();


        Color[,] inputPixels = new Color[_inputTexture.width, _inputTexture.height];

        for (int x = 0; x < inputPixels.GetLength(0); x++)
        {
            for (int y = 0; y < inputPixels.GetLength(1); y++)
            {
                inputPixels[x, y] = _inputTexture.GetPixel(x, y);
            }
        }

        // _patternManager.ReadPattern(inputPixels, SamplingSize, inputData.UseFreeRotate);
        //
        // _output = new Output(_patternManager, _outputSize, _patternManager.GetAllPatterns().Count);
    }
}
