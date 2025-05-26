using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Streamline
{
    private TensorFieldContainer _tensorFieldContainer;
    private VertexField _vertexField;
    private VertexField _seedVertexField;
    private Vector3 _size;
    private float _distance;
    public int Index = 0;
    private static int _Index = 0;
    public List<Vertex> ContainsVertices = new List<Vertex>();
    public List<Vector3> ContainsPositions = new List<Vector3>();
    
    public class Vertex
    {
        public Vector3 Position;
        public Vertex NextVertex;
        public Streamline ContainsStreamline;
        public List<Vertex> LinkedVertices = new List<Vertex>();
        public int Index = 0;
        public bool IsCalculated = false;
        private static int _Index = 0;
        
        public Vertex(Vector3 position, bool isSeedPoint)
        {
            Position = position;

            if (!isSeedPoint)
            {
                Index = _Index;
                _Index++;
            }
            
        }

        public override bool Equals(object obj)
        {
            var otherVertex = obj as Vertex;
            return otherVertex?.Index.Equals(Index) ?? false;
        }

        public override int GetHashCode()
        {
            return Index;
        }
    }

    public class TraceResult
    {
        public List<Vertex> SeedPoints = new List<Vertex>();
    }

    class TraceResultInternal
    {
        public bool IsEndWithCircle = false;
    }

    public Streamline(TensorFieldContainer tensorFieldContainer, VertexField field, VertexField seedVertexField, float distance)
    {
        Index = _Index;
        _tensorFieldContainer = tensorFieldContainer;
        _distance = distance;
        _vertexField = field;
        _seedVertexField = seedVertexField;
        _Index++;
    }
    
    public TraceResult Trace(Vector3 maxSize, Vertex seedPoint, bool isMajor)
    {
        var result = new TraceResult();

        seedPoint.ContainsStreamline = this;

        if (seedPoint.IsCalculated)
        {
            return result;
        }
        
        _size = maxSize;
        
        Vector3 dir = _tensorFieldContainer.GetTensorSampling(seedPoint.Position, isMajor);
        
        if(_vertexField.IsSameDirectionExist(seedPoint.Position, dir.normalized, _distance * 0.9f, 0.9f))
        {
            return result;
        }

        if (_vertexField.IsOtherVertexExist(seedPoint.Position,  this, 0.2f, out var existVertex))
        {
            return result;
        }
        
        if (TryCreateSeedPoint(GetSeedPoint(seedPoint.Position, dir, _distance), out var leftSeedPoint))
        {
            result.SeedPoints.Add(leftSeedPoint);
        }
        
        if (TryCreateSeedPoint(GetSeedPoint(seedPoint.Position, dir * -1.0f, _distance), out var rightSeedPoint))
        {
            result.SeedPoints.Add(rightSeedPoint);
        }
        
        var startVertex = new Vertex(seedPoint.Position, false)
        {
            ContainsStreamline = this
        };
        
        ContainsVertices.Add(startVertex);
        ContainsPositions.Add(startVertex.Position);
        
        var reverseTrace = TraceStreamlineResult(result.SeedPoints, startVertex, isMajor, true, true);
        ContainsVertices.Reverse();
        ContainsPositions.Reverse();

        if (!reverseTrace.IsEndWithCircle)
        {
            TraceStreamlineResult(result.SeedPoints, startVertex, isMajor, false, false);
        }
        
        
        for(int i = 0; i < ContainsVertices.Count - 1; i++)
        {
            ContainsVertices[i].NextVertex = ContainsVertices[i + 1];
            _vertexField.GetBoundingBox(ContainsVertices[i].Position).Vertices.Add(ContainsVertices[i]);            
        }

        if (ContainsVertices.Count != 0)
        {
            _vertexField.GetBoundingBox(ContainsVertices[^1].Position).Vertices.Add(ContainsVertices[^1]);
        }
        

        return result;
    }

    private TraceResultInternal TraceStreamlineResult(List<Vertex> seedPoints, Vertex startVertexPoint, bool isMajor, bool isReverse, bool checkCircle)
    {
        var traceResult = new TraceResultInternal();
        
        Vector3 prevPos = startVertexPoint.Position;
        Vertex previousVertex = startVertexPoint;
        Vector3 prevDir = _tensorFieldContainer.GetTensorSampling(prevPos, isMajor) * (isReverse ? -1.0f : 1.0f);

        int maximumCalculateCount = 2000;
        float leftSeedPointLengthStep = 0.0f;
        float rightSeedPointLengthStep = 0.0f;
        
        while ((prevPos.x < _size.x &&
               prevPos.x >= 0.0f && 
               prevPos.z < _size.z &&
               prevPos.z >= 0.0f))
        {
            maximumCalculateCount--;
            
            if (maximumCalculateCount <= 0)
            {
                break;
            }
            
            Vector3 nextPosition = _tensorFieldContainer.GetRungeKuttaSampling(prevPos, prevDir, isReverse, isMajor, 0.1f);
            
            prevDir = (nextPosition - prevPos);

            float deltaDistance = prevDir.magnitude;

            if (deltaDistance < float.Epsilon)
            {
                break;
            }

            var currentVertex = new Vertex(nextPosition, false)
            {
                ContainsStreamline = this
            };

            if (checkCircle)
            {
                if (ContainsVertices.Count > 50 &&
                    Vector3.Distance(prevPos, startVertexPoint.Position) <= 0.1f)
                {
                    traceResult.IsEndWithCircle = true;
                    previousVertex.LinkedVertices.Add(currentVertex); 
                    currentVertex.LinkedVertices.Add(startVertexPoint); 
                    currentVertex.NextVertex = startVertexPoint;
                    ContainsVertices.Add(currentVertex);
                    ContainsPositions.Add(currentVertex.Position);
                    break;
                }
            }
            

            if (IsOtherStreamlineContact(previousVertex, nextPosition, out var contactPosition))
            {
                break;
            }

            if (_seedVertexField.IsOtherVertexExist(nextPosition, this, 0.11f, out var existVertex))
            {
                existVertex.IsCalculated = true;
                _seedVertexField.GetBoundingBox(nextPosition).Vertices.Remove(existVertex);
            }

            previousVertex.LinkedVertices.Add(currentVertex); 
            
            ContainsVertices.Add(currentVertex);
            ContainsPositions.Add(currentVertex.Position);

            leftSeedPointLengthStep += deltaDistance;
            rightSeedPointLengthStep += deltaDistance;
            
            if (leftSeedPointLengthStep >= _distance)
            {
                if (TryCreateSeedPoint(GetSeedPoint(nextPosition, prevDir, _distance), out var newSeedPoint))
                {
                    seedPoints.Add(newSeedPoint);
                    leftSeedPointLengthStep = 0.0f;
                }
            }

            if (rightSeedPointLengthStep >= _distance)
            {
                if (TryCreateSeedPoint(GetSeedPoint(nextPosition, -prevDir, _distance), out var newSeedPoint))
                {
                    seedPoints.Add(newSeedPoint);
                    rightSeedPointLengthStep = 0.0f;
                }
            }
            
            prevPos = nextPosition;
            previousVertex = currentVertex;
        }

        return traceResult;
    }
    
    Vector3 GetSeedPoint(Vector3 position, Vector3 direction, float distance)
    {
        Vector3 reverseVector = new Vector3(direction.z, 0.0f, -direction.x).normalized;
        return position + reverseVector * distance;
    }

    bool IsOtherStreamlineContact(Vertex prevPos, Vector3 nextPosition, out Vertex contactPosition)
    {
        contactPosition = null;
        
        if (prevPos == null)
        {
            return false;
        }
        
        var rangedVertices = _vertexField.GetRangedBoundingBox(nextPosition, 0.1f);
        bool isCross = false;
        bool isContact = rangedVertices.Count != 0;
        float crossMinDistance = float.MaxValue;
        float contactMinDistance = float.MaxValue;
        Vertex contactMinPoint = null;

        Vertex crossPrevVertex = null;
        Vertex crossNextVertex = null;
        Vector3 crossMinPoint = Vector3.zero;

        foreach (var rangedVertex in rangedVertices)
        {
            if (rangedVertex.NextVertex != null)
            {
                if (MathUtil.IsCrossLine(prevPos.Position, nextPosition, rangedVertex.Position,
                        rangedVertex.NextVertex.Position))
                {
                    Vector3 contactPos = MathUtil.GetCrossPoint(prevPos.Position, nextPosition, rangedVertex.Position,
                        rangedVertex.NextVertex.Position);
                    
                    float distance = Vector3.Distance(contactPos, nextPosition);
                    
                    if (distance <= crossMinDistance)
                    {
                        isCross = true;
                        crossMinDistance = distance;
                        crossPrevVertex = rangedVertex;
                        crossNextVertex = rangedVertex.NextVertex;
                        crossMinPoint = contactPos;
                    }
                    
                }
            }

            float contactDistance = Vector3.Distance(rangedVertex.Position, nextPosition);
            if (contactDistance <= contactMinDistance)
            {
                contactMinDistance = contactDistance;
                contactMinPoint = rangedVertex;
            }
        }
        
        if(!(isCross || isContact))
        {
            return false;
        }

        if (contactMinDistance < crossMinDistance)
        {
            contactPosition = contactMinPoint;
            contactPosition.LinkedVertices.Add(prevPos);
            ContainsPositions.Add(contactPosition.Position);
            return true;
        }
        else
        {
            contactPosition = new Vertex(crossMinPoint, false)
            {
                NextVertex = crossNextVertex
            };
            
            ContainsVertices.Add(contactPosition);
            ContainsPositions.Add(contactPosition.Position);
            crossPrevVertex.NextVertex = contactPosition;
            contactPosition.LinkedVertices.Add(prevPos);
            contactPosition.LinkedVertices.Add(crossNextVertex);
            contactPosition.LinkedVertices.Add(crossPrevVertex);
            return true;
        }
    }
    
    bool TryCreateSeedPoint(Vector3 seedPosition, out Vertex newSeedPoint)
    {
        if (seedPosition.x < _size.x &&
            seedPosition.x >= 0.0f &&
            seedPosition.z < _size.z &&
            seedPosition.z >= 0.0f)
        {
            if (!_vertexField.IsOtherVertexExist(seedPosition, _distance))
            {
                bool isOtherVertexExist = false;
                newSeedPoint = null;
                
                foreach (var vertex in _seedVertexField.GetRangedBoundingBox(seedPosition, _distance))
                {
                    if (Vector3.Distance(vertex.Position, seedPosition) > _distance)
                    {
                        continue;
                    }
                    
                    isOtherVertexExist = true;
                    newSeedPoint = vertex;
                    newSeedPoint.Position = seedPosition;
                }
                
                if (!isOtherVertexExist)
                {
                    newSeedPoint = new Vertex(seedPosition, true);
                    _seedVertexField.GetBoundingBox(seedPosition).Vertices.Add(newSeedPoint);
                }
                
                return true;
            }

            newSeedPoint = null;
            return false;
        }

        newSeedPoint = null;
        return false;
    }
}
