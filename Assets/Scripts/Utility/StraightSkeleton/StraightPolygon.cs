using System.Collections.Generic;
using UnityEngine;

namespace Utility.StraightSkeleton
{
    public class StraightPolygon
    {
        public class Vertex
        {
            public enum eIntersectionType
            {
                eNone,
                eSplit,
                eEdge
            }

            private static int _lastIndex;
            public readonly int Index = GetIndex();
            public Vector3 Position;
            public Vector3 Direction;
        
            public Vertex PreviousVertex;
            public Vertex NextVertex;
            public Vertex DefaultNextVertex;
        
            public Edge PreviousEdge;
            public Edge NextEdge;
        
            public bool IsProcessed;
            public eIntersectionType IntersectionType = eIntersectionType.eEdge; 
        
            public Vertex DirectingPointA;
            public Vertex DirectingPointB;
            public Vertex DirectingPointOrigin;
            public float DistanceToEdge;

            private static int GetIndex()
            {
                _lastIndex++;
                return _lastIndex;
            }
        }

        public class Edge
        {
            private static int _lastIndex;
            private readonly int _index = _GetIndex();
            public Vertex DefaultPreviousVertex;
            public Vertex DefaultNextVertex;
            public Vertex PreviousVertex;
            public Vertex NextVertex;

            public readonly List<Vertex> PrevSubVertices = new();
            public readonly List<Vertex> NextSubVertices = new();
        
            public override bool Equals(object obj)
            {
                if (obj!.GetType() != typeof(Edge))
                {
                    return false;
                }

                var e = obj as Edge;
        
                return e != null && ((PreviousVertex.Position.RefEquals(e.PreviousVertex.Position) && NextVertex.Position.RefEquals(e.NextVertex.Position)) || (PreviousVertex.Position.RefEquals(e.NextVertex.Position) && NextVertex.Position.RefEquals(e.PreviousVertex.Position)));
            }

            public override int GetHashCode()
            {
                return _index;
            }

            private static int _GetIndex()
            {
                _lastIndex++;
                return _lastIndex;
            }

            public Vector3 GetDirection()
            {
                return (DefaultNextVertex.Position - DefaultPreviousVertex.Position).normalized;
            }

            public Vector3 GetDirectionReverse()
            {
                return (DefaultPreviousVertex.Position - DefaultNextVertex.Position).normalized;
            }

            public Vertex FindNextVertex(List<Vertex> subPolygon)
            {
                foreach (var vertex in NextSubVertices)
                {
                    if (subPolygon.Contains(vertex))
                    {
                        return vertex;
                    }
                }

                return NextVertex;
            }

            public Vertex FindPrevVertex(List<Vertex> subPolygon)
            {
                foreach (var vertex in PrevSubVertices)
                {
                    if (subPolygon.Contains(vertex))
                    {
                        return vertex;
                    }
                }

                return PreviousVertex;
            }
        }

        private class FindOppositeEdgeResult
        {
            public Edge OppositeEdge;
            public float DistanceToEdge;
            public Vector3 IntersectPoint;
        }
    
        private readonly List<Edge> _defaultEdges = new List<Edge>();
        private readonly List<Vertex> _vertices = new List<Vertex>();
        private readonly List<Vertex> _priorityQueue = new List<Vertex>();
    
        public List<Edge> Process(List<Vector3> vertices)
        {
            InitializeVertices(vertices);
        
            var results = new List<Edge>();
        
            while (_priorityQueue.Count != 0)
            {
                var currentIntersection = _priorityQueue[0];
                _priorityQueue.RemoveAt(0);
            
                var currentOriginVertex = currentIntersection.DirectingPointOrigin;
        
                if (currentOriginVertex.PreviousVertex != null &&
                    currentOriginVertex.NextVertex != null)
                {
                    if (currentOriginVertex.PreviousVertex.Index ==
                        currentOriginVertex.NextVertex.Index)
                    {
                        results.Add(new Edge()
                            { PreviousVertex = currentOriginVertex, NextVertex = currentOriginVertex.NextVertex });

                        currentOriginVertex.PreviousVertex.IsProcessed = true;
                        currentOriginVertex.NextVertex.IsProcessed = true;
                        continue;
                    }
                }

                if (currentIntersection.IntersectionType == Vertex.eIntersectionType.eEdge)
                {
                    ProcessEdgeEvent(currentIntersection, results);
                }
                else if(currentIntersection.IntersectionType == Vertex.eIntersectionType.eSplit)
                {
                    _ProcessSplitEvent(currentIntersection, results);
                }
            }

            return results;
        }

        private void InitializeVertices(List<Vector3> vertices)
        {
            _defaultEdges.Clear();
            _priorityQueue.Clear();
            _vertices.Clear();
        
            var nonConvexIntersection = new List<Vertex>();
        
            for (int i = 0; i < vertices.Count; i++)
            {
                var current = vertices[i];
                var next = vertices[i + 1 >= vertices.Count ? 0 : i + 1];
                var prev = vertices[i - 1 < 0 ? vertices.Count - 1 : i - 1];

                var vertex = new Vertex
                {
                    Position = current,
                    Direction = MathUtil.GetBisectorVector3(current, prev, next)
                };
            
                _vertices.Add(vertex);
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                int prevIndex = i - 1 < 0 ? vertices.Count - 1 : i - 1;
                int nextIndex = i + 1 >= vertices.Count ? 0 : i + 1;
            
                var current = _vertices[i];
                var prev = _vertices[prevIndex];
                var next = _vertices[nextIndex];
            
                var nextEdge = new Edge
                {
                    PreviousVertex = current,
                    DefaultPreviousVertex = current,
                    NextVertex = next,
                    DefaultNextVertex = next
                };
            
                _defaultEdges.Add(nextEdge);

                current.NextEdge = nextEdge;
            
                current.PreviousVertex = prev;
                current.NextVertex = next;
                current.DefaultNextVertex = next;
            }

            for (int i = 0; i < _defaultEdges.Count; i++)
            {
                int nextIndex = i + 1 >= _defaultEdges.Count ? 0 : i + 1;

                _defaultEdges[i].NextVertex = _defaultEdges[nextIndex].PreviousVertex;
                _defaultEdges[i].DefaultNextVertex = _defaultEdges[nextIndex].DefaultPreviousVertex;
                _defaultEdges[nextIndex].PreviousVertex.PreviousEdge = _defaultEdges[i];
            }

            foreach (var vertex in _vertices)
            {
                var intersection = _CreateNewIntersection(vertex, vertex.PreviousVertex, vertex.NextVertex);
                float angle = MathUtil.GetAngleAxis(vertex.Position, vertex.PreviousVertex.Position, vertex.NextVertex.Position);

                if (angle >= 180.0f)
                {
                    if (intersection.IntersectionType.Equals(Vertex.eIntersectionType.eNone))
                    {
                        intersection.DistanceToEdge = float.PositiveInfinity;
                    }
                
                    nonConvexIntersection.Add(intersection);
                }
            
                _priorityQueue.Add(intersection);
            }

            foreach (var vertex in nonConvexIntersection)
            {
                var findOppositeEdgeResult = _FindOppositeEdge(vertex.DirectingPointOrigin);

                if (!(findOppositeEdgeResult.DistanceToEdge < vertex.DistanceToEdge)) continue;
            
                vertex.Position = findOppositeEdgeResult.IntersectPoint;
                vertex.IntersectionType = Vertex.eIntersectionType.eSplit;
                vertex.DistanceToEdge = findOppositeEdgeResult.DistanceToEdge;
            }

            _priorityQueue.Sort((v1, v2) => v1.DistanceToEdge.CompareTo(v2.DistanceToEdge));
        }

        private void ProcessEdgeEvent(Vertex currentIntersection, List<Edge> results)
        {
            if ((currentIntersection.DirectingPointA?.IsProcessed ?? true) ||
                (currentIntersection.DirectingPointB?.IsProcessed ?? true))
            {
                return;
            }

            if (currentIntersection.DirectingPointA.NextVertex.NextVertex.Index ==
                currentIntersection.DirectingPointB.Index)
            {
                results.Add(new Edge()
                    { PreviousVertex = currentIntersection, NextVertex = currentIntersection.DirectingPointA });
                results.Add(new Edge()
                    { PreviousVertex = currentIntersection, NextVertex = currentIntersection.DirectingPointA.NextVertex });
                results.Add(new Edge()
                    { PreviousVertex = currentIntersection, NextVertex = currentIntersection.DirectingPointB });
            
                currentIntersection.DirectingPointA.IsProcessed = true;
                currentIntersection.DirectingPointA.NextVertex.IsProcessed = true;
                currentIntersection.DirectingPointB.IsProcessed = true;
            
                return;
            }
        
        
            currentIntersection.DirectingPointA.IsProcessed = true;
            currentIntersection.DirectingPointB.IsProcessed = true;

            var outputPrevEdge = new Edge()
            {
                PreviousVertex = currentIntersection, DefaultPreviousVertex = currentIntersection, NextVertex = currentIntersection.DirectingPointA,
                DefaultNextVertex = currentIntersection.DirectingPointA
            };
            var outputNextEdge = new Edge()
            {
                PreviousVertex = currentIntersection, DefaultPreviousVertex = currentIntersection, NextVertex = currentIntersection.DirectingPointB,
                DefaultNextVertex = currentIntersection.DirectingPointA
            }; 
        
            results.Add(outputPrevEdge);
            results.Add(outputNextEdge);

            currentIntersection.NextEdge = currentIntersection.DirectingPointA.NextEdge;
            currentIntersection.NextVertex = currentIntersection.DirectingPointA.NextVertex;
            currentIntersection.NextVertex.PreviousVertex = currentIntersection;
            currentIntersection.NextEdge.PreviousVertex = currentIntersection;
            currentIntersection.PreviousEdge = currentIntersection.DirectingPointB.PreviousEdge;
            currentIntersection.PreviousVertex = currentIntersection.DirectingPointB.PreviousVertex;
            currentIntersection.PreviousVertex.NextVertex = currentIntersection;
            currentIntersection.PreviousEdge.NextVertex = currentIntersection;

            var next = currentIntersection.NextVertex;
            var prev = currentIntersection.PreviousVertex;

            var prevDir = (currentIntersection.PreviousEdge.DefaultPreviousVertex.Position -
                           currentIntersection.PreviousEdge.DefaultNextVertex.Position).normalized;
            var nextDir = (currentIntersection.NextEdge.DefaultNextVertex.Position -
                           currentIntersection.NextEdge.DefaultPreviousVertex.Position).normalized;

            currentIntersection.Direction = MathUtil.GetBisectorVector3(prevDir, nextDir);

            Vertex intersection = _CreateNewIntersection(currentIntersection, prev, next);
        
            _InsertIntersection(intersection);
        }

        private void _ProcessSplitEvent(Vertex currentIntersection, List<Edge> results)
        {
            if (currentIntersection.DirectingPointOrigin?.IsProcessed ?? false)
            {
                return;
            }
        
            results.Add(new Edge(){PreviousVertex = currentIntersection, NextVertex = currentIntersection.DirectingPointOrigin});

            var currentVertex = currentIntersection.DirectingPointOrigin;

            var subPolygon = new List<Vertex> { currentVertex };
        
            // ReSharper disable once PossibleNullReferenceException
            // 알고리즘 상 데이터가 항상 있다고 가정
            var subPolygonIterator = currentVertex.NextVertex;

            while (subPolygonIterator.Index != currentVertex.Index)
            {
                subPolygon.Add(subPolygonIterator);
                subPolygonIterator = subPolygonIterator.NextVertex;
            }
        
            Edge oppositeEdge = _FindOppositeEdge(currentVertex).OppositeEdge;

            Vertex oppositeEdgePrevVertex = oppositeEdge.FindPrevVertex(subPolygon);
            Vertex oppositeEdgeNextVertex = oppositeEdge.FindNextVertex(subPolygon);
        
            currentVertex.IsProcessed = true;

            Vertex b1 = new Vertex()
            {
                Position = currentIntersection.Position,
                PreviousVertex = currentVertex.PreviousVertex,
                NextVertex = oppositeEdgeNextVertex,
                PreviousEdge = currentVertex.PreviousEdge,
                NextEdge = oppositeEdge
            };

            currentVertex.PreviousEdge.NextVertex = b1;
            b1.NextVertex.PreviousVertex = b1;
            b1.PreviousVertex.NextVertex = b1;
        
            Vertex b2 = new Vertex()
            {
                Position = currentIntersection.Position,
                PreviousVertex = oppositeEdgePrevVertex,
                NextVertex = currentVertex.NextVertex,
                PreviousEdge = oppositeEdge,
                NextEdge = currentVertex.NextEdge
            };

            currentVertex.NextEdge.PreviousVertex = b2;
            b2.NextVertex.PreviousVertex = b2;
            b2.PreviousVertex.NextVertex = b2;
        
            b1.Direction = MathUtil.GetBisectorVector3(b1.PreviousEdge.GetDirectionReverse(), b1.NextEdge.GetDirection());
            b2.Direction = MathUtil.GetBisectorVector3(b2.PreviousEdge.GetDirectionReverse(), b2.NextEdge.GetDirection());
        
            oppositeEdge.PrevSubVertices.Add(b1);
            oppositeEdge.NextSubVertices.Add(b2);

            var newIntersectB1 = _CreateNewIntersection(b1, b1.PreviousVertex, b1.NextVertex);
            _InsertIntersection(newIntersectB1);

            var newIntersectB2 = _CreateNewIntersection(b2, b2.PreviousVertex, b2.NextVertex);
            _InsertIntersection(newIntersectB2);
        }
    
        Vertex _CreateNewIntersection(Vertex current, Vertex prev, Vertex next)
        {
            Vector3 previousBisectorIntersectionPoint =
                MathUtil.GetCrossPoint(prev.Position, prev.Position + prev.Direction, current.Position, current.Position + current.Direction);
            float previousIntersectionToEdge =
                MathUtil.GetLineToPositionDistance(current.PreviousEdge.DefaultPreviousVertex.Position, current.PreviousEdge.DefaultNextVertex.Position, previousBisectorIntersectionPoint);

            bool isPrevIntersectInside = IsAngleInsidePosition(previousBisectorIntersectionPoint, prev.PreviousVertex.Position,
                prev.Position, current.Position);
            
            Vector3 nextBisectorIntersectionPoint =
                MathUtil.GetCrossPoint(current.Position, current.Position + current.Direction, next.Position,  next.Position + next.Direction);
            float nextIntersectionToEdge =
                MathUtil.GetLineToPositionDistance( current.NextEdge.DefaultPreviousVertex.Position,current.NextEdge.DefaultNextVertex.Position, nextBisectorIntersectionPoint);
        
            bool isNextIntersectInside = IsAngleInsidePosition(nextBisectorIntersectionPoint, current.Position,
                next.Position, next.NextVertex.Position);
        
            Vertex intersection = new Vertex
            {
                IntersectionType = Vertex.eIntersectionType.eEdge
            };

            switch (isNextIntersectInside)
            {
                case true when !isPrevIntersectInside:
                    LinkToNextVertex();
                    break;
                case false when isPrevIntersectInside:
                    LinkToPrevVertex();
                    break;
                case true when nextIntersectionToEdge <= previousIntersectionToEdge:
                    LinkToNextVertex();
                    break;
                case true:
                    LinkToPrevVertex();
                    break;
                default:
                    intersection.IntersectionType = Vertex.eIntersectionType.eNone;
                    break;
            }

            intersection.DirectingPointOrigin = current;

            return intersection;

            void LinkToPrevVertex()
            {
                intersection.Position = previousBisectorIntersectionPoint;
                intersection.DirectingPointA = current;
                intersection.DirectingPointB = prev;
                intersection.DistanceToEdge = previousIntersectionToEdge;
            }

            void LinkToNextVertex()
            {
                intersection.Position = nextBisectorIntersectionPoint;
                intersection.DirectingPointA = next;
                intersection.DirectingPointB = current;
                intersection.DistanceToEdge = nextIntersectionToEdge;
            }
        }

        FindOppositeEdgeResult _FindOppositeEdge(Vertex vertex)
        {
            var result = new FindOppositeEdgeResult
            {
                DistanceToEdge = float.PositiveInfinity
            };

            foreach (var edge in _defaultEdges)
            {
                Vector3 intersectPoint = GetIntersectPointWithOppositeEdge(vertex, edge);
            
                if (!IsVertexInFrontOfEdge(intersectPoint,edge))
                {
                    continue;
                }
                
                float distance = MathUtil.GetLineToPositionDistance(edge.DefaultPreviousVertex.Position, edge.DefaultNextVertex.Position, intersectPoint);
        
                if (distance < result.DistanceToEdge)
                {
                    result.DistanceToEdge = distance;
                    result.OppositeEdge = edge;
                    result.IntersectPoint = intersectPoint;
                }
            }

            return result;
        }

        private void _InsertIntersection(Vertex vertex)
        {
            for (int i = 0; i < _priorityQueue.Count; i++)
            {
                if (vertex.DistanceToEdge < _priorityQueue[i].DistanceToEdge)
                {
                    _priorityQueue.Insert(i, vertex);
                    return;
                }
            }
        
            _priorityQueue.Add(vertex);
        }

        private static Vector3 GetIntersectPointWithOppositeEdge(Vertex origin, Edge oppositeEdge)
        {
            return GetIntersectPointWithOppositeEdge(origin.Position, origin.DefaultNextVertex.Position, origin.Direction, oppositeEdge.DefaultPreviousVertex.Position, oppositeEdge.DefaultNextVertex.Position);
        }
    
        public static Vector3 GetIntersectPointWithOppositeEdge(Vector3 origin, Vector3 originNextPosition, Vector3 originBisectorDirection, Vector3 edgePosition, Vector3 edgeNextPosition)
        {
            Vector3 testIntersectPoint = MathUtil.GetCrossPoint(edgePosition, edgeNextPosition, origin,
                originNextPosition);
        
            Vector3 testIntersectBisector = MathUtil.GetBisectorVector3(testIntersectPoint, origin, edgePosition);

            return MathUtil.GetCrossPoint(origin, origin + originBisectorDirection * 2.0f, testIntersectPoint,
                testIntersectPoint + testIntersectBisector * 2.0f);
        }

        private static bool IsVertexInFrontOfEdge(Vector3 origin, Edge edge)
        {
            bool isPrevAngleInside = IsAngleInside(origin, (edge.DefaultNextVertex.Position - edge.DefaultPreviousVertex.Position).normalized, edge.DefaultPreviousVertex);
            bool isNextAngleInside = IsAngleInside(origin, (edge.DefaultPreviousVertex.Position - edge.DefaultNextVertex.Position).normalized, edge.DefaultNextVertex); 
        
            return isPrevAngleInside && isNextAngleInside;
        }

        private static bool IsAngleInside(Vector3 origin, Vector3 edgeDirection, Vertex edgeVertex)
        {
            float maxAngle = MathUtil.GetAngleAxis(edgeDirection, edgeVertex.Direction);
            float testedAngle = MathUtil.GetAngleAxis(edgeDirection, (origin - edgeVertex.Position).normalized);

            if (!(maxAngle >= 180.0f)) return 0.0f <= testedAngle && testedAngle <= maxAngle;
        
            maxAngle = 360.0f - maxAngle;
            testedAngle = 360.0f - testedAngle;

            return 0.0f <= testedAngle && testedAngle <= maxAngle;
        }

        private static bool IsAngleInside(Vector3 origin, Vector3 from, Vector3 to, Vector3 center)
        {
            float maxAngle = MathUtil.GetAngleAxis(from, to);
            float testedAngle = MathUtil.GetAngleAxis(from, (origin - center).normalized);

            return 0.0f <= testedAngle && testedAngle <= maxAngle;
        }

        private static bool IsAngleInsidePosition(Vector3 origin, Vector3 prevPosition, Vector3 centerPosition, Vector3 nextPosition)
        {
            return IsAngleInside(origin, (prevPosition - centerPosition).normalized,
                (nextPosition - centerPosition).normalized, centerPosition);
        }
    }
}
