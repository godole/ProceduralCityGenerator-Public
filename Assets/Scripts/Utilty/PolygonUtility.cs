using System.Collections.Generic;
using UnityEngine;

public class PolygonUtility
{
    public class LinkedVector3
    {
        private static int LastIndex;

        public Vector3 Point;
        public LinkedVector3 Next;
        public int IterationIndex;
        public int Index { get; }

        public LinkedVector3()
        {
            Index = LastIndex;
            LastIndex++;
        }
    }

    public class MinimumBoundingRectangleResult
    {
        public float Width;
        public float Height;
        public Vector3[] Points;
    }
    
    public class Vector3CcwComparor : IComparer<Vector3>
    {
        private Vector3 _origin;
        public Vector3CcwComparor(Vector3 origin)
        {
            _origin = origin;
        }
        
        public int Compare(Vector3 v1, Vector3 v2)
        {
            var v1Delta = v1 - _origin;
            var v2Delta = v2 - _origin;

            if (v1Delta is { x: <= float.Epsilon, z: <= float.Epsilon })
            {
                return -1;
            }

            if (v2Delta is {x: <= float.Epsilon, z: <= float.Epsilon})
            {
                return 1;
            }

            if ((v1Delta.x < 0) != (v2Delta.x < 0))
            {
                if (v1Delta.x < 0)
                {
                    return 1;
                }
                if (v2Delta.x < 0)
                {
                    return -1;
                }
            }

            if (v1Delta.x < 0 && v2Delta.x < 0)
            {
                return Mathf.Abs(v1Delta.z * v2Delta.x) < Mathf.Abs(v1Delta.x * v2Delta.z) ? 1 : -1;    
            }
            
            return Mathf.Abs(v1Delta.z * v2Delta.x) < Mathf.Abs(v1Delta.x * v2Delta.z) ? -1 : 1;
        }
    }
    
    public static List<Vector3> GetConvexHull(List<Vector3> polygonPoints)
    {
        var polygonPointCopy = new List<Vector3>(polygonPoints);
        var convexHull = new List<Vector3>();

        if (polygonPoints.Count <= 3)
        {
            return polygonPoints;
        }

        Vector3 minSortPosition = new Vector3(float.MaxValue, 0.0f, float.MaxValue);

        foreach (var point in polygonPoints)
        {
            if (point.z.RefEquals(minSortPosition.z))
            {
                if (point.x < minSortPosition.x)
                {
                    minSortPosition = point;
                }
            }
            
            else if (point.z < minSortPosition.z)
            {
                minSortPosition = point;
            }
        }
        
        polygonPointCopy.Sort(new Vector3CcwComparor(minSortPosition));
        
        convexHull.Add(polygonPointCopy[0]);
        convexHull.Add(polygonPointCopy[1]);

        for (int i = 2; i < polygonPointCopy.Count; i++)
        {
            Vector3 prevPosition = convexHull[^2];
            Vector3 currentPosition = convexHull[^1];
            Vector3 nextPosition = polygonPointCopy[i % polygonPointCopy.Count];

            if (MathUtil.ccw(prevPosition, currentPosition, nextPosition) >= 0) 
            {
                convexHull.Add(nextPosition);
            }
            else
            {
                convexHull.RemoveAt(convexHull.Count - 1);
                i--;
            }
        }
        
        return convexHull;
    }

    // left top
    // left bottom
    // right bottom
    // right up
    
    public static MinimumBoundingRectangleResult GetMinimumBoundingRectangle(List<Vector3> convexHull)
    {
        MinimumBoundingRectangleResult result = new MinimumBoundingRectangleResult()
        {
            Points = new Vector3[4]
        };

        float minX = float.MaxValue;
        float minZ = float.MaxValue;
        float maxX = float.MinValue;
        float maxZ = float.MinValue;
        var minArea = float.MaxValue;
        float minAreaAngle = 0.0f;
        int minAreaPositionIndex = -1;

        for (int i = 0; i < convexHull.Count; i++)
        {
            var currentPosition = convexHull[i];
            var currentAngle = MathUtil.GetAngleAxis(Vector3.forward, (convexHull[(i+1) % convexHull.Count] - convexHull[i]).normalized);
            float currentMinX = float.MaxValue;
            float currentMinZ = float.MaxValue;
            float currentMaxX = float.MinValue;
            float currentMaxZ = float.MinValue;
            
            for (int j = 0; j < convexHull.Count; j++)
            {
                var calculateDeltaPosition = Quaternion.Euler(0.0f, -currentAngle, 0.0f) * (convexHull[j] - currentPosition);
                
                if (calculateDeltaPosition.x < currentMinX)
                {
                    currentMinX = calculateDeltaPosition.x;
                }

                if (calculateDeltaPosition.x > currentMaxX)
                {
                    currentMaxX = calculateDeltaPosition.x;
                }

                if (calculateDeltaPosition.z < currentMinZ)
                {
                    currentMinZ = calculateDeltaPosition.z;
                }

                if (calculateDeltaPosition.z > currentMaxZ)
                {
                    currentMaxZ = calculateDeltaPosition.z;
                }
            }

            if ((currentMaxX - currentMinX) * (currentMaxZ - currentMinZ) < minArea)
            {
                minX = currentMinX;
                minZ = currentMinZ;
                maxX = currentMaxX;
                maxZ = currentMaxZ;
                
                minAreaAngle = currentAngle;
                minAreaPositionIndex = i;
            }
        }

        result.Width = Mathf.Abs(maxX - minX);
        result.Height = Mathf.Abs(maxZ - minZ);
        
        result.Points[0] = Quaternion.Euler(0.0f, minAreaAngle, 0.0f) * new Vector3(minX, 0.0f, maxZ) + convexHull[minAreaPositionIndex];
        result.Points[1] = Quaternion.Euler(0.0f, minAreaAngle, 0.0f) * new Vector3(minX, 0.0f, minZ) + convexHull[minAreaPositionIndex];
        result.Points[2] = Quaternion.Euler(0.0f, minAreaAngle, 0.0f) * new Vector3(maxX, 0.0f, minZ) + convexHull[minAreaPositionIndex];
        result.Points[3] = Quaternion.Euler(0.0f, minAreaAngle, 0.0f) * new Vector3(maxX, 0.0f, maxZ) + convexHull[minAreaPositionIndex];

        return result;
    }

    public static Vector3[] GetSplitLine(Vector3[] boundingBox, float additionalRange = 0.0f)
    {
        var horizontalSplitLine1 = Vector3.Lerp(boundingBox[0], boundingBox[1], 0.5f);
        var horizontalSplitLine2 = Vector3.Lerp(boundingBox[2], boundingBox[3], 0.5f);
        var verticalSplitLine1 = Vector3.Lerp(boundingBox[0], boundingBox[3], 0.5f);
        var verticalSplitLine2 = Vector3.Lerp(boundingBox[1], boundingBox[2], 0.5f);

        var splitLine =
            (horizontalSplitLine1 - horizontalSplitLine2).sqrMagnitude <=
            (verticalSplitLine1 - verticalSplitLine2).sqrMagnitude
                ? new Vector3[] { horizontalSplitLine1, horizontalSplitLine2 }
                : new Vector3[] { verticalSplitLine1, verticalSplitLine2 };
        
        var splitLineDirection = splitLine[1] - splitLine[0];
        splitLineDirection.Normalize();
        
        splitLine[0] -= splitLineDirection * additionalRange;
        splitLine[1] += splitLineDirection * additionalRange;

        return splitLine;
    }

    public static List<List<Vector3>> SplitPolygon(List<Vector3> polygon, Vector3 splitLinePoint1, Vector3 splitLinePoint2)
    {
        List<List<Vector3>> result = new List<List<Vector3>>();
        List<Vector3> leftResult = new List<Vector3>();
        List<Vector3> rightResult = new List<Vector3>();
        
        LinkedVector3 firstPoint = null;
        List<LinkedVector3> linkedPolygonEdges = new List<LinkedVector3>();
        
        for (int i = 0; i < polygon.Count; i++)
        {
            var linkedPoint = new LinkedVector3(){Point = polygon[i]};
            firstPoint ??= linkedPoint;
            linkedPolygonEdges.Add(linkedPoint);
        }

        for (int i = 0; i < linkedPolygonEdges.Count; i++)
        {
            linkedPolygonEdges[i].Next = linkedPolygonEdges[(i + 1) % linkedPolygonEdges.Count];
        }

        float lastMinIntersectPoint = float.MaxValue;
        Vector3 lastMinIntersectPosition = Vector3.zero;
        
        LinkedVector3 firstIntersectStartPoint = null;
        LinkedVector3 lastIntersectStartPoint = null;
        LinkedVector3 leftFirstCreatePoint = new LinkedVector3();
        LinkedVector3 rightFirstCreatePoint = new LinkedVector3();
        
        List<LinkedVector3> firstIntersectEdgeLists = new List<LinkedVector3>();

        for(int i = 0; i < linkedPolygonEdges.Count; i++)
        {
            var polygonEdge = linkedPolygonEdges[i];
            polygonEdge.IterationIndex = i;
            if (!MathUtil.IsCrossLine(polygonEdge.Point, polygonEdge.Next.Point, splitLinePoint1, splitLinePoint2))
            { 
                continue;
            }

            var threshold = Mathf.Abs(Vector3.Cross((polygonEdge.Next.Point - polygonEdge.Point).normalized,
                (splitLinePoint2 - splitLinePoint1).normalized).magnitude);

            if (Mathf.Abs(threshold) < 0.01f)
            {
                continue;
            }
            
            firstIntersectEdgeLists.Add(polygonEdge);
        }
        
        firstIntersectEdgeLists.Sort((e1, e2) => Vector3.Distance(MathUtil.GetCrossPoint(e2.Point, e2.Next.Point,
            splitLinePoint1, splitLinePoint2), splitLinePoint1).CompareTo(Vector3.Distance(MathUtil.GetCrossPoint(e1.Point, e1.Next.Point,
            splitLinePoint1, splitLinePoint2), splitLinePoint1)));

        if (firstIntersectEdgeLists.Count == 0)
        {
            return new List<List<Vector3>>();
        }
        
        firstIntersectStartPoint = firstIntersectEdgeLists[0];
        leftFirstCreatePoint.Point = MathUtil.GetCrossPoint(firstIntersectStartPoint.Point, firstIntersectStartPoint.Next.Point,
            splitLinePoint1, splitLinePoint2);
        rightFirstCreatePoint.Point = leftFirstCreatePoint.Point;

        for(int i = 0; i < linkedPolygonEdges.Count; i++)
        {
            var polygonEdge = linkedPolygonEdges[(i + firstIntersectStartPoint.IterationIndex + 1) % linkedPolygonEdges.Count];
            
            if (!MathUtil.IsCrossLine(polygonEdge.Point, polygonEdge.Next.Point, splitLinePoint1, splitLinePoint2))
            { 
                continue;
            }
            
            var threshold = Mathf.Abs(Vector3.Cross((polygonEdge.Next.Point - polygonEdge.Point).normalized,
                (splitLinePoint2 - splitLinePoint1).normalized).magnitude);

            if (Mathf.Abs(threshold) < 0.01f)
            {
                continue;
            }
            
            var intersectPosition = MathUtil.GetCrossPoint(polygonEdge.Point,
                polygonEdge.Next.Point, splitLinePoint1, splitLinePoint2);
            var distance = Vector3.Distance(intersectPosition, leftFirstCreatePoint.Point);

            if (distance < 0.01f)
            {
                continue;
            }

            if (distance < lastMinIntersectPoint)
            {
                lastMinIntersectPoint = distance;
                lastMinIntersectPosition = intersectPosition;
                lastIntersectStartPoint = polygonEdge;
            }
        }

        foreach (var linkedPolygonEdge in linkedPolygonEdges)
        {
            if ((linkedPolygonEdge.Point.RefEquals(firstIntersectStartPoint.Point) &&
                 linkedPolygonEdge.Next.Point.RefEquals(lastMinIntersectPosition)) ||
                (linkedPolygonEdge.Next.Point.RefEquals(firstIntersectStartPoint.Point) &&
                 linkedPolygonEdge.Point.RefEquals(lastMinIntersectPosition)))
            {
                return SplitPolygon(polygon, splitLinePoint2, splitLinePoint1);
            }
        }
        
        var leftLastCreatePoint = new LinkedVector3()
        {
            Point = lastMinIntersectPosition
        };
        var rightLastCreatePoint = new LinkedVector3()
        {
            Point = lastMinIntersectPosition
        };
        
        rightFirstCreatePoint.Next = firstIntersectStartPoint.Next;
        firstIntersectStartPoint.Next = leftFirstCreatePoint;
        leftFirstCreatePoint.Next = leftLastCreatePoint;
        leftLastCreatePoint.Next = lastIntersectStartPoint.Next;
        
        lastIntersectStartPoint.Next = rightLastCreatePoint;
        rightLastCreatePoint.Next = rightFirstCreatePoint;

        var iterationPoint = leftFirstCreatePoint.Next;
        
        while (iterationPoint.Index != leftFirstCreatePoint.Index)
        {
            leftResult.Add(iterationPoint.Point);
            iterationPoint = iterationPoint.Next;
        }
        leftResult.Add(iterationPoint.Point);
        
        iterationPoint = rightFirstCreatePoint.Next;

        while (iterationPoint.Index != rightFirstCreatePoint.Index)
        {
            rightResult.Add(iterationPoint.Point);
            iterationPoint = iterationPoint.Next;
        }
        rightResult.Add(iterationPoint.Point);
        
        result.Add(leftResult);
        result.Add(rightResult);

        return result;
    }
}
