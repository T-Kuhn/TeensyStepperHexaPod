using UniversalJointCheck.Ik;
using UnityEngine;

namespace UniversalJointCheck.MachineModel
{
    public class SingleArmMover : MonoBehaviour
    {
        [SerializeField] private Transform _viewContainer;

        [SerializeField] private Transform _target;
        [SerializeField] private Transform _joint1;
        [SerializeField] private Transform _joint2;
        [SerializeField] private Transform _joint3;

        [SerializeField] private Transform _joint1Tip;

        [SerializeField] private bool _useSecondSolution;
        [SerializeField] private bool _showJoin2Gizmos;
        [SerializeField] private bool _showJoin3Gizmos;
        [SerializeField] private bool _debugLog;

        private Vector3 _worldLink2Dir;

        private Vector3 _transformRight;
        private Vector3 _joint2ForwardDir;
        private Vector3 _joint2UpDir;

        void Update()
        {
            var localTarget = transform.InverseTransformPoint(_target.position);

            var ikResult = SphereCircleIntersectIK.Solve(
                sphereCenter: localTarget,
                circleCenter: Vector3.zero,
                sphereRadius: 0.124f,
                circleRadius: 0.112f);

            if (!ikResult.Success) return;

            var intersectionPoint = _useSecondSolution ? ikResult.P2 : ikResult.P1;

            if (_debugLog)
            {
                Debug.Log("frame: " + Time.frameCount + "  localTarget: " + localTarget + "  intersectionPoint: " + intersectionPoint);
            }

            RotateJoint1(intersectionPoint);
            var worldLink2Dir = MoveLink2ToJoint1TipAndGetLink2Dir(intersectionPoint, localTarget);
            var containerLocalDir = _viewContainer.InverseTransformDirection(worldLink2Dir);
            var transformRight = RotateJoint2(containerLocalDir);
            var (joint2ForwardDir, joint2UpDir) = RotateJoint3(containerLocalDir);

            UpdateGizmoData(worldLink2Dir, transformRight, joint2ForwardDir, joint2UpDir);
        }

        private void UpdateGizmoData(Vector3 worldLink2Dir, Vector3 transformRight, Vector3 joint2ForwardDir, Vector3 joint2UpDir)
        {
            _worldLink2Dir = worldLink2Dir;
            _transformRight = transformRight;
            _joint2ForwardDir = joint2ForwardDir;
            _joint2UpDir = joint2UpDir;
        }

        private Vector3 MoveLink2ToJoint1TipAndGetLink2Dir(Vector3 intersectionPoint, Vector3 localTarget)
        {
            // Move Link2 to joint1 tip
            var joint1TipWorldPos = _joint1Tip.position;
            var containerLocalJoint1TipPos = _viewContainer.InverseTransformPoint(joint1TipWorldPos);
            _joint2.localPosition = containerLocalJoint1TipPos;

            // Get Link2 direction
            var localLink2Dir = (localTarget - intersectionPoint).normalized;
            return transform.TransformDirection(localLink2Dir);
        }

        private Vector3 RotateJoint2(Vector3 containerLocalDir)
        {
            var transformRight = transform.right;

            var containerLocalDirInXPlaneOnly = new Vector3(0f, containerLocalDir.y, containerLocalDir.z).normalized;
            var containerLocalRight = _viewContainer.InverseTransformDirection(_transformRight);
            var angle = Vector3.SignedAngle(containerLocalRight, containerLocalDirInXPlaneOnly, Vector3.forward);
            _joint2.localRotation = Quaternion.Euler(angle, 0f, 0f);

            return transformRight;
        }

        private (Vector3, Vector3) RotateJoint3(Vector3 containerLocalDir)
        {
            var joint2ForwardDir = -_joint2.forward;
            var joint2UpDir = _joint2.up;

            var containerLocalx = _viewContainer.InverseTransformDirection(joint2ForwardDir);
            var angle = Vector3.SignedAngle(containerLocalx, containerLocalDir, joint2UpDir);
            _joint3.localRotation = Quaternion.Euler(0f, angle, 0f);

            return (joint2ForwardDir, joint2UpDir);
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
            var target = origin + _worldLink2Dir * 0.1f;
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
            var target = origin + _transformRight * 0.1f;
            Gizmos.DrawLine(origin, target);
        }
    }
}