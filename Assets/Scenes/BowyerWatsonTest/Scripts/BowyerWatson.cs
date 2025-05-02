using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class BowyerWatson 
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
