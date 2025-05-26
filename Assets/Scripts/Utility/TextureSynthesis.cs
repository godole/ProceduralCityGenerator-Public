using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    public class TextureSynthesis : MonoBehaviour
    {
        class Patch
        {
            public Vector2Int Origin;
            public static int Size = 16;

            public Vector2Int Min { get => _min; }
            public Vector2Int Max { get => _max; }

            Vector2Int _min;
            Vector2Int _max;

            public Patch(Vector2Int origin)
            {
                Origin = origin;

                int halfSize = Size / 2;

                _min = new Vector2Int(origin.x - halfSize, origin.y - halfSize);
                _max = new Vector2Int(origin.x + halfSize, origin.y + halfSize);
            }

            public Vector2Int GetiterationPosition(int x, int y)
            {
                Vector2Int itrPos = Vector2Int.zero;

                itrPos.x = (Size + x) % Size;
                itrPos.y = (Size + y) % Size;

                return itrPos;
            }
        }

        class OutputPixel
        {
            public Color color;
            public Vector2Int Pos;
            public double error;
        }

        [SerializeField] GameObject _MatchDebugObject2;
        [SerializeField] Texture2D _inputTexture;
        [SerializeField] Vector2Int _outputSize;
        [SerializeField] MeshRenderer _outputRenderer;

        Color[,] _inputTexturePixels;
        double[,] _gaussianKernel;

        Texture2D _outputTexture;
        List<Vector2Int> _filledPixels = new List<Vector2Int>();

        // Start is called before the first frame update
        void Start()
        {
            _inputTexturePixels = new Color[_inputTexture.width, _inputTexture.height];
            _gaussianKernel = CalculateGaussianKernel(Patch.Size + 1, 2.0f);
            List<Vector2Int> remainPixelPositionList = new List<Vector2Int>();
            _outputTexture = new Texture2D(_outputSize.x, _outputSize.y);

            for(int x = 0; x < _inputTexture.width; x++)
            {
                for(int y = 0; y < _inputTexture.height; y++)
                {
                    _inputTexturePixels[x, y] = _inputTexture.GetPixel(x, y);
                }
            }

            //초기 아웃풋 설정
            Vector2Int startPixelPosition = new Vector2Int(Random.Range(0, _inputTexture.width), Random.Range(0, _inputTexture.height));
            Vector2Int center = new Vector2Int(_outputSize.x / 2, _outputSize.y / 2);

            for(int x = 0; x < _outputSize.x; x++)
            {
                for(int y= 0; y < _outputSize.y; y++)
                {
                    _outputTexture.SetPixel(x, y, Color.white);
                }
            }

        

            for(int x = -Patch.Size / 2; x < Patch.Size / 2; x++)
            {
                for (int y = -Patch.Size / 2; y < Patch.Size / 2; y++)
                {
                    int ix = (x + startPixelPosition.x + _inputTexture.width) % _inputTexture.width;
                    int iy = (y + startPixelPosition.y + _inputTexture.height) % _inputTexture.height;
                    _outputTexture.SetPixel(x + center.x, y + center.y, _inputTexturePixels[ix, iy]);

                    _removeAndAddRemainPosition(new Vector2Int(x + center.x, y + center.y));
                }
            }

            _outputTexture.Apply();

            _outputRenderer.material.mainTexture = _outputTexture;

            void _removeAndAddRemainPosition(Vector2Int pos)
            {
                remainPixelPositionList.Remove(pos);
                _filledPixels.Add(pos);

                if (pos.x + 1 < _outputSize.x)
                {
                    _addInclusiveRemainPosition(pos + Vector2Int.right);
                }
                if(pos.x - 1 >= 0)
                {
                    _addInclusiveRemainPosition(pos + Vector2Int.left);
                }
                if (pos.y + 1 < _outputSize.y)
                {
                    _addInclusiveRemainPosition(pos + Vector2Int.up);
                }
                if (pos.y - 1 >= 0)
                {
                    _addInclusiveRemainPosition(pos + Vector2Int.down);
                }
            }

            void _addInclusiveRemainPosition(Vector2Int pos)
            {
                if(_filledPixels.Contains(pos))
                {
                    return;
                }
                if(!remainPixelPositionList.Contains(pos))
                {
                    remainPixelPositionList.Add(pos);
                }
            }

            StartCoroutine(_Process(remainPixelPositionList));
        }

        IEnumerator _Process(List<Vector2Int> remainPixelPositionList)
        {
            int iterator = 0;
            while (remainPixelPositionList.Count != 0)
            {
                if(!_outputTexture.GetPixel(remainPixelPositionList[iterator].x, remainPixelPositionList[iterator].y).Equals(Color.white))
                {
                    iterator++;
                    continue;
                }

                Patch template = GetNeighporhoodWindow(remainPixelPositionList[iterator]);
                var bestMatches = FindMatches(template);
            
                var randomMatch = bestMatches[Random.Range(0, bestMatches.Count)];

                //if (randomMatch.error < MaxErrorThreshold)
                //{
                
                //}

                _outputTexture.SetPixel(remainPixelPositionList[iterator].x, remainPixelPositionList[iterator].y, randomMatch.color);
                _removeAndAddRemainPosition(remainPixelPositionList[iterator]);

                _outputTexture.Apply();

                _outputRenderer.material.mainTexture = _outputTexture;

                iterator = 0;

                yield return null;
            }

            void _removeAndAddRemainPosition(Vector2Int pos)
            {
                remainPixelPositionList.Remove(pos);
                _filledPixels.Add(pos);

                if (pos.x + 1 <= _outputSize.x)
                {
                    _addInclusiveRemainPosition(pos + Vector2Int.right);
                }
                if (pos.x - 1 >= 0)
                {
                    _addInclusiveRemainPosition(pos + Vector2Int.left);
                }
                if (pos.y + 1 <= _outputSize.y)
                {
                    _addInclusiveRemainPosition(pos + Vector2Int.up);
                }
                if (pos.y - 1 >= 0)
                {
                    _addInclusiveRemainPosition(pos + Vector2Int.down);
                }
            }

            void _addInclusiveRemainPosition(Vector2Int pos)
            {
                if (!remainPixelPositionList.Contains(pos))
                {
                    remainPixelPositionList.Add(pos);
                }
            }
        }
    
        Patch GetNeighporhoodWindow(Vector2Int pos)
        {
            Patch neighboorWindow = new Patch(new Vector2Int(pos.x - Patch.Size / 2, pos.y - Patch.Size / 2));
            //_MatchDebugObject2.transform.position = new Vector3(neighboorWindow.Origin.x + (Patch.Size / 2) - 128, 0.0f, neighboorWindow.Origin.y + (Patch.Size / 2) - 64);

            return neighboorWindow;
        }

        List<OutputPixel> FindMatches(Patch template)
        {
            double[,] ssd = new double[_inputTexture.width, _inputTexture.height];

            double minssd = double.PositiveInfinity;

            for (int x = Patch.Size / 2; x < _inputTexture.width - Patch.Size / 2; x++)
            {
                for (int y = Patch.Size / 2; y < _inputTexture.height - Patch.Size / 2; y++)
                {
                    Patch samplePatch = new Patch(new Vector2Int(x - Patch.Size / 2, y - Patch.Size / 2));

                    ssd[x, y] = GetColorDifference(samplePatch, template);

                    if(ssd[x, y] == 0)
                    {
                        continue;
                    }

                    if (minssd > ssd[x, y])
                    {
                        minssd = ssd[x, y];
                    }
                }
            }

            List<OutputPixel> pixelList = new List<OutputPixel>();

            for (int x = 0; x < _inputTexture.width; x++)
            {
                for (int y = 0; y < _inputTexture.height; y++)
                {
                    if(ssd[x, y] == 0)
                    {
                        continue;
                    }

                    if(ssd[x, y] <= (minssd * 1.1f))
                    {
                        OutputPixel op = new OutputPixel();
                        op.Pos = new Vector2Int(x, y);
                        op.color = _inputTexturePixels[x, y];
                        op.error = ssd[x, y];
                        pixelList.Add(op);
                    }
                }
            }

            pixelList.Sort((p1, p2) => { return p1.error.CompareTo(p2.error); });

            return pixelList;
        }

        double GetColorDifference(Patch samplePatch, Patch outputPatch)
        {
            double difference = 0;
            float matchCount = 0;

            for(int x = 0; x < Patch.Size; x++)
            {
                for(int y = 0; y < Patch.Size; y++)
                {
                    Color sampleTextureColor = _inputTexture.GetPixel(x + samplePatch.Origin.x, y + samplePatch.Origin.y);
                    Color outputTextureColor = _outputTexture.GetPixel(x + outputPatch.Origin.x, y + outputPatch.Origin.y);

                    if(outputTextureColor.Equals(Color.white))
                    {
                        continue;
                    }

                    double difR = (sampleTextureColor.r * 255) - (outputTextureColor.r * 255);
                    double difG = (sampleTextureColor.g * 255) - (outputTextureColor.g * 255);
                    double difB = (sampleTextureColor.b * 255) - (outputTextureColor.b * 255);

                    matchCount++;

                    difference = difference + ((difR * difR + difG * difG + difB * difB) * _gaussianKernel[x, y]);
                }
            }

            difference /= (Patch.Size * Patch.Size);

            return difference;
        }

        double GetColorLuminance(Color color)
        {
            return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
        }


        /// <summary>
        /// 가우시안 커널 계산하기
        /// 구글링해서 들고옴
        /// </summary>
        /// <param name="length">길이</param>
        /// <param name="weight">가중치</param>
        /// <returns>가우시안 커널 배열</returns>
        public static double[,] CalculateGaussianKernel(int length, float weight)
        {
            double[,] filterArray = new double[length, length];
            double totalSummary = 0;
            int kernelRadius = length / 2;
            double distance = 0;
            double eulerCalculated = 1.0 / (2.0 * System.Math.PI * Mathf.Pow(weight, 2));
            int ix = 0, iy = 0;

            try
            {
                for (int filterY = -kernelRadius; filterY <= kernelRadius; filterY++)
                {
                    for (int filterX = -kernelRadius; filterX <= kernelRadius; filterX++)
                    {
                        ix = filterY + kernelRadius;
                        iy = filterX + kernelRadius;

                        distance = ((filterX * filterX) + (filterY * filterY)) / (2 * (weight * weight));

                        filterArray[filterY + kernelRadius, filterX + kernelRadius] = eulerCalculated * Mathf.Exp(-(float)distance);

                        totalSummary += filterArray[filterY + kernelRadius, filterX + kernelRadius];
                    }
                }
            }
            catch(System.IndexOutOfRangeException)
            {
                Debug.Log($"X : {ix}, Y : {iy}");
            }
        

            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < length; x++)
                {
                    filterArray[y, x] = filterArray[y, x] * (1.0 / totalSummary);
                }
            }

            return filterArray;
        }
    }
}
