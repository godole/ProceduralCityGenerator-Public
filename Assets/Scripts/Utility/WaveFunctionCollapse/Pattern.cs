using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility.WaveFunctionCollapse
{
    public class Pattern
    {
        public enum eDirection
        {
            eUp,
            eDown,
            eLeft,
            eRight
        }

        public int Index;
        private readonly int[,] _grid;
        public int SuperPositionIndex => _grid[0, 0];
        public float Frequency = 1.0f;
        private float _relativeFrequency;
        public float RelativeFrequencyLog;
        public readonly Dictionary<eDirection, List<int>> NeighbourList = new();

        public static float TotalFrequency;
        public static float TotalFrequencyLog;
        public static int Size;
        
        public static long Hash(int[,] grid)
        {
            long simpleHash = 0;
            int index = 0;

            for (int y = 0; y < grid.GetLength(1); y++)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    simpleHash += grid[x, y] * (1 << index);
                    index++;
                }
            }

            return simpleHash;
        }

        public static int[,] RotateGrid(int[,] grid)
        {
            int n = grid.GetLength(0);
            int[,] rotatedGrid = new int[n, n];

            if (n % 2 == 1)
            {
                rotatedGrid[n / 2, n / 2] = grid[n / 2, n / 2];
            }

            for (int x = 0; x < n / 2; x++)
            {
                for (int y = x; y < n - x - 1; y++)
                {
                    rotatedGrid[x, y] = grid[y, n - 1 - x];
                    rotatedGrid[y, n - 1 - x] = grid[n - 1 - x, n - 1 - y];
                    rotatedGrid[n - 1 - x, n - 1 - y] = grid[n - 1 - y, x];
                    rotatedGrid[n - 1 - y, x] = grid[x, y];
                }
            }

            return rotatedGrid;
        }

        public static int[,] ReflectXGrid(int[,] grid)
        {
            int xMax = grid.GetLength(0);
            int yMax = grid.GetLength(1);

            int[,] reflectedGrid = new int[xMax, yMax];

            for (int x = 0; x < xMax; x++)
            {
                for (int y = 0; y < yMax; y++)
                {
                    reflectedGrid[x, y] = grid[xMax - 1 - x, y];
                }
            }

            return reflectedGrid;
        }

        public static int[,] ReflectYGrid(int[,] grid)
        {
            int xMax = grid.GetLength(0);
            int yMax = grid.GetLength(1);

            int[,] reflectedGrid = new int[xMax, yMax];

            for (int x = 0; x < xMax; x++)
            {
                for (int y = 0; y < yMax; y++)
                {
                    reflectedGrid[x, y] = grid[x, yMax - 1 - y];
                }
            }

            return reflectedGrid;
        }

        public Pattern(int[,] grid)
        {
            _grid = grid;

            foreach (eDirection dir in Enum.GetValues(typeof(eDirection)))
            {
                NeighbourList[dir] = new List<int>();
            }
        }

        public void CalculateFrequency()
        {
            _relativeFrequency = Frequency / TotalFrequency;
            RelativeFrequencyLog = Mathf.Log(2.0f, _relativeFrequency);
        }

        public void FindNeighbours(List<Pattern> others)
        {
            foreach (var (dir, neighbour) in NeighbourList)
            {
                foreach (var another in others.Where(another => GetEqualsAnother(dir, another)))
                {
                    neighbour.Add(another.Index);
                }
            }
        }

        private bool GetEqualsAnother(eDirection dir, Pattern another)
        {
            int[,] mySideGrid = GetSideGrid(dir);
            int[,] anotherSideGrid = another.GetSideGrid(GetOpponent(dir));

            for (int x = 0; x < mySideGrid.GetLength(0); x++)
            {
                for (int y = 0; y < mySideGrid.GetLength(1); y++)
                {
                    if (!mySideGrid[x, y].Equals(anotherSideGrid[x, y]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private int[,] GetSideGrid(eDirection dir)
        {
            int[,] sideGrid = dir is eDirection.eUp or eDirection.eDown ? new int[Size, Size - 1] : new int[Size - 1, Size];

            int minX;
            int maxX;
            int minY;
            int maxY;

            switch (dir)
            {
                case eDirection.eUp:
                    minX = 0;
                    maxX = Size;
                    minY = 1;
                    maxY = Size;
                    break;
                case eDirection.eDown:
                    minX = 0;
                    maxX = Size;
                    minY = 0;
                    maxY = Size - 1;
                    break;
                case eDirection.eLeft:
                    minX = 0;
                    maxX = Size - 1;
                    minY = 0;
                    maxY = Size;
                    break;
                case eDirection.eRight:
                    minX = 1;
                    maxX = Size;
                    minY = 0;
                    maxY = Size;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    sideGrid[x - minX, y - minY] = _grid[x, y];
                }
            }

            return sideGrid;
        }

        public static eDirection GetOpponent(eDirection dir)
        {
            eDirection opponent = dir switch
            {
                eDirection.eUp => eDirection.eDown,
                eDirection.eDown => eDirection.eUp,
                eDirection.eLeft => eDirection.eRight,
                eDirection.eRight => eDirection.eLeft,
                _ => eDirection.eUp
            };

            return opponent;
        }
    }
}

