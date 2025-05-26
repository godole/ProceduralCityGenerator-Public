using System.Collections;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public class WaveFunctionTester : MonoBehaviour
    {
        [Header("Input Data")] 
        [SerializeField] private MeshRenderer inputPreviewObject;
        [SerializeField] private WfcInputData inputData;
        
        [Header("Output Data")]
        [SerializeField] private Vector2Int _outputSize;
        [SerializeField] private MeshRenderer _outputTexture;
        
        private int[,] _childCells;
        
        private GameObject _viewerParent;
        private PatternManager _patternManager;
        private WaveFunctionCollapse _waveFunctionCollapse;
    
        private void Start()
        {
            inputPreviewObject.sharedMaterial.mainTexture = inputData.InputTexture;
            Pattern.Size = inputData.SamplingSize;
            _patternManager = new PatternManager();
            
            Color[,] inputPixels = new Color[inputData.InputTexture.width, inputData.InputTexture.height];
    
            for (int x = 0; x < inputPixels.GetLength(0); x++)
            {
                for (int y = 0; y < inputPixels.GetLength(1); y++)
                {
                    inputPixels[x, y] = inputData.InputTexture.GetPixel(x, y);
                }
            }
    
            _patternManager.ReadPattern(inputPixels, inputData.SamplingSize, inputData.UseFreeRotate);
    
            _waveFunctionCollapse = new WaveFunctionCollapse(_patternManager, _outputSize, _patternManager.GetAllPatterns().Count);
            
            StartCoroutine(PropagateStep());
        }
    
        private IEnumerator PropagateStep()
        {
            var texture = new Texture2D(_outputSize.x, _outputSize.y);
            _outputTexture.sharedMaterial.mainTexture = texture;
            
            while (!_waveFunctionCollapse.IsCollapseComplete())
            {
                _waveFunctionCollapse.PropagationStep();
    
                foreach (var cell in _waveFunctionCollapse.GeneratedCells)
                {
                    texture.SetPixel(cell.Position.x, cell.Position.y, _patternManager.ColorList[cell.CollapsedPattern.SuperPositionIndex]);
                    texture.Apply();
                }
                
                yield return null;    
            }
        }
    }
}

