using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;
using Utility.Triangulation;

namespace Scenes
{
    public class TestUtil
    {
        public static GameObject CreateWireframePolygonObject(LineRenderer lineRenderer, List<Vector3> points)
        {
            var pointInstance = new List<Vector3>(points);
            var lineRendererInstance = Object.Instantiate(lineRenderer.gameObject);
            var lineRendererComponent = lineRendererInstance.GetComponent<LineRenderer>();
            pointInstance.Add(pointInstance[0]);
            lineRendererComponent.positionCount = pointInstance.Count;
            lineRendererComponent.SetPositions(pointInstance.ToArray());
            return lineRendererInstance;
        }
        public static GameObject CreateSolidPolygonObject(Material renderMaterial, List<Vector3> polygonPoints)
        {
            var removeTriangles = new List<Triangle>();
            var mesh = new Mesh();
            var meshObject = new GameObject("Polygon");

            var meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = renderMaterial;
        
            var triangulation = new Triangulation();
            var triangulationResult = triangulation.Calculate(polygonPoints);

            var polygonUvs = new Vector2[polygonPoints.Count];

            for (int i = 0; i < polygonPoints.Count; i++)
            {
                polygonUvs[i] = new Vector2(polygonPoints[i].x, polygonPoints[i].z);
            }
            
            mesh.vertices = polygonPoints.ToArray();
            mesh.uv = polygonUvs;
            
            foreach (var triangle in triangulationResult)
            {
                var intersection = MathUtil.GetInscribedCircleCenter(triangle.P1, triangle.P2, triangle.P3);
                
                int polygonIntersectCount = 0;

                for (int i = 0; i < polygonPoints.Count; i++)
                {
                    var polygonLine1 = polygonPoints[i];
                    var polygonLine2 = polygonPoints[polygonPoints.Count - 1 == i ? 0 : i + 1];

                    if (MathUtil.IsCrossLine(intersection, intersection + Quaternion.Euler(0.0f, 20.0f, 0.0f) * Vector3.right * 1010.0f, polygonLine1, polygonLine2))
                    {
                        polygonIntersectCount++;
                    }
                }

                if (polygonIntersectCount % 2 == 1)
                {
                    continue;
                }
                
                removeTriangles.Add(triangle);
            }

            foreach (var removeTriangle in removeTriangles)
            {
                triangulationResult.Remove(removeTriangle);
            }
            
            var meshIndices = new List<int>();

            foreach (var triangle in triangulationResult)
            {
                meshIndices.Add(triangle.P1Index);
                meshIndices.Add(triangle.P2Index);
                meshIndices.Add(triangle.P3Index);
            }
            mesh.triangles = meshIndices.ToArray();

            return meshObject;
        }
        
        public static GameObject CreateBuildingObject(Material renderMaterial, List<Vector3> polygonPoints, float height)
        {
            var buildingPoints = new List<Vector3>(polygonPoints);
            var removeTriangles = new List<Triangle>();
            var mesh = new Mesh();
            var meshObject = new GameObject("Building");

            var meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = renderMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        
            var triangulation = new Triangulation();
            var triangulationResult = triangulation.Calculate(polygonPoints);
            
            for (int i = 0; i < buildingPoints.Count; i++)
            {
                buildingPoints[i] = new Vector3(buildingPoints[i].x, height, buildingPoints[i].z);
            }

            buildingPoints.AddRange(polygonPoints);

            var polygonUvs = new Vector2[buildingPoints.Count];

            for (int i = 0; i < polygonPoints.Count; i++)
            {
                polygonUvs[i] = new Vector2(polygonPoints[i].x, polygonPoints[i].z);
            }
            
            mesh.vertices = buildingPoints.ToArray();
            mesh.uv = polygonUvs;
            
            foreach (var triangle in triangulationResult)
            {
                if (MathUtil.IsPointInPolygon(MathUtil.GetInscribedCircleCenter(triangle.P1, triangle.P2, triangle.P3), polygonPoints))
                {
                    continue;
                }
                
                removeTriangles.Add(triangle);
            }

            foreach (var removeTriangle in removeTriangles)
            {
                triangulationResult.Remove(removeTriangle);
            }
            
            var meshIndices = new List<int>();

            foreach (var triangle in triangulationResult)
            {
                var orientation = (triangle.P2.x - triangle.P1.x) * (triangle.P3.z - triangle.P1.z) - (triangle.P3.x - triangle.P1.x) * (triangle.P2.z - triangle.P1.z);

                meshIndices.Add(triangle.P1Index);
                
                if (orientation >= 0.0f)
                {
                    meshIndices.Add(triangle.P3Index);
                    meshIndices.Add(triangle.P2Index);                    
                }
                else
                {
                    meshIndices.Add(triangle.P2Index);
                    meshIndices.Add(triangle.P3Index);
                }
            }

            for (int i = 0; i < polygonPoints.Count; i++)
            {
                meshIndices.Add(i);
                meshIndices.Add((i + polygonPoints.Count + 1) % polygonPoints.Count + polygonPoints.Count);
                meshIndices.Add((i + polygonPoints.Count) % polygonPoints.Count + polygonPoints.Count);
                
                meshIndices.Add(i);
                meshIndices.Add((i + 1) % polygonPoints.Count);
                meshIndices.Add((i + polygonPoints.Count + 1) % polygonPoints.Count + polygonPoints.Count);
            }
            mesh.triangles = meshIndices.ToArray();
            
            mesh.RecalculateNormals();

            return meshObject;
        }

        public static GameObject CreateRoofObject(Material renderMaterial,List<Vector3> polygonPoints, float height)
        {
            var buildingPoints = new List<Vector3>(polygonPoints);
            var removeTriangles = new List<Triangle>();
            var mesh = new Mesh();
            var meshObject = new GameObject("Building");
            var polygonUvs = new Vector2[buildingPoints.Count];

            var meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = renderMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            
            for (int i = 0; i < buildingPoints.Count; i++)
            {
                buildingPoints[i] = new Vector3(buildingPoints[i].x, height, buildingPoints[i].z);
            }
            
            for (int i = 0; i < polygonPoints.Count; i++)
            {
                polygonUvs[i] = new Vector2(polygonPoints[i].x, polygonPoints[i].z);
            }
            
            mesh.vertices = buildingPoints.ToArray();
            mesh.uv = polygonUvs;
            
            var triangulation = new Triangulation();
            var triangulationResult = triangulation.Calculate(polygonPoints);
            
            foreach (var triangle in triangulationResult)
            {
                if (MathUtil.IsPointInPolygon(MathUtil.GetInscribedCircleCenter(triangle.P1, triangle.P2, triangle.P3), polygonPoints))
                {
                    continue;
                }
                
                removeTriangles.Add(triangle);
            }

            foreach (var removeTriangle in removeTriangles)
            {
                triangulationResult.Remove(removeTriangle);
            }
            
            var meshIndices = new List<int>();

            foreach (var triangle in triangulationResult)
            {
                var orientation = (triangle.P2.x - triangle.P1.x) * (triangle.P3.z - triangle.P1.z) - (triangle.P3.x - triangle.P1.x) * (triangle.P2.z - triangle.P1.z);

                meshIndices.Add(triangle.P1Index);
                
                if (orientation >= 0.0f)
                {
                    meshIndices.Add(triangle.P3Index);
                    meshIndices.Add(triangle.P2Index);                    
                }
                else
                {
                    meshIndices.Add(triangle.P2Index);
                    meshIndices.Add(triangle.P3Index);
                }
            }
            
            mesh.triangles = meshIndices.ToArray();
            
            mesh.RecalculateNormals();

            return meshObject;
        }

        public static void CreateLineObject(GameObject lineRendererPrefab, Vector3 p1, Vector3 p2, Transform parent = null)
        {
            var lineObject = Object.Instantiate(lineRendererPrefab, parent, true);
            var lineRenderer = lineObject.GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, p1);
            lineRenderer.SetPosition(1, p2);
        }

        public static void CreateLineObject(GameObject lineRendererPrefab, List<Vector3> points, Transform parent = null)
        {
            CreateLineObject(lineRendererPrefab, points.ToArray(), parent);
        }
        
        public static void CreateLineObject(GameObject lineRendererPrefab, Vector3[] points, Transform parent = null)
        {
            var lineObject = Object.Instantiate(lineRendererPrefab, parent, true);
            var lineRenderer = lineObject.GetComponent<LineRenderer>();
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        }
        
        public static void CreateOutlines(GameObject lineRendererPrefab, List<Vector3> points, Transform parent = null)
        {
            CreateOutlines(lineRendererPrefab, points.ToArray(), parent);
        }

        public static void CreateOutlines(GameObject lineRendererPrefab, Vector3[] points, Transform parent = null)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                CreateLineObject(lineRendererPrefab, points[i], points[i + 1], parent);
            }
        
            CreateLineObject(lineRendererPrefab, points[^1], points[0], parent);
        }
    }
}
