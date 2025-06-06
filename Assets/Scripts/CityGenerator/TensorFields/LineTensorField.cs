using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace CityGenerator.TensorFields
{
    [CreateAssetMenu(fileName = "LineTensorField", menuName = "LoadGeneration/LineTensorField")]
    public class LineTensorField : TensorFieldData
    {
        public float Gamma;
        public List<Vector3> Positions;

        public override void CalculateTensor(Vector3 pos)
        {
            TensorField.Values[0, 0] = 0;
            TensorField.Values[0, 1] = 0;
            TensorField.Values[1, 0] = 0;
            TensorField.Values[1, 1] = 0;
            
            for (int i = 0; i < Positions.Count - 1; i++)
            {
                Vector3 dir = (Positions[i + 1] - Positions[i]).normalized;
                
                var angleAxis = Mathf.Acos(Vector3.Dot(dir, Vector3.right));
                float rCos2Theta = Mathf.Cos(2.0f * angleAxis);
                float rSin2Theta = Mathf.Sin(2.0f * angleAxis);
                
                var gaussianValue = TensorField.GetGaussianKernel(Gamma, Positions[i], pos);
                
                TensorField.Values[0, 0] += rCos2Theta* gaussianValue;
                TensorField.Values[0, 1] += rSin2Theta* gaussianValue;
                TensorField.Values[1, 0] += rSin2Theta* gaussianValue;
                TensorField.Values[1, 1] += -rCos2Theta* gaussianValue;
            }
        }
    }
}
