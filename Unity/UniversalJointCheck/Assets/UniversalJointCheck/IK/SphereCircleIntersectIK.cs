using UnityEngine;

namespace IK
{
    public static class SphereCircleIntersectIK
    {
        public static IkResult Solve(
            Vector3 sphereCenter,
            Vector3 circleCenter,
            float sphereRadius,
            float circleRadius)
        {
            // z_0
            var zPlane = circleCenter.z;

            // rs_proj
            var sphereRadiusProjectedSquared = sphereRadius * sphereRadius - (zPlane - sphereCenter.z) * (zPlane - sphereCenter.z);

            // A, B, C
            var bigA = 2f * (sphereCenter.x - circleCenter.x);
            var bigB = 2f * (sphereCenter.y - circleCenter.y);
            var bigC = circleRadius * circleRadius
                       - sphereRadiusProjectedSquared
                       - circleCenter.x * circleCenter.x
                       - circleCenter.y * circleCenter.y
                       + sphereCenter.x * sphereCenter.x
                       + sphereCenter.y * sphereCenter.y;

            // a, b, c
            var a = 1 + bigA * bigA / (bigB * bigB);
            var b = -2f * circleCenter.x - 2f * bigA * bigC / (bigB * bigB) + 2f * bigA * circleCenter.y / bigB;
            var c = circleCenter.x * circleCenter.x
                    + bigC * bigC / (bigB * bigB)
                    - 2f * bigC * circleCenter.y / bigB
                    + circleCenter.y * circleCenter.y
                    - circleRadius * circleRadius;

            // determinant
            var determinant = b * b - 4f * a * c;

            if (determinant < 0) return new IkResult { Success = false, P1 = Vector3.zero, P2 = Vector3.zero };

            // x_0, x_1
            var x0 = (-b + Mathf.Sqrt(determinant)) / (2f * a);
            var x1 = (-b - Mathf.Sqrt(determinant)) / (2f * a);

            // y_0, y_1
            var y0 = (bigC - bigA * x0) / bigB;
            var y1 = (bigC - bigA * x1) / bigB;
            
            return new IkResult
            {
                Success = true,
                P1 = new Vector3(x0, y0, zPlane),
                P2 = new Vector3(x1, y1, zPlane)
            };
        }
    }

    public struct IkResult
    {
        public Vector3 P1;
        public Vector3 P2;
        public bool Success;
    }
}