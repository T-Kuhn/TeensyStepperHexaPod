using UniversalJointCheck.Ik;
using UnityEngine;

namespace UniversalJointCheck.MachineModel
{
    public class SingleArmMover : MonoBehaviour
    {
        [SerializeField] private Transform _container;
        [SerializeField] private Transform _viewContainer;

        [SerializeField] private Transform _target;
        [SerializeField] private Transform _joint1;
        [SerializeField] private Transform _joint2;
        [SerializeField] private Transform _joint3;

        [SerializeField] private Transform _joint1Tip;

        [SerializeField] private bool _showJoin2Gizmos;
        [SerializeField] private bool _showJoin3Gizmos;

        private Vector3 _link2Dir;
        private Vector3 _joint2ForwardDir;
        private Vector3 _joint2UpDir;

        void Update()
        {
            //var localTarget = _container.InverseTransformPoint(_target.position);

            var ikResult = SphereCircleIntersectIK.Solve(
                sphereCenter: _target.position,
                circleCenter: Vector3.zero,
                sphereRadius: 0.124f,
                circleRadius: 0.112f);

            if (!ikResult.Success) return;

            var intersectionPoint = ikResult.P1;

            RotateJoint1(intersectionPoint);

            var link2Dir = MoveLink2ToJoint1TipAndGetLink2Dir(intersectionPoint);
            var containerLocalDir = _viewContainer.InverseTransformDirection(link2Dir);

            RotateJoint2(containerLocalDir);
            RotateJoint3(containerLocalDir);
        }

        private Vector3 MoveLink2ToJoint1TipAndGetLink2Dir(Vector3 intersectionPoint)
        {
            var joint1TipWorldPos = _joint1Tip.position;
            var containerLocalJoint1TipPos = _viewContainer.InverseTransformPoint(joint1TipWorldPos);
            _joint2.localPosition = containerLocalJoint1TipPos;
            _link2Dir = (_target.position - intersectionPoint).normalized;

            return _link2Dir;
        }

        private void RotateJoint2(Vector3 containerLocalDir)
        {
            var containerLocalDirInXPlaneOnly = new Vector3(0f, containerLocalDir.y, containerLocalDir.z).normalized;
            var containerLocalRight = _viewContainer.InverseTransformDirection(Vector3.right);
            var angle = Vector3.SignedAngle(containerLocalRight, containerLocalDirInXPlaneOnly, Vector3.forward);
            _joint2.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        private void RotateJoint3(Vector3 containerLocalDir)
        {
            _joint2ForwardDir = -_joint2.forward;
            _joint2UpDir = _joint2.up;

            var containerLocalx = _viewContainer.InverseTransformDirection(-_joint2.forward);
            var angle = Vector3.SignedAngle(containerLocalx, containerLocalDir, _joint2.up);
            _joint3.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        private void RotateJoint1(Vector3 intersectionPoint)
        {
            var theta = Mathf.Atan2(intersectionPoint.y, intersectionPoint.x) * Mathf.Rad2Deg;
            _joint1.localRotation = Quaternion.Euler(theta, 0f, 0f);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            var origin = _joint2.position;
            var target = origin + _link2Dir * 0.1f;
            Gizmos.DrawLine(origin, target);

            DrawJoint2Gizmos();
            DrawJoint3Gizmos();
        }

        private void DrawJoint3Gizmos()
        {
            if (!_showJoin3Gizmos) return;

            {
                Gizmos.color = Color.red;
                var origin = _joint2.position;
                var target = origin + _joint2ForwardDir * 0.1f;
                Gizmos.DrawLine(origin, target);
            }

            {
                Gizmos.color = Color.yellow;
                var origin = _joint2.position;
                var target = origin + _joint2UpDir * 0.1f;
                Gizmos.DrawLine(origin, target);
            }
        }

        private void DrawJoint2Gizmos()
        {
            if (!_showJoin2Gizmos) return;

            Gizmos.color = Color.blue;
            var origin = _joint2.position;
            var target = origin + Vector3.right * 0.1f;
            Gizmos.DrawLine(origin, target);
        }
    }
}