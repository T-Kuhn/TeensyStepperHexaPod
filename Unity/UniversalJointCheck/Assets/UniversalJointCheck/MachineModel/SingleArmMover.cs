using IK;
using UnityEngine;

public class SingleArmMover : MonoBehaviour
{
    [SerializeField] private Transform _container;

    [SerializeField] private Transform _target;
    [SerializeField] private Transform _joint1;
    [SerializeField] private Transform _joint2;
    [SerializeField] private Transform _joint3;

    [SerializeField] private Transform _joint1Tip;

    private Vector3 _dir;
    private Vector3 _joint2Dir;
    private Vector3 _joint2Dir2;

    void Update()
    {
        var ikResult = SphereCircleIntersectIK.Solve(
            sphereCenter: _target.position,
            circleCenter: Vector3.zero,
            sphereRadius: 0.124f,
            circleRadius: 0.112f);

        if (ikResult.Success)
        {
            var intersectionPoint = ikResult.P1;

            var theta = Mathf.Atan2(intersectionPoint.y, intersectionPoint.x) * Mathf.Rad2Deg;
            _joint1.localRotation = Quaternion.Euler(theta, 0f, 0f);

            // Joint2: X Rot
            // Joint3: Y Rot
            var diff = _target.position - intersectionPoint;

            // Quaternion.FromToRotation(Vector3.right)
            // _joint2.localRotation = Quaternion.Euler(Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg, 0f, 0f);
            // _joint3.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg, 0f);

            var joint1TipWorldPos = _joint1Tip.position;
            var containerLocalJoint1TipPos = _container.InverseTransformPoint(joint1TipWorldPos);
            _joint2.localPosition = containerLocalJoint1TipPos;
            var dir = diff.normalized;
            _dir = dir;

            var containerLocalDir = _container.InverseTransformDirection(dir);
            var containerLocalDirInXPlaneOnly = new Vector3(0f, containerLocalDir.y, containerLocalDir.z).normalized;
            var containerLocalRight = _container.InverseTransformDirection(Vector3.right);
            var angle = Vector3.SignedAngle(containerLocalRight, containerLocalDirInXPlaneOnly, Vector3.forward);
            _joint2.localRotation = Quaternion.Euler(angle, 0f, 0f);

            _joint2Dir = -_joint2.forward;
            _joint2Dir2 = _joint2.up;
            var containerLocalx = _container.InverseTransformDirection(-_joint2.forward);
            var angle2 = Vector3.SignedAngle(containerLocalx, containerLocalDir, _joint2.up);
            _joint3.localRotation = Quaternion.Euler(0f, angle2, 0f);
            // TODO: Something is wrong with this.
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        var origin = _joint2.position;
        var target = origin + _dir * 0.1f;
        Gizmos.DrawLine(origin, target);

        Gizmos.color = Color.blue;
        var origin2 = _joint2.position;
        var target2 = origin2 + Vector3.right * 0.1f;
        Gizmos.DrawLine(origin2, target2);
        
        Gizmos.color = Color.red;
        var origin3 = _joint2.position;
        var target3 = origin3 + _joint2Dir * 0.1f;
        Gizmos.DrawLine(origin3, target3);
        
        Gizmos.color = Color.yellow;
        var origin4 = _joint2.position;
        var target4 = origin4 + _joint2Dir2 * 0.1f;
        Gizmos.DrawLine(origin4, target4);
    }
}