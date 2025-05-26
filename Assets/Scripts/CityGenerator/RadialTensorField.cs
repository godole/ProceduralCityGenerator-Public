using UnityEngine;

[CreateAssetMenu(fileName = "RadialTensorField", menuName = "LoadGeneration/RadialTensorField")]
public class RadialTensorField : TensorFieldData
{
    public Vector3 Center;
    public float Gamma;

    public override void CalculateTensor(Vector3 pos, bool isMajor)
    {
        Vector3 p = pos - Center;

        double y2MinusX2 = p.z * p.z - p.x * p.x;
        double minus2xy = -2 * p.x * p.z;
            
        _tensorField.Values[0, 0] = y2MinusX2 * TensorField.GetGaussianKernel(Gamma, Center, pos);
        _tensorField.Values[0, 1] = minus2xy * TensorField.GetGaussianKernel(Gamma, Center, pos);
        _tensorField.Values[1, 0] = minus2xy * TensorField.GetGaussianKernel(Gamma, Center, pos);
        _tensorField.Values[1, 1] = -y2MinusX2 * TensorField.GetGaussianKernel(Gamma, Center, pos);
    }

    public override Vector3 Sampling(Vector3 pos, bool isMajor)
    {
        CalculateTensor(pos, isMajor);

        _tensorField.CalculateEigen();

        return (isMajor ? _tensorField.majorEigenVector : _tensorField.minorEigenVector) ;
    }
}
