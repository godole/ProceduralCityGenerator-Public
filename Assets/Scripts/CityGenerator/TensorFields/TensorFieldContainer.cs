using System.Collections;
using System.Collections.Generic;
using CityGenerator.TensorFields;
using UnityEngine;

public class TensorFieldContainer
{
    TensorField _tensorField = new TensorField();
    
    private List<TensorFieldData> _tensorFields = new List<TensorFieldData>();

    public void AddTensorField(TensorFieldData tensorField)
    {
        _tensorFields.Add(tensorField);
    }
    
    public Vector3 GetTensorSampling(Vector3 pos, bool isMajor)
    {
        _tensorField.Values[0, 0] = 0;
        _tensorField.Values[1, 0] = 0;
        _tensorField.Values[0, 1] = 0;
        _tensorField.Values[1, 1] = 0;

        foreach (var tensorField in _tensorFields)
        {
            tensorField.CalculateTensor(pos);

            for (int i = 0; i < _tensorField.Values.GetLength(0); i++)
            {
                for (int j = 0; j < _tensorField.Values.GetLength(1); j++)
                {
                    _tensorField.Values[i, j] += tensorField.Values[i, j];
                }
            }
        }
        
        _tensorField.CalculateEigen();

        return (isMajor ? _tensorField.MajorEigenVector : _tensorField.MinorEigenVector) ;;
    }

    public Vector3 GetTensorSampling(Vector3 pos, Vector3 prevDir, bool isMajor)
    {
        Vector3 ret = GetTensorSampling(pos, isMajor);

        return (Vector3.Dot(prevDir, ret) > 0 ? ret : -ret);
    }

    public Vector3 GetRungeKuttaSampling(Vector3 pos, Vector3 prevDir, bool isReverse, bool isMajor, float h = 0.5f)
    {
        float reverse = isReverse ? -1.0f : 1.0f;
            
        var k1 = h * GetTensorSampling(pos, prevDir, isMajor) ;
        var k2 = h * GetTensorSampling(k1 / 2.0f + pos, prevDir, isMajor);
        var k3 = h * GetTensorSampling(k2 / 2.0f + pos, prevDir, isMajor) ;
        var k4 = h * GetTensorSampling(k3 + pos, prevDir, isMajor);

        var res = (1.0f / 6.0f) * (k1 + 2.0f * k2 + 2.0f * k3 + k4);

        return pos + res;
    }
}
