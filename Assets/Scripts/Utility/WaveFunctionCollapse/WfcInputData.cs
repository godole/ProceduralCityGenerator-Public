using UnityEngine;

namespace WaveFunctionCollapse
{
    [CreateAssetMenu(fileName = "WfcInputData", menuName = "WaveFunctionCollapse/WfcInputData")]
    public class WfcInputData : ScriptableObject
    {
        public Texture2D InputTexture;
        public int SamplingSize;
        public bool UseFreeRotate;
    }
}
