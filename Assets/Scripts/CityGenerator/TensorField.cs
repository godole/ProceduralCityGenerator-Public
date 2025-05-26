using UnityEngine;
using UnityEngine.Serialization;

public class TensorField
{
    public static float GetGaussianKernel(float gamma, Vector3 center, Vector3 pos)
    {
        float distance = Vector3.Distance(pos, new Vector3(center.x, 0.0f, center.z));
        return Mathf.Exp(-gamma * distance * distance);
    }
    
    public Vector3 majorEigenVector;
    public Vector3 minorEigenVector;

    public double[,] Values = new double[2,2];

    public double eigenValue1;
    public double eigenValue2;

    public virtual Vector3 Sampling(Vector3 pos, bool isMajor)
    {
        return isMajor ? Vector3.right : Vector3.up;
    }

    public virtual void CalculateTensor(Vector3 pos, bool isMajor)
    {
        
    }

    //행렬 고유값 구하기
    //https://gaussian37.github.io/math-la-2_by_2_eigen/
    //라고 합니다 출처가 날아가서 자세히는 모?름
    public void CalculateEigen()
    {
        double d = Values[0, 0] * Values[1, 1] - Values[0, 1] * Values[1, 0];
        eigenValue1 = System.Math.Sqrt(-d);
        eigenValue2 = -eigenValue1;

        if (Mathf.Abs((float)Values[1, 0]) <= 0.00001f)
        {
            if (Mathf.Abs((float)Values[0, 1]) <= 0.00001f)
            {
                majorEigenVector = new Vector3(1.0f, 0.0f, 0.0f);
                minorEigenVector = new Vector3(0.0f, 0.0f, 1.0f);
            }
            else
            {
                majorEigenVector = new Vector3((float)Values[0, 1], 0.0f, (float)(eigenValue1 - Values[0,0]));
                majorEigenVector.Normalize();
                minorEigenVector = new Vector3((float)Values[0, 1], 0.0f, (float)(eigenValue2 - Values[0,0]));
                minorEigenVector.Normalize();
            }
        }
        else
        {
            majorEigenVector = new Vector3((float)(eigenValue1 - Values[1,1]), 0.0f, (float)Values[1,0]);
            majorEigenVector.Normalize();
            minorEigenVector = new Vector3((float)(eigenValue2 - Values[1,1]), 0.0f, (float)Values[1,0]);
            minorEigenVector.Normalize();
        }
    }
}

public class TensorFieldData : ScriptableObject
{
    protected TensorField _tensorField = new();

    public double[,] Values => _tensorField.Values;
    
    public virtual void CalculateTensor(Vector3 pos, bool isMajor)
    {
        
    }

    public virtual Vector3 Sampling(Vector3 pos, bool isMajor)
    {
        return Vector3.zero;
    }
}





[CreateAssetMenu(fileName = "LineTensorField", menuName = "LoadGeneration/LineTensorField")]
public class LineTensorField : TensorFieldData
{
    private Vector3 _startPos;
    private Vector3 _endPos;
    private float _gamma;
    private float _strength;
    private Vector3 _direction;

    public LineTensorField(Vector3 startPos, Vector3 endPos, float gamma, float strength = 2.0f)
    {
        _startPos = startPos;
        _endPos = endPos;
        _gamma = gamma;
        _strength = strength;

        _direction = (_endPos - startPos).normalized;
    }

    public override Vector3 Sampling(Vector3 pos, bool isMajor)
    {
        var linePerpendicularPos = MathUtil.GetCircleLineIntersect(_startPos, _endPos, pos, 10.0f);

        if (!linePerpendicularPos.IsIntersect)
        {
            return Vector3.zero;
        }
        
        float gaussian = TensorField.GetGaussianKernel(_gamma, linePerpendicularPos.LinePoint, pos);
        return (isMajor ? _direction : new Vector3(_direction.z, 0.0f, -_direction.x)) * gaussian * _strength;
    }
}
