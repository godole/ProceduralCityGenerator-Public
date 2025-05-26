using System.Collections.Generic;
using UnityEngine;

namespace Utility.WaveFunctionCollapse
{
    public class PatternManager
    {
        private int _patternIndex;

        private int[,] _pixels;
        private readonly Dictionary<long, Pattern> _patternHashDictionary = new();
        private readonly Dictionary<int, Pattern> _patterns = new();
        private readonly List<Pattern> _patternList = new();

        private readonly Dictionary<int, int> _colors = new();
        public readonly List<Color> ColorList = new();

        private static int ColorToInt(Color color)
        {
            float tr = color.r * 100;
            int r = (int)tr * 255 / 100;
            float tg = color.g * 100;
            int g = (int)tg * 255 / 100;
            float tb = color.b * 100;
            int b = (int)tb  * 255/ 100;

            return r << 16 | g << 8 | b;
        }

        public void ReadPattern(Color[,] pixels, int patternSize, bool useFreeRotate)
        {
            int colorStaticIndex = 0;

            _pixels = new int[pixels.GetLength(0), pixels.GetLength(1)];

            for (int y = 0; y < pixels.GetLength(1); y++)
            {
                for (int x = 0; x < pixels.GetLength(0); x++)
                {
                    int colorIndex;
                    int colorHash = ColorToInt(pixels[x, y]);
                
                    if (_colors.TryGetValue(colorHash, out int index))
                    {
                        colorIndex = index;
                    }
                    else
                    {
                        colorIndex = colorStaticIndex;
                        _colors.Add(colorHash, colorIndex);
                        ColorList.Add(pixels[x, y]);
                        colorStaticIndex++;
                    }

                    _pixels[x, y] = colorIndex;
                }
            }

            for (int y = patternSize / 2; y < _pixels.GetLength(1) - patternSize / 2; y++)
            {
                for(int x = patternSize / 2; x < _pixels.GetLength(0) - patternSize / 2; x++)
                {
                    int[,] patternGrid = GetPartPixel(x, y, patternSize);
                    ReadPattern(patternGrid);
                
                    int[,] originReflectXGrid = Pattern.ReflectXGrid(patternGrid);
                    ReadPattern(originReflectXGrid);

                    if (!useFreeRotate) continue;
                
                    int[,] rotate1Grid = Pattern.RotateGrid(patternGrid);
                    ReadPattern(rotate1Grid);
                
                    int[,] rotate2Grid = Pattern.RotateGrid(rotate1Grid);
                    ReadPattern(rotate2Grid);
                
                    int[,] rotate3Grid = Pattern.RotateGrid(rotate2Grid);
                    ReadPattern(rotate3Grid);
                    
                    int[,] originReflectYGrid = Pattern.ReflectYGrid(patternGrid);
                    ReadPattern(originReflectYGrid);
                
                    int[,] rotate1ReflectXGrid = Pattern.ReflectXGrid(rotate1Grid);
                    ReadPattern(rotate1ReflectXGrid);
                
                    int[,] rotate1ReflectYGrid = Pattern.ReflectYGrid(rotate1Grid);
                    ReadPattern(rotate1ReflectYGrid);
                }
            }

            Pattern.TotalFrequencyLog = Mathf.Log(2.0f, Pattern.TotalFrequency);

            foreach (var pattern in _patternList)
            {
                pattern.FindNeighbours(_patternList);
                pattern.CalculateFrequency();
            }
        }

        private void ReadPattern(int[,] grid)
        {
            long patternHash = Pattern.Hash(grid);

            if (!_patternHashDictionary.TryGetValue(patternHash, out var existPattern))
            {
                var pattern = new Pattern(grid)
                {
                    Index = _patternIndex
                };
                _patternHashDictionary.Add(patternHash, pattern);
                _patterns.Add(_patternIndex, pattern);
                _patternList.Add(pattern);
                _patternIndex++;
            }
            else
            {
                existPattern.Frequency += 1.0f;
            }

            Pattern.TotalFrequency += 1.0f;
        }

        public Pattern GetPattern(int index)
        {
            return _patterns[index];
        }

        public List<Pattern> GetAllPatterns()
        {
            return _patternList;
        }

        private int[,] GetPartPixel(int x, int y, int size)
        {
            int[,] part = new int[size, size];

            for(int newx = 0; newx < size; newx++)
            {
                for(int newy = 0; newy < size; newy++)
                {
                    int originFixedX = x + newx < 0 ? _pixels.GetLength(0) + x + newx : x + newx;
                    int originFixedY = y + newy < 0 ? _pixels.GetLength(1) + y + newy : y + newy;
                    int originx = originFixedX == 0 ? 0 : originFixedX % (_pixels.GetLength(0));
                    int originy = originFixedY == 0 ? 0 : originFixedY % (_pixels.GetLength(1));
                    part[newx, newy] = _pixels[originx, originy];
                }
            }

            return part;
        }
    }
}
