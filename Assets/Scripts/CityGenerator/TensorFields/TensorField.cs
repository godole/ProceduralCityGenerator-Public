using UnityEngine;

namespace CityGenerator.TensorFields
{
    public class TensorField
    {
        public static float GetGaussianKernel(float gamma, Vector3 center, Vector3 pos)
        {
            float distance = Vector3.Distance(pos, new Vector3(center.x, 0.0f, center.z));
            return Mathf.Exp(-gamma * distance * distance);
        }
    
        public Vector3 MajorEigenVector;
        public Vector3 MinorEigenVector;

        public readonly double[,] Values = new double[2,2];

        private double _eigenValue1;
        private double _eigenValue2;

        //행렬 고유값 구하기
        //https://gaussian37.github.io/math-la-2_by_2_eigen/
        public void CalculateEigen()
        {
            double d = Values[0, 0] * Values[1, 1] - Values[0, 1] * Values[1, 0];
            _eigenValue1 = System.Math.Sqrt(-d);
            _eigenValue2 = -_eigenValue1;

            if (Mathf.Abs((float)Values[1, 0]) <= 0.00001f)
            {
                if (Mathf.Abs((float)Values[0, 1]) <= 0.00001f)
                {
                    MajorEigenVector = new Vector3(1.0f, 0.0f, 0.0f);
                    MinorEigenVector = new Vector3(0.0f, 0.0f, 1.0f);
                }
                else
                {
                    MajorEigenVector = new Vector3((float)Values[0, 1], 0.0f, (float)(_eigenValue1 - Values[0,0]));
                    MajorEigenVector.Normalize();
                    MinorEigenVector = new Vector3((float)Values[0, 1], 0.0f, (float)(_eigenValue2 - Values[0,0]));
                    MinorEigenVector.Normalize();
                }
            }
            else
            {
                MajorEigenVector = new Vector3((float)(_eigenValue1 - Values[1,1]), 0.0f, (float)Values[1,0]);
                MajorEigenVector.Normalize();
                MinorEigenVector = new Vector3((float)(_eigenValue2 - Values[1,1]), 0.0f, (float)Values[1,0]);
                MinorEigenVector.Normalize();
            }
        }
    }

    public class TensorFieldData : ScriptableObject
    {
        protected readonly TensorField TensorField = new();

        public double[,] Values => TensorField.Values;
    
        public virtual void CalculateTensor(Vector3 pos)
        {
        
        }
    }
}