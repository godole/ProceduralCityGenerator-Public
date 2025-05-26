using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility.WaveFunctionCollapse
{
    public class Cell
    {
        public Vector2Int Position;
        public HashSet<int> PossiblePatternIndices = new();
        public bool IsCollapsed;
        public Pattern CollapsedPattern;
        public float Entropy;

        public Cell(int maxPatternCount)
        {
            for (int i = 0; i < maxPatternCount; i++)
            {
                PossiblePatternIndices.Add(i);
            }
        }

        public void CalculateEntropy(PatternManager pm)
        {
            float frequencyLogSum = 0.0f;

            foreach (int possibleIndex in PossiblePatternIndices)
            {
                frequencyLogSum += pm.GetPattern(possibleIndex).RelativeFrequencyLog;
            }

            Entropy = Pattern.TotalFrequencyLog - (frequencyLogSum / Pattern.TotalFrequency);
        }

        public void Collapse(PatternManager pm)
        {
            if (PossiblePatternIndices.Count == 0)
            {
                IsCollapsed = true;
                return;
            }

            float sum = 0.0f;
            float partialSum = 0.0f;
            int collapsedPatternIndex = 0;

            var availablePatterns = new List<Pattern>();

            foreach (var pattern in PossiblePatternIndices.Select(pm.GetPattern))
            {
                availablePatterns.Add(pattern);
                sum += pattern.Frequency;
            }

            sum *= Random.Range(0.0f, 1.0f);
            availablePatterns.Sort((p1, p2) => p1.Frequency.CompareTo(p2.Frequency));

            foreach (var availablePattern in availablePatterns)
            {
                partialSum += availablePattern.Frequency;

                if (!(partialSum >= sum)) continue;
                
                collapsedPatternIndex = availablePattern.Index;
                break;
            }

            PossiblePatternIndices.Clear();
            PossiblePatternIndices.Add(collapsedPatternIndex);
            CollapsedPattern = pm.GetPattern(collapsedPatternIndex);
            IsCollapsed = true;
        }
    }

    public class PropagateData
    {
        public Pattern.eDirection Dir;
        public Vector2Int PropagatePosition;
        public Cell PreviousCell;
    }

    public class WaveFunctionCollapse
    {
        private readonly Cell[,] _outputCells;
        private readonly List<Cell> _outputCellList = new();
        private readonly List<PropagateData> _willPropagate = new();

        public readonly List<Cell> GeneratedCells = new();
        private readonly PatternManager _patternManager;

        public WaveFunctionCollapse(PatternManager patternManager, Vector2Int size, int maxPatternCount)
        {
            _patternManager = patternManager;

            _outputCells = new Cell[size.x, size.y];

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var cell = new Cell(maxPatternCount)
                    {
                        Position = new Vector2Int(x, y)
                    };
                    _outputCells[x, y] = cell;
                    _outputCellList.Add(cell);
                    cell.CalculateEntropy(patternManager);
                }
            }
        }

        private Cell Observe()
        {
            float minEntropy = float.PositiveInfinity;
            Cell observedCell = null;

            foreach (var outputCell in _outputCellList.Where(outputCell => outputCell.Entropy <= minEntropy))
            {
                minEntropy = outputCell.Entropy;
                observedCell = outputCell;
            }

            return observedCell;
        }

        private void Propagate(PropagateData data)
        {
            _willPropagate.Remove(data);

            if (data.PropagatePosition.x < 0)
            {
                data.PropagatePosition.x += _outputCells.GetLength(0);
            }

            if (data.PropagatePosition.x >= _outputCells.GetLength(0))
            {
                data.PropagatePosition.x -= _outputCells.GetLength(0);
            }

            if (data.PropagatePosition.y < 0)
            {
                data.PropagatePosition.y += _outputCells.GetLength(1);
            }

            if (data.PropagatePosition.y >= _outputCells.GetLength(1))
            {
                data.PropagatePosition.y -= _outputCells.GetLength(1);
            }

            Cell willCollapseCell = _outputCells[data.PropagatePosition.x, data.PropagatePosition.y];

            if (willCollapseCell.IsCollapsed)
            {
                return;
            }

            var neighborPossibleIndices = new HashSet<int>();

            foreach (var patternIndex in data.PreviousCell.PossiblePatternIndices)
            {
                var pattern = _patternManager.GetPattern(patternIndex);

                neighborPossibleIndices.UnionWith(pattern.NeighbourList[Pattern.GetOpponent(data.Dir)]);
            }

            neighborPossibleIndices.IntersectWith(willCollapseCell.PossiblePatternIndices);

            if (willCollapseCell.PossiblePatternIndices.Count > neighborPossibleIndices.Count)
            {
                AddPropagateNeighbor(willCollapseCell);
            }

            if (willCollapseCell.PossiblePatternIndices.Count == 1)
            {
                CollapseCell(willCollapseCell);
            }

            willCollapseCell.PossiblePatternIndices = neighborPossibleIndices;
            willCollapseCell.CalculateEntropy(_patternManager);
        }

        private void AddPropagateNeighbor(Cell previousCell)
        {
            _willPropagate.Add(new PropagateData()
            {
                Dir = Pattern.eDirection.eUp, PropagatePosition = previousCell.Position + Vector2Int.up,
                PreviousCell = previousCell
            });
            _willPropagate.Add(new PropagateData()
            {
                Dir = Pattern.eDirection.eDown, PropagatePosition = previousCell.Position + Vector2Int.down,
                PreviousCell = previousCell
            });
            _willPropagate.Add(new PropagateData()
            {
                Dir = Pattern.eDirection.eLeft, PropagatePosition = previousCell.Position + Vector2Int.left,
                PreviousCell = previousCell
            });
            _willPropagate.Add(new PropagateData()
            {
                Dir = Pattern.eDirection.eRight, PropagatePosition = previousCell.Position + Vector2Int.right,
                PreviousCell = previousCell
            });
        }

        public Cell PropagationStep()
        {
            GeneratedCells.Clear();

            var returnCell = Observe();

            CollapseCell(returnCell);

            while (_willPropagate.Count != 0)
            {
                Propagate(_willPropagate[0]);
            }

            return returnCell;
        }

        private void CollapseCell(Cell cell)
        {
            _outputCellList.Remove(cell);

            if (cell.IsCollapsed)
            {
                return;
            }

            if (cell.PossiblePatternIndices.Count == 0)
            {
                cell.IsCollapsed = true;
                return;
            }

            cell.Collapse(_patternManager);

            AddPropagateNeighbor(cell);

            GeneratedCells.Add(cell);
        }

        public bool IsCollapseComplete()
        {
            return _outputCellList.Count == 0;
        }
    }
}



