using System;
using System.Collections.Generic;
using System.Linq;
using CityGenerator;
using UnityEngine;
using Utility.StraightSkeleton;

namespace Utility
{
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

    public static class PolygonSplit
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

                    while (!nextVertex.Index.Equals(vertex.Index))
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
                        else if (nextVertex.LinkedVertices.Count == 2)
                        {
                            minAngleVertex = nextVertex.LinkedVertices[0].Equals(prevVertex)
                                ? nextVertex.LinkedVertices[1]
                                : nextVertex.LinkedVertices[0];
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
                        bool isAlreadyContains = allPolygons.Any(existPolygon =>
                            !existPolygon.Indices.Except(polygon.Indices).Any() &&
                            existPolygon.Indices.Count == polygon.Indices.Count);

                        if (!isAlreadyContains)
                        {
                            allPolygons.Add(polygon);
                        }
                    }
                }
            }

            return allPolygons;
        }

        public class LinkedPolygonShrinkPoint
        {
            private static long LatestIndex = 0;
            public long Index;
            public int OriginalIndex;
            public Vector3 Position { get; set; }
            public LinkedPolygonShrinkPoint NextPoint { get; set; }
            public LinkedPolygonShrinkPoint PrevPoint { get; set; }

            public LinkedPolygonShrinkPoint()
            {
                Index = LatestIndex;
                LatestIndex++;
            }
        }

        public static List<List<Vector3>> GetShrinkPolygon(List<Vector3> polygonPoints, float distance)
        {
            List<List<Vector3>> results = new List<List<Vector3>>();
            GetShrinkPolygonInternal(results, polygonPoints, distance);
            return results;
        }

        private static void GetShrinkPolygonInternal(List<List<Vector3>> results, List<Vector3> polygonPoints,
            float distance)
        {
            if (Math.Abs(distance) <= 0.00001f)
            {
                results.Add(polygonPoints);
                return;
            }

            var shrinkPoints = new List<Vector3>();
            var bisectorDirections = new List<Vector3>();
            var distanceParents = new List<float>();
            var linkedShrinkPoints = new List<LinkedPolygonShrinkPoint>();

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
                linkedShrinkPoints.Add(new LinkedPolygonShrinkPoint()
                {
                    Position = currentPosition
                });
            }

            for (int i = 0; i < linkedShrinkPoints.Count; i++)
            {
                linkedShrinkPoints[i].OriginalIndex = i;
                var nextPoint = linkedShrinkPoints[i + 1 > linkedShrinkPoints.Count - 1 ? 0 : i + 1];
                linkedShrinkPoints[i].NextPoint = nextPoint;
                nextPoint.PrevPoint = linkedShrinkPoints[i];
            }

            int minShrinkPointIndex = -1;
            Vector3 minShrinkPoint = Vector3.zero;
            float minShrinkDistance = float.MaxValue;
            LinkedPolygonShrinkPoint minLinkedShrinkEdgePoint = null;
            LinkedPolygonShrinkPoint minLinkedShrinkPoint = null;

            int eventType = -1;

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
                    float distanceToLine =
                        MathUtil.GetLineToPositionDistance(polygonPoints[i], prevPosition, prevShrinkPoint);

                    if (distanceToLine < minShrinkDistance)
                    {
                        eventType = 1;
                        minShrinkDistance = distanceToLine;
                        minShrinkPointIndex = i;
                        minShrinkPoint = prevShrinkPoint;
                    }
                }

                for (int j = 0; j < shrinkPoints.Count - 2; j++)
                {
                    var currentCheckEdgePoint = linkedShrinkPoints[(i + j + 1) % shrinkPoints.Count];
                    float lineToPositionDistance = MathUtil.GetLineToPositionDistance(currentCheckEdgePoint.Position,
                        currentCheckEdgePoint.NextPoint.Position, polygonPoints[i]);

                    if (!(lineToPositionDistance < distance / distanceParents[i] + distance))
                    {
                        continue;
                    }

                    var edgeEventPoint = StraightPolygon.GetIntersectPointWithOppositeEdge(
                        linkedShrinkPoints[i].Position, linkedShrinkPoints[i].NextPoint.Position, bisectorDirections[i],
                        currentCheckEdgePoint.Position, currentCheckEdgePoint.NextPoint.Position);
                    float edgeEventDistance = MathUtil.GetLineToPositionDistance(currentCheckEdgePoint.Position,
                        currentCheckEdgePoint.NextPoint.Position, edgeEventPoint);

                    if (edgeEventDistance < minShrinkDistance)
                    {
                        eventType = 2;
                        minShrinkPoint = edgeEventPoint;
                        minLinkedShrinkPoint = linkedShrinkPoints[i];
                        minShrinkDistance = edgeEventDistance;
                        minLinkedShrinkEdgePoint = currentCheckEdgePoint;
                    }
                }

                if (nextPosition != -1)
                {
                    i = nextPosition;
                }
            }

            if (eventType == 1)
            {
                List<Vector3> vertexEventResult = new List<Vector3>();

                for (int i = 0; i < shrinkPoints.Count; i++)
                {
                    if (i == minShrinkPointIndex)
                    {
                        vertexEventResult.Add(minShrinkPoint);
                        i++;
                    }
                    else
                    {
                        vertexEventResult.Add(polygonPoints[i] +
                                              bisectorDirections[i] * (minShrinkDistance / distanceParents[i]));
                    }
                }

                GetShrinkPolygonInternal(results, vertexEventResult, distance - minShrinkDistance);
                return;
            }

            if (eventType == 2)
            {
                List<LinkedPolygonShrinkPoint> leftShrinkPoints = new List<LinkedPolygonShrinkPoint>();
                List<Vector3> leftShrinkPointResults = new List<Vector3>();
                List<LinkedPolygonShrinkPoint> rightShrinkPoints = new List<LinkedPolygonShrinkPoint>();
                List<Vector3> rightShrinkPointResults = new List<Vector3>();
                LinkedPolygonShrinkPoint leftCreateShrinkPoint = new LinkedPolygonShrinkPoint()
                {
                    Position = minShrinkPoint
                };

                LinkedPolygonShrinkPoint rightCreateShrinkPoint = new LinkedPolygonShrinkPoint()
                {
                    Position = minShrinkPoint
                };

                leftCreateShrinkPoint.NextPoint = minLinkedShrinkPoint.NextPoint;
                rightCreateShrinkPoint.NextPoint = minLinkedShrinkEdgePoint.NextPoint;
                minLinkedShrinkEdgePoint.NextPoint = leftCreateShrinkPoint;
                minLinkedShrinkPoint.PrevPoint.NextPoint = rightCreateShrinkPoint;


                var leftIterator = leftCreateShrinkPoint;
                long leftIterationStartIndex = leftIterator.Index;
                leftIterator = leftIterator.NextPoint;

                var rightIterator = rightCreateShrinkPoint;
                long rightIterationStartIndex = rightIterator.Index;
                rightIterator = rightIterator.NextPoint;

                while (leftIterator.Index != leftIterationStartIndex)
                {
                    leftShrinkPoints.Add(leftIterator);
                    leftIterator = leftIterator.NextPoint;
                }

                leftShrinkPoints.Add(leftIterator);

                while (rightIterator.Index != rightIterationStartIndex)
                {
                    rightShrinkPoints.Add(rightIterator);
                    rightIterator = rightIterator.NextPoint;
                }

                rightShrinkPoints.Add(rightIterator);

                foreach (var shrinkPoint in leftShrinkPoints)
                {
                    if (shrinkPoint.Index == leftCreateShrinkPoint.Index)
                    {
                        leftShrinkPointResults.Add(minShrinkPoint);
                    }
                    else
                    {
                        int originalIndex = shrinkPoint.OriginalIndex;
                        leftShrinkPointResults.Add(polygonPoints[originalIndex] + bisectorDirections[originalIndex] *
                            (minShrinkDistance / distanceParents[originalIndex]));
                    }
                }

                foreach (var shrinkPoint in rightShrinkPoints)
                {
                    if (shrinkPoint.Index == rightCreateShrinkPoint.Index)
                    {
                        rightShrinkPointResults.Add(minShrinkPoint);
                    }
                    else
                    {
                        int originalIndex = shrinkPoint.OriginalIndex;
                        rightShrinkPointResults.Add(polygonPoints[originalIndex] + bisectorDirections[originalIndex] *
                            (minShrinkDistance / distanceParents[originalIndex]));
                    }
                }

                GetShrinkPolygonInternal(results, leftShrinkPointResults, Mathf.Abs(distance - minShrinkDistance));
                GetShrinkPolygonInternal(results, rightShrinkPointResults, Mathf.Abs(distance - minShrinkDistance));
                return;
            }

            results.Add(shrinkPoints);
        }
    }

    public abstract class PolygonUtility
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
}
