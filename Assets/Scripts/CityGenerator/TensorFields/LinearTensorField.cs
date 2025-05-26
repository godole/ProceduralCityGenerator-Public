using UnityEngine;

namespace CityGenerator.TensorFields
{
    [CreateAssetMenu(fileName = "LinearTensorField", menuName = "LoadGeneration/LinearTensorField")]
    public class LinearTensorField : TensorFieldData
    {
        public float R;
        public float Theta;
        public float Gamma;
        public Vector3 Center;

        public override void CalculateTensor(Vector3 pos)
        {
            float rCos2Theta = R * Mathf.Cos(2.0f * Theta);
            float rSin2Theta = R * Mathf.Sin(2.0f * Theta);
            
            TensorField.Values[0, 0] = rCos2Theta* TensorField.GetGaussianKernel(Gamma, Center, pos);
            TensorField.Values[0, 1] = rSin2Theta* TensorField.GetGaussianKernel(Gamma, Center, pos);
            TensorField.Values[1, 0] = rSin2Theta* TensorField.GetGaussianKernel(Gamma, Center, pos);
            TensorField.Values[1, 1] = -rCos2Theta* TensorField.GetGaussianKernel(Gamma, Center, pos);
        }
    }
}