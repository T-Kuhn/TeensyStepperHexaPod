using UnityEngine;

namespace UniversalJointCheck.Ik
{
    public class IkTest : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            var sphereCenter = new Vector3(10f, 7f, 0);
            var circleCenter = new Vector3(2f, 1f, 0);
            var sphereRadius = 5f;
            var circleRadius = 6f;

            var result = SphereCircleIntersectIK.Solve(sphereCenter, circleCenter, sphereRadius, circleRadius);

            Debug.Log(result.Success);
            Debug.Log(result.P1);
            Debug.Log(result.P2);
        }
    }
}