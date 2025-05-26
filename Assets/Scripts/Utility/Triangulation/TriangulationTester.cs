using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utility.Triangulation
{
    public class TriangulationTester : MonoBehaviour
    {
        [FormerlySerializedAs("PointParent")] [SerializeField] Transform pointParent;
        [FormerlySerializedAs("PointPrefab")] [SerializeField] GameObject pointPrefab;
        [FormerlySerializedAs("Count")] [SerializeField] int count;

        [FormerlySerializedAs("_TrianglePrefab")] [SerializeField] GameObject trianglePrefab;

        [SerializeField] bool _drawDelawrey;

        private Triangulation _triangulation = new global::Utility.Triangulation.Triangulation();

        // Start is called before the first frame update
        void Start()
        {
            float drawXMin = -50.0f;
            float drawXMax = 50.0f;
            float drawZMin = 0.0f;
            float drawZMax = 50.0f;
            List<Vector3> points = new List<Vector3>();

            points.Add(new Vector3(drawXMin, 0.0f, drawZMin));
            points.Add(new Vector3(drawXMax, 0.0f, drawZMin));
            points.Add(new Vector3(drawXMin, 0.0f, drawZMax));
            points.Add(new Vector3(drawXMax, 0.0f, drawZMax));

            for(int i = 0; i < count - 4; i++)
            {
                Vector3 randPos = new Vector3(Random.Range(drawXMin, drawXMax), 0.0f, Random.Range(drawZMin, drawZMax));
            
                points.Add(randPos);
            }

            foreach (var point in points)
            {
                var pointObject = Instantiate(pointPrefab);
                pointObject.transform.position = point;
            }

            var triangles = _triangulation.Calculate(points);


            if(_drawDelawrey)
            {
                GameObject delawrey = new GameObject("��γ�");

                foreach (var triangle in triangles)
                {
                    GameObject triangleObject = Instantiate(trianglePrefab);
                    triangleObject.transform.position = Vector3.zero;
                    triangleObject.transform.parent = delawrey.transform;
                    var lineRenderer = triangleObject.GetComponent<LineRenderer>();
                    lineRenderer.positionCount = 3;
                    lineRenderer.SetPositions(new Vector3[] { triangle.P1, triangle.P2, triangle.P3 });
                }
            }

        

            GameObject Voronoi = new GameObject("��������");

            foreach (var triangle in triangles)
            {
                foreach (var neighbor in triangle.ShareEdgeTriangles)
                {
                    GameObject triangleObject = Instantiate(trianglePrefab);
                    triangleObject.transform.position = Vector3.zero;
                    triangleObject.transform.parent = Voronoi.transform;
                    var lineRenderer = triangleObject.GetComponent<LineRenderer>();
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPositions(new Vector3[] { triangle.Circumcircle.Center, neighbor.Circumcircle.Center });
                }
            }
        }

        public List<Triangle> Calculate(List<Vector3> points)
        {
            List<Triangle> triangles = new List<Triangle>();
            List<Triangle> badTriangles = new List<Triangle>();
            List<Edge> edges = new List<Edge>();

            Triangle superTriangle = new Triangle();

            superTriangle.P1 = new Vector3(-1000.0f, 0.0f, 0.0f);
            superTriangle.P2 = new Vector3(0.0f, 0.0f, 1000.0f);
            superTriangle.P3 = new Vector3(1000.0f, 0.0f, 0.0f);

            superTriangle.Initialize();

            triangles.Add(superTriangle);

            foreach (var point in points)
            {
                badTriangles.Clear();
                edges.Clear();

                foreach (var triangle in triangles)
                {
                    if(triangle.Circumcircle.IsInsidePoint(point))
                    {
                        badTriangles.Add(triangle);
                        edges.AddRange(triangle.Edges);
                    }
                }

                foreach (var badTriangle in badTriangles)
                {
                    triangles.Remove(badTriangle);
                }

                List<Edge> uniqueEdges = _getUniqueEdges(edges);

                foreach (var edge in uniqueEdges)
                {
                    Triangle newTriangle = new Triangle() { P1 = edge.P1, P2 = edge.P2, P3 = point };
                    newTriangle.Initialize();
                    triangles.Add(newTriangle);
                }

            
            
            }

            badTriangles.Clear();

            foreach (var triangle in triangles)
            {
                bool isContainsSuperPoint = false;

                foreach (var point in triangle.Points)
                {
                    foreach (var superPoint in superTriangle.Points)
                    {
                        if (point.RefEquals(superPoint))
                        {
                            isContainsSuperPoint = true;
                        }
                    }
                }

                if (isContainsSuperPoint)
                {
                    badTriangles.Add(triangle);
                }
            }

            foreach (var badTriangle in badTriangles)
            {
                triangles.Remove(badTriangle);
            }

            return triangles;
        }

        List<Edge> _getUniqueEdges(List<Edge> edges)
        {
            List<Edge> uniqueEdges = new List<Edge>();

            for(int i = 0; i < edges.Count; i++)
            {
                bool isUnique = true;

                for(int j = 0; j < edges.Count; j++)
                {
                    if(i == j)
                    {
                        continue;
                    }

                    if(edges[i].Equals(edges[j]))
                    {
                        isUnique = false;
                        break;
                    }
                }

                if(isUnique)
                {
                    uniqueEdges.Add(edges[i]);
                }
            }

            return uniqueEdges;
        }
    }
}
