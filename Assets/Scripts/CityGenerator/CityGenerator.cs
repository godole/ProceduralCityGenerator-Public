using System;
using System.Collections.Generic;
using System.Linq;
using CityGenerator.TensorFields;
using ProceduralBuildingGenerator;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;
using Utility.ObjectPool;

namespace CityGenerator
{
    public class CityGenerator : MonoBehaviour
    {
        [FormerlySerializedAs("_buildingGenerator")]
        [Header("Input Data")]
        [SerializeField] private BuildingRuleData _buildingRuleData;
        [SerializeField] private Vector2Int _size;
        [SerializeField] private Vector2 _center;
        [SerializeField] private float _loadDistance;
        [SerializeField] private float _minorLoadDistance;
        [SerializeField] private int _maxCalculateCount;
        [SerializeField] private List<TensorFieldData> _tensorFields;

        [Header("Visualizer Data")] 
        [SerializeField] private List<ObjectPoolData> _objectPoolData;
        [SerializeField] private LineRenderer _mainStreamline;
        [SerializeField] private Material _polygonMaterial;
        [SerializeField] private Material _buildingMaterial;
        [SerializeField] private LineRenderer _minorStreamline;
        [SerializeField] private List<Material> _buildingMaterials;

        [Header("Test Options")] [SerializeField]
        private bool _isGenerateBuilding;
        
        private TensorFieldContainer _tensorFieldContainer;
        private readonly List<Streamline.Vertex> _seedPoints = new();
        private VertexField _vertexField;
        private VertexField _seedVertexField;
        private Vector3 _sizeInternal;
        private int _maxCalculateCountInternal;
        private int _buildingIndex;
        
        private GameObject _buildingObjectParent;
    
        private void Start()
        {
            _buildingObjectParent = new GameObject("BuildingObjectParent");

            foreach (ObjectPoolData objectPoolData in _objectPoolData)
            {
                ObjectPoolContainer.Instance.InitWithPoolData(objectPoolData);    
            }
            
            ObjectPoolContainer.Instance.ResetAll();
        
            _maxCalculateCountInternal = _maxCalculateCount;
            _sizeInternal = new Vector3(_size.x, 0.0f, _size.y);

            _tensorFieldContainer = new TensorFieldContainer();
            _vertexField = new VertexField(_sizeInternal, 100);
            _seedVertexField = new VertexField(_sizeInternal, 100);

            foreach (var tensorField in _tensorFields)
            {
                _tensorFieldContainer.AddTensorField(tensorField);
                if (tensorField is LineTensorField line)
                {
                    foreach (Vector3 linePosition in line.Positions)
                    {
                        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        primitive.transform.position = linePosition * 100.0f;
                        primitive.transform.localScale = Vector3.one * 50.0f;
                    }
                }
            }
        
            _seedPoints.Add(new Streamline.Vertex(new Vector3(_center.x + _loadDistance, 0.0f, _center.y + _loadDistance), true));

            while (true)
            {
                _maxCalculateCountInternal--;
            
                if (_maxCalculateCountInternal < 0 ||
                    _seedPoints.Count == 0)
                {
                    break;
                }
        
                var nextPosition = _seedPoints[0];
                _seedPoints.RemoveAt(0);
            
                TraceStep(nextPosition, _mainStreamline, _loadDistance);
            }

            _seedPoints.Clear();
            _seedVertexField = new VertexField(_sizeInternal, 100);
        
            _maxCalculateCountInternal = _maxCalculateCount;

            for (int x = 0; x < _size.x; x++)
            {
                for (int y = 0; y < _size.y; y++)
                {
                    _seedPoints.Add(new Streamline.Vertex(new Vector3(x,0,y), true));
                }
            }
        

            while (true)
            {
                _maxCalculateCountInternal--;
            
                if (_maxCalculateCountInternal < 0 ||
                    _seedPoints.Count == 0)
                {
                    break;
                }
        
                var nextPosition = _seedPoints[0];
                _seedPoints.RemoveAt(0);
            
                TraceStep(nextPosition, _minorStreamline,_minorLoadDistance);
            }

            var connectionInfo = new List<Streamline.Vertex>();
            var vertices = new Dictionary<int, Streamline.Vertex>(); 
        
            foreach (var vertex in _vertexField.BoundingBoxes.SelectMany(boundingBox => boundingBox.Vertices))
            {
                connectionInfo.Add(vertex);
                vertices.Add(vertex.Index, vertex);
            }

            if (!_isGenerateBuilding)
                return;

            var polygons = PolygonSplit.GetSplitPolygons(connectionInfo);

            for(int i = 0; i < polygons.Count; i++)
            {
                var polygon = polygons[i];
            
                var polygonPoints = polygon.Indices.Select(polygonIndex => vertices[polygonIndex].Position * 100.0f).ToList();
                var shrinkPolygons = PolygonSplit.GetShrinkPolygon(polygonPoints, 9f);
                
                foreach (var shrinkPolygon in shrinkPolygons)
                {
                    if (shrinkPolygon.Count < 3)
                    {
                        continue;
                    }
                        
                    try
                    {
                        CreateTexturedBuildings(shrinkPolygon);    
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    
                    TestUtil.CreateSolidPolygonObject(_polygonMaterial, shrinkPolygon);
                }
            }
        }

        private void TraceStep(Streamline.Vertex startPosition, LineRenderer lineRenderer, float loadDistance)
        {
            var startStreamline = new Streamline(_tensorFieldContainer, _vertexField, _seedVertexField, loadDistance);
        
            var traceResult = startStreamline.Trace(_sizeInternal, startPosition, true);
            DrawStreamline(startStreamline.ContainsPositions,lineRenderer, startStreamline.Index);
        
            foreach (var seedPoint in traceResult.SeedPoints)
            {
                var nextStreamline = new Streamline(_tensorFieldContainer, _vertexField,  _seedVertexField, loadDistance);
            
                var traceNextResult = nextStreamline.Trace(_sizeInternal, seedPoint, false);
            
                if (nextStreamline.ContainsPositions.Count != 0)
                {
                    DrawStreamline(nextStreamline.ContainsPositions, lineRenderer, nextStreamline.Index);
                }
            
                _seedPoints.AddRange(traceNextResult.SeedPoints);
            }
        }

        private void DrawStreamline(List<Vector3> streamlineResult, LineRenderer lineRenderer, int objectIndex = 0)
        {
            if (streamlineResult.Count == 0)
            {
                return;
            }
        
            var line = Instantiate(lineRenderer).GetComponent<LineRenderer>();
            var o = line.gameObject;
            o.name = $"streamline Index : {objectIndex}";

            var createPositionList = new List<Vector3>(streamlineResult);

            for (int i = 0; i < createPositionList.Count; i++)
            {
                var pos = createPositionList[i];
                createPositionList[i] = pos * 100;
            }
        
            line.positionCount = createPositionList.Count;
            line.SetPositions(createPositionList.ToArray());
        }

        private void CreateParcelingBuildings(List<Vector3> subDivisionPoints, string groupName)
        {
            if (subDivisionPoints.Count <= 2)
            {
                return;
            }
            
            var results = CreateBuildingPoints(subDivisionPoints);
        
            for(var i = 0; i < results.Count; i++, _buildingIndex++)
            {
                var fixedPoints = new List<Vector3>();
                var currentPolygon = results[i];
        
                for (int j = 0; j < currentPolygon.Count; j++)
                {
                    Vector3 prevPoint = currentPolygon[j - 1 < 0 ? currentPolygon.Count - 1 : j - 1];
        
                    if (Vector3.Distance(currentPolygon[j], prevPoint) < 3.0f)
                    {
                        continue;
                    }
                
                    fixedPoints.Add(currentPolygon[j]);
                }
            
                var shrinkPolygons = PolygonSplit.GetShrinkPolygon(fixedPoints, 5f);

                foreach (var shrinkPolygon in shrinkPolygons)
                {
                    var buildingObject = TestUtil.CreateBuildingObject(_buildingMaterials[i % _buildingMaterials.Count], shrinkPolygon, UnityEngine.Random.Range(10.0f, 70.0f));
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        buildingObject.transform.SetParent(_buildingObjectParent.transform);
                    }
                
                    buildingObject.name = $"{_buildingIndex}";
                }
            }
        }

        private void CreateTexturedBuildings(List<Vector3> subDivisionPoints)
        {
            var results = CreateBuildingPoints(subDivisionPoints);

            for(var i = 0; i < results.Count; i++, _buildingIndex++)
            {
                var shrinkPolygons = PolygonSplit.GetShrinkPolygon(results[i], 5f);
                
                foreach (var shrinkPolygon in shrinkPolygons)
                {
                    var building = new GameObject();
                    
                    float height = UnityEngine.Random.Range(10f, 30f);
                    
                    var buildingObject = ProceduralBuildingGenerator.ProceduralBuildingGenerator.Generate(height, shrinkPolygon, _buildingRuleData);
                    
                    var roofObject = TestUtil.CreateRoofObject(_buildingMaterial, shrinkPolygon, height);
                    roofObject.transform.SetParent(buildingObject.transform);
                    
                    CombineMesh(buildingObject);
                    
                    Destroy(buildingObject);
                }
            }
        }

        private void CombineMesh(GameObject buildingObject)
        {
            Dictionary<string, MeshCombineData> meshCombineData = new();
            var meshFilters = buildingObject.GetComponentsInChildren<MeshFilter>();

            for(int i = 0; i < meshFilters.Length; i++)
            {
                var meshRenderer = meshFilters[i].gameObject.GetComponent<MeshRenderer>();

                if (!meshCombineData.ContainsKey(meshRenderer.sharedMaterial.name))
                {
                    var combineData = new MeshCombineData
                    {
                        Material = meshRenderer.sharedMaterial,
                        Objects = new List<GameObject> { meshRenderer.gameObject }
                    };
                    
                    meshCombineData.Add(meshRenderer.sharedMaterial.name, combineData);
                }
                else
                {
                    meshCombineData[meshRenderer.sharedMaterial.name].Objects.Add(meshFilters[i].gameObject);
                }
                
                meshFilters[i].gameObject.SetActive(false);
            }

            foreach (var combineData in meshCombineData)
            {
                GameObject combinedObject = new GameObject(combineData.Key);
                
                CombineInstance[] combineInstances = new CombineInstance[combineData.Value.Objects.Count];

                for (int i = 0; i < combineData.Value.Objects.Count; i++)
                {
                    combineInstances[i].mesh = combineData.Value.Objects[i].GetComponent<MeshFilter>().sharedMesh;
                    combineInstances[i].transform = combineData.Value.Objects[i].transform.localToWorldMatrix;
                }
                
                var combinedMesh = new Mesh();
                combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                combinedMesh.CombineMeshes(combineInstances);
            
                combinedObject.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
                var meshRenderer = combinedObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = combineData.Value.Material;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private List<List<Vector3>> CreateBuildingPoints(List<Vector3> subDivisionPoints)
        {
            List<List<Vector3>> results = new List<List<Vector3>>();
            List<List<Vector3>> splitPoints = new List<List<Vector3>> { subDivisionPoints };
            
            ConvertSmoothPolygon(subDivisionPoints);

            while(splitPoints.Count > 0)
            {
                var polygon = splitPoints[0];
                var convexHull = PolygonUtility.GetConvexHull(polygon);

                var minimumBoundingBox = PolygonUtility.GetMinimumBoundingRectangle(convexHull);
            
                splitPoints.RemoveAt(0);

                if (minimumBoundingBox.Width < 70.0f && minimumBoundingBox.Height < 70.0f)
                {
                    results.Add(polygon);
                    continue;
                }
        
                var splitLine = PolygonUtility.GetSplitLine(minimumBoundingBox.Points, 5f);
                var splitPolygons = PolygonUtility.SplitPolygon(polygon, splitLine[0], splitLine[1]);

                splitPoints.AddRange(splitPolygons);
            }

            return results;
        }

        private void ConvertSmoothPolygon(List<Vector3> polygonPoints)
        {
            var minAngle = Mathf.Cos(Mathf.Deg2Rad * 10f);
            List<Vector3> removePoints = new List<Vector3>();
            Vector3 prevPoint = polygonPoints[0];
            Vector3 currentPoint = polygonPoints[1];
            

            for (int i = 2; i < polygonPoints.Count; i++)
            {
                Vector3 prevToCurrentDir = Vector3.Normalize(currentPoint - prevPoint);
                Vector3 curToNextDir = Vector3.Normalize( polygonPoints[i] - currentPoint);

                if (Vector3.Dot(prevToCurrentDir, curToNextDir) < minAngle)
                {
                    prevPoint = polygonPoints[i - 2];
                    currentPoint = polygonPoints[i - 1];
                    continue;
                }
                
                removePoints.Add(currentPoint);
                currentPoint = polygonPoints[i];
            }

            foreach (var point in removePoints)
            {
                polygonPoints.Remove(point);
            }
        }
    }
}
