using System.Collections.Generic;
using System.Linq;
using CityGenerator;
using UnityEngine;

namespace Utility
{
    public class VertexField
    {
        public class BoundingBox
        {
            public List<Streamline.Vertex> Vertices = new List<Streamline.Vertex>();
            public Vector3 Min;
            public Vector3 Max;

            public BoundingBox(Vector3 min, Vector3 max)
            {
                Min = min;
                Max = max;
            }
        }
    
        private readonly BoundingBox[,] _boundingBoxes;
        public List<BoundingBox> BoundingBoxes { get; } = new();
        private Vector3 _maxSize;
        private readonly Vector3 _cellSize;

        public VertexField(Vector3 maxSize, int count)
        {
            _boundingBoxes = new BoundingBox[count, count];
        
            float xdelta = maxSize.x / count;
            float zdelta = maxSize.z / count;

            _cellSize = new Vector3(xdelta, 0.0f, zdelta);

            for (int xCount = 0; xCount < count; xCount++)
            {
                for (int zCount = 0; zCount < count; zCount++)
                {
                    Vector3 minPosition = new Vector3(xCount * xdelta, 0.0f, zCount * zdelta);
                    Vector3 maxPosition = new Vector3((xCount + 1) * xdelta, 0.0f, (zCount + 1) * zdelta);
                    var boundingBox = new BoundingBox(minPosition, maxPosition);
                    _boundingBoxes[xCount, zCount] = boundingBox;
                    BoundingBoxes.Add(boundingBox);
                }
            }
        }

        public BoundingBox GetBoundingBox(Vector3 position)
        {
            int xIndex = Mathf.Clamp((int)(position.x / _cellSize.x), 0, _boundingBoxes.GetLength(0) - 1);
            int yIndex = Mathf.Clamp((int)(position.z / _cellSize.z), 0, _boundingBoxes.GetLength(1) - 1);

            return _boundingBoxes[xIndex, yIndex];
        }

        public bool IsOtherVertexExist(Vector3 position, float distance)
        {
            var rangedBoundingBox = GetRangedBoundingBox(position, distance);

            return rangedBoundingBox.Any(vertex => Vector3.Distance(vertex.Position, position) <= distance);
        }

        public bool IsOtherVertexExist(Vector3 position, Streamline exceptStreamline, float distance, out Streamline.Vertex existVertex)
        {
            existVertex = null;
            var rangedVertices = GetRangedBoundingBox(position, distance);

            foreach (var vertex in rangedVertices)
            {
                if (vertex.ContainsStreamline != null)
                {
                    if (vertex.ContainsStreamline.Equals(exceptStreamline))
                    {
                        continue;
                    }
                }
            
                    
                if (Vector3.Distance(vertex.Position, position) <= distance)
                {
                    existVertex = vertex;
                    return true;
                }
            }

            return false;
        }

        public bool IsSameDirectionExist(Vector3 position, Vector3 direction,  float maxDistance, float angle)
        {
            var rangedBoundingBox = GetRangedBoundingBox(position, maxDistance);

            foreach (var vertex in rangedBoundingBox)
            {
                if (vertex.NextVertex == null)
                {
                    continue;
                }
            
                var existStreamlineNormal = (vertex.Position - vertex.NextVertex.Position)
                    .normalized;
                if (Mathf.Abs(Vector3.Dot(direction, existStreamlineNormal.normalized)) > angle)
                {
                    return true;
                }
            }

            return false;
        }

        public List<Streamline.Vertex> GetRangedBoundingBox(Vector3 position, float distance)
        {
            List<Streamline.Vertex> result = new List<Streamline.Vertex>();
        
            int xMinIndex = Mathf.Clamp((int)((position.x - distance) / _cellSize.x), 0, _boundingBoxes.GetLength(0) - 1);
            int xMaxIndex = Mathf.Clamp((int)((position.x + distance) / _cellSize.x),  0, _boundingBoxes.GetLength(0) - 1);
            int yMinIndex = Mathf.Clamp((int)((position.z - distance) / _cellSize.z),  0, _boundingBoxes.GetLength(1) - 1);
            int yMaxIndex = Mathf.Clamp((int)((position.z + distance) / _cellSize.z),  0, _boundingBoxes.GetLength(1) - 1);

            for (int x = xMinIndex; x <= xMaxIndex; x++)
            {
                for (int y = yMinIndex; y <= yMaxIndex; y++)
                {
                    var boundingBox = _boundingBoxes[x, y];

                    result.AddRange(boundingBox.Vertices);
                }
            }

            return result;
        }
    }
}
