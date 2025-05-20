using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ExVector
{
    static float _epsilon = 9.99999944E-11f;
    public static bool RefEquals(this Vector3 v1, Vector3 v2)
    {
        return Vector3.SqrMagnitude(v1 - v2) < Vector3.kEpsilon;
    }

    public static bool IsValid(this Vector3 v)
    {
        return v.x.IsValid() && v.y.IsValid() && v.z.IsValid();
    }

    public static bool IsValid(this float f)
    {
        return !float.IsNaN(f) && float.IsFinite(f);
    }

    public static bool RefEquals(this float f, float other)
    {
        return Mathf.Abs(f - other) <= _epsilon;
    }
}

public class Circle
{
    public Vector3 Center;
    public float Radius;

    public bool IsInsidePoint(Vector3 point)
    {
        return Vector3.Distance(Center, point) < Radius;
    }
}

public class Edge
{
    public Vector3 P1;
    public int P1Index;
    public Vector3 P2;
    public int P2Index;

    public override bool Equals(object obj)
    {
        if (obj!.GetType() != typeof(Edge))
        {
            return false;
        }

        var e = obj as Edge;
        
        return (P1.RefEquals(e.P1) && P2.RefEquals(e.P2)) || (P1.RefEquals(e.P2) && P2.RefEquals(e.P1));
    }

    public override int GetHashCode()
    {
        return P1.GetHashCode() ^ (P2.GetHashCode());
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

    public List<Room> ConnectedRoom = new List<Room>();
    public List<Vector3> Points = new List<Vector3>();
    public List<Edge> Edges = new List<Edge>();
    public List<Triangle> ShareEdgeTriangles = new List<Triangle>();

    Circle _circumcircle;

    public Triangle()
    {
        
    }

    public bool IsNeighborTriangle(Triangle another)
    {
        bool containsEqualEdge = false;

        foreach (var edge in Edges)
        {
            if(another.Edges.Find(anotherEdge => anotherEdge.Equals(edge)) != null)
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

        //var e2 = new Edge() { P1 = P2, P2 = P3 };
        var e2 = new Edge() 
        {
            P1 = P2,
            P1Index = P2Index,
            P2 = P3,
            P2Index = P3Index
        };
        
        Edges.Add(e2);

        //var e3 = new Edge() { P1 = P3, P2 = P1 };
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

    public Circle Circumcircle { get => _circumcircle; }
}

public class PolygonSplit
{
    public class Vertex
    {
        public int Index;
        public Vector3 Position;
        public List<Vertex> LinkedVertex = new List<Vertex>();

        public override bool Equals(object obj)
        {
            return obj is Vertex anotherVertex && Position.RefEquals(anotherVertex.Position);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }

    public class Polygon
    {
        public HashSet<int> Indices = new HashSet<int>();
        public List<Vector3> Points = new List<Vector3>();

        public void AddVertex(int index)
        {
            Indices.Add(index);
        }

        public override bool Equals(object obj)
        {
            return obj is Polygon anotherPolygon && !Indices.Except(anotherPolygon.Indices).Any();
        }

        public override int GetHashCode()
        {
            return Indices.GetHashCode();
        }
    }
    
    public static List<Polygon> GetSplitPolygons(List<Streamline.Vertex> connectInfo)
    {
        Dictionary<int, Streamline.Vertex> vertices = new Dictionary<int, Streamline.Vertex>();
        List<Polygon> allPolygons = new List<Polygon>();

        foreach (var vertex in connectInfo)
        {
            vertices.Add(vertex.Index, vertex);
        }

        foreach (var vertex in connectInfo)
        {
            foreach (var connectedVertex in vertex.LinkedVertices)
            {
                if (!connectedVertex.LinkedVertices.Contains(vertex))
                {
                    connectedVertex.LinkedVertices.Add(vertex);
                }
            }
        }
        
        foreach (var vertex in connectInfo)
        {
            if (vertex.LinkedVertices.Count <= 2)
            {
                continue;
            }
            
            foreach (var linkVertex in vertex.LinkedVertices)
            {
                bool isCorrectEnd = true;
                
                Polygon polygon = new Polygon();
                polygon.AddVertex(vertex.Index);
                        
                Streamline.Vertex nextVertex = linkVertex;
                var prevVertex = vertex;
            
                while(!nextVertex.Index.Equals(vertex.Index))
                {
                    float minAngle = float.MaxValue;
                    Streamline.Vertex minAngleVertex = null;
                    Vector3 currentDirection = (prevVertex.Position - nextVertex.Position).normalized;

                    if (nextVertex.LinkedVertices.Count > 2)
                    {
                        foreach (var nextLinkedVertex in nextVertex.LinkedVertices)
                        {
                            if (nextLinkedVertex.Equals(prevVertex))
                            {
                                continue;
                            }
            
                            Vector3 nextDirection = (nextVertex.Position - nextLinkedVertex.Position).normalized;

                            float crossAngle = Vector3.Cross(currentDirection, nextDirection).y;

                            if (Mathf.Abs(crossAngle) >= 0.0001f)
                            {
                                if (crossAngle >=
                                    0.0f)
                                {
                                    continue;
                                }
                            }
                            
            
                            float angle = Vector3.Dot(currentDirection, nextDirection);
            
                            if (angle <= minAngle)
                            {
                                minAngle = angle;
                                minAngleVertex = nextLinkedVertex;
                            }
                        }
                    }
                    else if(nextVertex.LinkedVertices.Count == 2)
                    {
                        minAngleVertex = nextVertex.LinkedVertices[0].Equals(prevVertex) ? nextVertex.LinkedVertices[1] : nextVertex.LinkedVertices[0];
                    }
            
                    if (minAngleVertex != null)
                    {
                        polygon.AddVertex(nextVertex.Index);
                        prevVertex = nextVertex;
                        nextVertex = minAngleVertex;
                    }
                    else
                    {
                        isCorrectEnd = false;
                        break;
                    }
                }
            
                if (polygon.Indices.Count > 2 && isCorrectEnd)
                {
                    bool isAlreadyContains = allPolygons.Any(existPolygon => !existPolygon.Indices.Except(polygon.Indices).Any() && existPolygon.Indices.Count == polygon.Indices.Count);

                    if (!isAlreadyContains)
                    {
                        allPolygons.Add(polygon);
                    }
                }
            }
        }

        return allPolygons;
    }
    
    public static List<Polygon> GetSplitPolygons(List<(Vector3, List<int>)> connectInfo)
    {
        List<Vertex> vertices = new List<Vertex>();
        List<Polygon> allPolygons = new List<Polygon>();

        for(int i = 0; i < connectInfo.Count; i++)
        {
            var newVertex = new Vertex
            {
                Position = connectInfo[i].Item1,
                Index = i
            };
            vertices.Add(newVertex);
        }

        for (int i = 0; i < connectInfo.Count; i++)
        {
            var vertex = vertices[i];

            foreach (var connectIndex in connectInfo[i].Item2)
            {
                var connectedVertex = vertices[connectIndex];

                if (!vertex.LinkedVertex.Contains(connectedVertex))
                {
                    vertex.LinkedVertex.Add(connectedVertex);
                }
                
                if (!connectedVertex.LinkedVertex.Contains(vertex))
                {
                    connectedVertex.LinkedVertex.Add(vertex);
                }
            }
        }
        
        foreach (var vertex in vertices)
        {
            foreach (var linkVertex in vertex.LinkedVertex)
            {
                bool isCorrectEnd = true;
                
                Polygon polygon = new Polygon();
                polygon.AddVertex(vertex.Index);
                        
                Vertex nextVertex = linkVertex;
                var prevVertex = vertex;
            
                while(!nextVertex.Index.Equals(vertex.Index))
                {
                    float minAngle = float.MaxValue;
                    Vertex minAngleVertex = null;
                    Vector3 currentDirection = (prevVertex.Position - nextVertex.Position).normalized;

                    if (nextVertex.LinkedVertex.Count > 2)
                    {
                        foreach (var nextLinkedVertex in nextVertex.LinkedVertex)
                        {
                            if (nextLinkedVertex.Equals(prevVertex))
                            {
                                continue;
                            }

                            Vector3 nextDirection = (nextVertex.Position - nextLinkedVertex.Position).normalized;

                            if (Vector3.Cross(currentDirection, nextDirection).y >=
                                0.0f)
                            {
                                continue;
                            }

                            float angle = Vector3.Dot(currentDirection, nextDirection);

                            if (angle <= minAngle)
                            {
                                minAngle = angle;
                                minAngleVertex = nextLinkedVertex;
                            }
                        }
                    }

                    else if(nextVertex.LinkedVertex.Count == 2)
                    {
                        minAngleVertex = nextVertex.LinkedVertex[0].Equals(prevVertex) ? nextVertex.LinkedVertex[1] : nextVertex.LinkedVertex[0];
                    }
                    
            
                    if (minAngleVertex != null)
                    {
                        polygon.AddVertex(nextVertex.Index);
                        prevVertex = nextVertex;
                        nextVertex = minAngleVertex;
                    }
                    else
                    {
                        isCorrectEnd = false;
                        break;
                    }
                }
            
                if (polygon.Indices.Count > 2 && isCorrectEnd)
                {
                    bool isAlreadyContains = allPolygons.Any(existPolygon => !existPolygon.Indices.Except(polygon.Indices).Any() && existPolygon.Indices.Count == polygon.Indices.Count);

                    if (!isAlreadyContains)
                    {
                        foreach (var index in polygon.Indices)
                        {
                            polygon.Points.Add(vertices[index].Position);
                        }
                        allPolygons.Add(polygon);
                    }
                }
            }
        }

        return allPolygons;
    }

    public static List<Vector3> GetShrinkPolygon(List<Vector3> polygonPoints, float distance)
    {
        var results = new List<Vector3>();
        var shrinkPoints = new List<Vector3>();
        var bisectorDirections = new List<Vector3>();
        var distanceParents = new List<float>();

        for (int i = 0; i < polygonPoints.Count; i++)
        {
            var prevPosition = polygonPoints[i == 0 ? ^1 : i - 1];
            var currentPosition = polygonPoints[i];
            var nextPosition = polygonPoints[i == polygonPoints.Count - 1 ? 0 : i + 1];

            var prevDirection = Vector3.Normalize(prevPosition - currentPosition);
            var nextDirection = Vector3.Normalize(nextPosition - currentPosition);

            var bisectorDirection = MathUtil.GetBisectorVector3(prevDirection, nextDirection);

            var distParent = Mathf.Sin(MathUtil.GetAngleAxis(prevDirection, nextDirection) * 0.5f * Mathf.Deg2Rad);
            float dist = 0.0f;
            
            if (distParent <= 0.1f)
            {
                dist = distance;
            }

            else
            {
                dist = distance / distParent;    
            }
            
            bisectorDirections.Add(bisectorDirection);
            distanceParents.Add(distParent);
            shrinkPoints.Add(currentPosition + bisectorDirection * dist);
        }
        
        int minShrinkPointIndex = -1;
        Vector3 minShrinkPoint = Vector3.zero;
        float minShrinkDistance = float.MaxValue;

        for (int i = 0; i < shrinkPoints.Count; i++)
        {
            Vector3 prevPolygonPoint = polygonPoints[i];
            Vector3 prevShrinkPoint = shrinkPoints[i];
            int nextPosition = -1;
            
            for (int j = i + 1; j < shrinkPoints.Count; j++)
            {
                if (!MathUtil.IsCrossLine(prevPolygonPoint, prevShrinkPoint, polygonPoints[j],
                        shrinkPoints[j])) continue;
                nextPosition = j;
                prevShrinkPoint = MathUtil.GetCrossPoint(prevPolygonPoint, prevShrinkPoint, polygonPoints[j],
                    shrinkPoints[j]);

                Vector3 prevPosition = polygonPoints[i - 1 < 0 ? shrinkPoints.Count - 1 : i - 1]; 
                float distanceToLine = MathUtil.GetLineToPositionDistance(polygonPoints[i], prevPosition, prevShrinkPoint);

                if (distanceToLine < minShrinkDistance)
                {
                    minShrinkDistance = distanceToLine;
                    minShrinkPointIndex = i;
                    minShrinkPoint = prevShrinkPoint;
                }
            }

            if (nextPosition != -1)
            {
                i = nextPosition;
            }
        }

        for (int i = 0; i < shrinkPoints.Count; i++)
        {
            if (i == minShrinkPointIndex)
            {
                results.Add(minShrinkPoint);
                i++;
            }
            else
            {
                results.Add(polygonPoints[i] + bisectorDirections[i] * (minShrinkDistance / distanceParents[i]));
            }
        }

        return results;
    }
}

public class MathUtil
{
    public static int ccw(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float cross_productv1 = (v2.x - v1.x) * (v3.z - v1.z);
        float cross_productv2 = (v3.x - v1.x) * (v2.z - v1.z);

        if (Mathf.Abs(cross_productv1 - cross_productv2) < float.Epsilon)
        {
            return 0;
        }

        if (cross_productv1 > cross_productv2)
        {
            return 1;
        }

        return -1;
    }
    
    // 출처 : https://gaussian37.github.io/math-algorithm-line_intersection/
    public static bool IsCrossLine(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        bool comparator(Vector3 left, Vector3 right)
        {
            if(Math.Abs(left.x - right.x) < 0.001f)
            {
                return (left.y <= right.y);
            }

            return (left.x <= right.x);
        }

        void swap(Vector3 left, Vector3 right)
        {
            (left, right) = (right, left);
        }
        
        int l1_l2 = ccw(v1, v2, v3) * ccw(v1, v2, v4);
        int l2_l1 = ccw(v3, v4, v1) * ccw(v3, v4, v2);

        if (l1_l2 == 0 && l2_l1 == 0)
        {
            if (comparator(v2, v1))
            {
                swap(v1, v2);
            }
            if (comparator(v4, v3))
            {
                swap(v3, v4);
            }

            return comparator(v3, v1) && comparator(v1, v4);
        }
        
        return l1_l2 <= 0 && l2_l1 <= 0;
    }
    public static Vector3 GetCrossPoint(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float under = (p4.z - p3.z) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.z - p1.z);
        float s = ((p2.x - p1.x) * (p1.z - p3.z) - (p2.z - p1.z) * (p1.x - p3.x)) / under;

        return new Vector3(p3.x + s * (p4.x - p3.x), 0.0f, p3.z + s * (p4.z - p3.z));
    }
    
    public static Vector3 GetInscribedCircleCenter(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // 삼각형의 세 변의 길이 계산
        float a = Vector3.Distance(p2, p3);
        float b = Vector3.Distance(p1, p3);
        float c = Vector3.Distance(p1, p2);

        // 삼각형의 둘레의 절반 계산
        float s = (a + b + c) / 2f;

        // 내심의 좌표 계산
        float x = (a * p1.x + b * p2.x + c * p3.x) / (a + b + c);
        float y = (a * p1.y + b * p2.y + c * p3.y) / (a + b + c);
        float z = (a * p1.z + b * p2.z + c * p3.z) / (a + b + c);

        return new Vector3(x, y, z);
    }
    
    public static bool IsPointInPolygon(Vector3 point, List<Vector3> polygon) {
        int i, j = polygon.Count - 1;
        bool inside = false;
        for (i = 0; i < polygon.Count; j = i++) {
            if ((polygon[i].z > point.z) != (polygon[j].z > point.z) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.z - polygon[i].z) /
                (polygon[j].z - polygon[i].z) + polygon[i].x) {
                inside = !inside;
            }
        }
        return inside;
    }

    
    public static bool GetIntersectionRayToRay(Vector3 v1Position, Vector3 v1Direction, Vector3 v2Position, Vector3 v2Direction, out Vector3 intersection)
    {
        intersection = Vector3.zero;

        // 두 벡터의 차이
        Vector3 v1ToV2 = v2Position - v1Position;

        // 두 방향 벡터의 외적
        float denom = Vector3.Cross(v1Direction, v2Direction).magnitude;

        // 두 벡터의 외적이 0이면 평행하거나 일치하는 경우 (교차점 없음)
        if (denom == 0f)
        {
            return false; // 교차점이 없음
        }

        // 교차점 계산
        float t = Vector3.Cross(v1ToV2, v2Direction).magnitude / denom;
        float s = Vector3.Cross(v1ToV2, v1Direction).magnitude / denom;

        // t, s가 각각 0 이상이면 교차점이 존재
        if (t >= 0f && s >= 0f)
        {
            intersection = v1Position + t * v1Direction;
            return true; // 교차점이 존재
        }

        return false; // 교차점이 없음
    }
    
    // 반직선 v와 직선 l의 교차 여부를 판단하는 함수
    public static bool GetIntersectionRayToLine(Vector3 vPosition, Vector3 vDirection, Vector3 lPosition1, Vector3 lPosition2, out Vector3 intersection)
    {
        intersection = Vector3.zero;
        // 직선의 방향 벡터 계산
        Vector3 lineDirection = lPosition2 - lPosition1;
        
        // 반직선과 직선이 평행한 경우 처리
        if (Vector3.Cross(vDirection, lineDirection).sqrMagnitude < Mathf.Epsilon)
        {
            return false; // 평행하므로 교차하지 않음
        }

        // 교차점 계산을 위한 매개변수
        Vector3 lineToRay = vPosition - lPosition1;
        float denominator = Vector3.Dot(Vector3.Cross(vDirection, lineDirection), Vector3.Cross(lineDirection, lineToRay));
        float t = Vector3.Dot(Vector3.Cross(lineDirection, lineToRay), Vector3.Cross(vDirection, lineDirection)) / denominator;

        // 반직선 방향으로의 교차 여부 확인 (t >= 0)
        if (t >= 0)
        {
            // 직선 선분 내에서의 교차 여부 확인
            intersection = vPosition + t * vDirection;
            Vector3 lineToIntersection = intersection - lPosition1;
            float dot = Vector3.Dot(lineToIntersection, lineDirection);
            
            return dot >= 0 && dot <= lineDirection.sqrMagnitude;
        }

        return false;
    }

    public class CircleLineIntersectResult
    {
        public bool IsIntersect = false;
        public Vector3 LinePoint;
        public Edge DistanceEdge;
    }

    public static CircleLineIntersectResult GetCircleLineIntersect(Edge line, Vector3 circleCenter, float circleRadius)
    {
        return GetCircleLineIntersect(line.P1, line.P2, circleCenter, circleRadius);
    }
    
    public static CircleLineIntersectResult GetCircleLineIntersect(Vector3 v1, Vector3 v2, Vector3 circleCenter, float circleRadius)
    {
        CircleLineIntersectResult result = new CircleLineIntersectResult();
        
        Vector3 dirVector = v1 - v2;
        Vector3 reverseVector = new Vector3(dirVector.z, 0.0f, -dirVector.x);

        Vector3 crossLineP2 = circleCenter + reverseVector;
        Vector3 crossPoint = GetCrossPoint(v1, v2, circleCenter, crossLineP2);

        result.DistanceEdge = new Edge()
        {
            P1 = circleCenter,
            P2 = crossPoint
        };

        Vector3 newEndPoint = ((crossPoint - circleCenter) * circleRadius) + circleCenter;
        float circleRadiusPow = circleRadius * circleRadius;

        result.IsIntersect = (result.DistanceEdge.P1 - result.DistanceEdge.P2).sqrMagnitude <= circleRadiusPow
                             && IsCrossLine(result.DistanceEdge.P1, newEndPoint, v1, v2);
        result.LinePoint = crossPoint;

        if (!result.IsIntersect)
        {
            bool isV1InsideOfCircle = (v1 - circleCenter).sqrMagnitude <= circleRadiusPow; 
            bool isV2InsideOfCircle = (v2 - circleCenter).sqrMagnitude <= circleRadiusPow;

            result.IsIntersect = isV1InsideOfCircle || isV2InsideOfCircle;
            result.LinePoint = isV1InsideOfCircle ? v1 : v2;
        }

        return result;
    }

    public static float GetLineToPositionDistance(Vector3 v1, Vector3 v2, Vector3 position)
    {
        Vector3 dirVector = v1 - v2;
        Vector3 reverseVector = new Vector3(dirVector.z, 0.0f, -dirVector.x);

        Vector3 crossLineP2 = position + reverseVector;
        Vector3 crossPoint = GetCrossPoint(v1, v2, position, crossLineP2);

        return Vector3.Distance(position, crossPoint);
    }

    public static Vector3 GetLineToPositionPoint(Vector3 v1, Vector3 v2, Vector3 position)
    {
        Vector3 dirVector = v1 - v2;
        Vector3 reverseVector = new Vector3(dirVector.z, 0.0f, -dirVector.x);

        Vector3 crossLineP2 = position + reverseVector;
        return GetCrossPoint(v1, v2, position, crossLineP2);
    }
    
    public static Vector3 GetBisectorVector3(Vector3 center, Vector3 previous, Vector3 next)
    {
        Vector3 prevDir = (previous - center).normalized;
        return Quaternion.AngleAxis(GetAngleAxis(center, previous, next) * 0.5f, Vector3.up) * prevDir;
    }
    
    public static Vector3 GetBisectorVector3(Vector3 prevDir, Vector3 nextDir)
    {
        return Quaternion.AngleAxis(GetAngleAxis(prevDir, nextDir) * 0.5f, Vector3.up) * prevDir;
    }

    public static float GetAngleAxis(Vector3 center, Vector3 previous, Vector3 next)
    {
        Vector3 prevDir = (previous - center).normalized;
        Vector3 nextDir = (next - center).normalized;
        
        return GetAngleAxis(prevDir, nextDir);
    }
    
    public static float GetAngleAxis(Vector3 prevDir, Vector3 nextDir)
    {
        float signedAngle = Vector3.SignedAngle(prevDir, nextDir, Vector3.up);

        if (signedAngle < 0.0f)
        {
            signedAngle = 360.0f - signedAngle * -1.0f;
        }
        
        return signedAngle;
    }
    
    public static float GetDirRotation(Vector3 startPos, Vector3 endPos)
    {
        Vector3 dir = endPos - startPos;
        return -Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
    }
}
