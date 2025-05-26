using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LinearTensorField", menuName = "LoadGeneration/LinearTensorField")]
public class LinearTensorField : TensorFieldData
{
    public float R;
    public float Theta;
    public float Gamma;
    public Vector3 Center;

    public override void CalculateTensor(Vector3 pos, bool isMajor)
    {
        float rCos2Theta = R * Mathf.Cos(2.0f * Theta);
        float rSin2Theta = R * Mathf.Sin(2.0f * Theta);
            
        _tensorField.Values[0, 0] = rCos2Theta* TensorField.GetGaussianKernel(Gamma, Center, pos);
        _tensorField.Values[0, 1] = rSin2Theta* TensorField.GetGaussianKernel(Gamma, Center, pos);
        _tensorField.Values[1, 0] = rSin2Theta* TensorField.GetGaussianKernel(Gamma, Center, pos);
        _tensorField.Values[1, 1] = -rCos2Theta* TensorField.GetGaussianKernel(Gamma, Center, pos);
    }

    public override Vector3 Sampling(Vector3 pos, bool isMajor)
    {
        CalculateTensor(pos, isMajor);

        _tensorField.CalculateEigen();

        return (isMajor ? _tensorField.majorEigenVector : _tensorField.minorEigenVector) ;
    }
}