using UnityEngine;

namespace CityGenerator.TensorFields
{
    [CreateAssetMenu(fileName = "RadialTensorField", menuName = "LoadGeneration/RadialTensorField")]
    public class RadialTensorField : TensorFieldData
    {
        public Vector3 Center;
        public float Gamma;

        public override void CalculateTensor(Vector3 pos)
        {
            Vector3 p = pos - Center;

            double y2MinusX2 = p.z * p.z - p.x * p.x;
            double minus2XY = -2 * p.x * p.z;
            
            TensorField.Values[0, 0] = y2MinusX2 * TensorField.GetGaussianKernel(Gamma, Center, pos);
            TensorField.Values[0, 1] = minus2XY * TensorField.GetGaussianKernel(Gamma, Center, pos);
            TensorField.Values[1, 0] = minus2XY * TensorField.GetGaussianKernel(Gamma, Center, pos);
            TensorField.Values[1, 1] = -y2MinusX2 * TensorField.GetGaussianKernel(Gamma, Center, pos);
        }
    }
}
