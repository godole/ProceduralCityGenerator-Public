using UnityEngine;

namespace Utility
{
    public static class ExVector
    {
        static float _epsilon = 9.99999944E-11f;
        public static bool RefEquals(this Vector3 v1, Vector3 v2)
        {
            return Vector3.SqrMagnitude(v1 - v2) < Vector3.kEpsilon;
        }

        public static bool IsValid(this Vector3 v)
        {
            return v.x.IsValid() && v.y.IsValid() && v.z.IsValid();
        }

        public static bool IsValid(this float f)
        {
            return !float.IsNaN(f) && float.IsFinite(f);
        }

        public static bool RefEquals(this float f, float other)
        {
            return Mathf.Abs(f - other) <= _epsilon;
        }
    }
}
