using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility.Triangulation
{
    public class Circle
    {
        public Vector3 Center;
        public float Radius;

        public bool IsInsidePoint(Vector3 point)
        {
            return Vector3.Distance(Center, point) < Radius;
        }
    }
    
    public class Triangle
    {
        public Vector3 P1;
        public int P1Index;
        public Vector3 P2;
        public int P2Index;
        public Vector3 P3;
        public int P3Index;

        public List<Vector3> Points = new List<Vector3>();
        public List<Edge> Edges = new List<Edge>();
        public List<Triangle> ShareEdgeTriangles = new List<Triangle>();

        private Circle _circumcircle;

        public Triangle()
        {

        }

        public bool IsNeighborTriangle(Triangle another)
        {
            bool containsEqualEdge = false;

            foreach (var edge in Edges)
            {
                if (another.Edges.Find(anotherEdge => anotherEdge.Equals(edge)) != null)
                {
                    containsEqualEdge = true;
                    break;
                }
            }

            return containsEqualEdge;
        }

        public void Initialize()
        {
            _CalculateCircumcircle();

            var e1 = new Edge()
            {
                P1 = P1,
                P1Index = P1Index,
                P2 = P2,
                P2Index = P2Index
            };

            Edges.Add(e1);

            var e2 = new Edge()
            {
                P1 = P2,
                P1Index = P2Index,
                P2 = P3,
                P2Index = P3Index
            };

            Edges.Add(e2);

            var e3 = new Edge()
            {
                P1 = P3,
                P1Index = P3Index,
                P2 = P1,
                P2Index = P1Index
            };

            Edges.Add(e3);
            
            Points.Add(P1);
            Points.Add(P2);
            Points.Add(P3);
        }

        private void _CalculateCircumcircle()
        {
            float inverseD = 1 / ((P1.x * (P2.z - P3.z) + P2.x * (P3.z - P1.z) + P3.x * (P1.z - P2.z)) * 2);

            float x = inverseD * ((P1.x * P1.x + P1.z * P1.z) * (P2.z - P3.z) +
                                  (P2.x * P2.x + P2.z * P2.z) * (P3.z - P1.z) +
                                  (P3.x * P3.x + P3.z * P3.z) * (P1.z - P2.z));

            float z = inverseD * ((P1.x * P1.x + P1.z * P1.z) * (P3.x - P2.x) +
                                  (P2.x * P2.x + P2.z * P2.z) * (P1.x - P3.x) +
                                  (P3.x * P3.x + P3.z * P3.z) * (P2.x - P1.x));

            float radius = Mathf.Sqrt(Mathf.Pow(P1.x - x, 2) + Mathf.Pow(P1.z - z, 2));

            _circumcircle = new Circle() { Center = new Vector3(x, 0.0f, z), Radius = radius };
        }

        public Circle Circumcircle
        {
            get => _circumcircle;
        }
    }

    public class Triangulation 
    {
        public List<Triangle> Calculate(List<Vector3> points)
        {
            List<Triangle> triangles = new List<Triangle>();
            List<Triangle> badTriangles = new List<Triangle>();
            List<Edge> edges = new List<Edge>();

            Triangle superTriangle = new Triangle();

            superTriangle.P1 = new Vector3(-100000.0f, 0.0f, 0.0f);
            superTriangle.P2 = new Vector3(0.0f, 0.0f, 100000.0f);
            superTriangle.P3 = new Vector3(100000.0f, 0.0f, 0.0f);

            superTriangle.Initialize();

            triangles.Add(superTriangle);

        
            for(int i = 0; i < points.Count(); i++)
            {
                var point = points[i];
                badTriangles.Clear();
                edges.Clear();

                foreach (var triangle in triangles)
                {
                    if (triangle.Circumcircle.IsInsidePoint(point))
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
                    Triangle newTriangle = new Triangle()
                    {
                        P1 = edge.P1,
                        P1Index = edge.P1Index,
                        P2 = edge.P2, 
                        P2Index = edge.P2Index,
                        P3 = point,
                        P3Index = i
                    };
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

        public void CalculateVoronoi(List<Triangle> triangles)
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                var triangle = triangles[i];

                for (int j = 0; j < triangles.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var neighbor = triangles[j];

                    if (triangle.IsNeighborTriangle(neighbor))
                    {
                        triangle.ShareEdgeTriangles.Add(neighbor);
                    }
                }
            }
        
        
        }

        List<Edge> _getUniqueEdges(List<Edge> edges)
        {
            List<Edge> uniqueEdges = new List<Edge>();

            for (int i = 0; i < edges.Count; i++)
            {
                bool isUnique = true;

                for (int j = 0; j < edges.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    if (edges[i].Equals(edges[j]))
                    {
                        isUnique = false;
                        break;
                    }
                }

                if (isUnique)
                {
                    uniqueEdges.Add(edges[i]);
                }
            }

            return uniqueEdges;
        }
    }
}
