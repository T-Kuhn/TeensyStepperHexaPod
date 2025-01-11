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
        [SerializeField] private Transform _joint4;
        [SerializeField] private Transform _joint5;

        [SerializeField] private Transform _joint1Tip;

        [SerializeField] private bool _useSecondSolution;
        [SerializeField] private bool _showJoin2Gizmos;
        [SerializeField] private bool _showJoin3Gizmos;
        [SerializeField] private bool _debugLog;

        private Vector3 _worldLink2Dir;

        private Vector3 _transformRight;
        private Vector3 _joint2BackDir;
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
            var (joint2BackDir, joint2UpDir) = RotateJoint3(containerLocalDir);
            RotateJoint4();

            UpdateGizmoData(worldLink2Dir, transformRight, joint2BackDir, joint2UpDir);
        }

        private void UpdateGizmoData(Vector3 worldLink2Dir, Vector3 transformRight, Vector3 joint2BackDir, Vector3 joint2UpDir)
        {
            _worldLink2Dir = worldLink2Dir;
            _transformRight = transformRight;
            _joint2BackDir = joint2BackDir;
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

        private void RotateJoint1(Vector3 intersectionPoint)
        {
            var theta = Mathf.Atan2(intersectionPoint.y, intersectionPoint.x) * Mathf.Rad2Deg;
            _joint1.localRotation = Quaternion.Euler(theta, 0f, 0f);
        }

        // NOTE: Joint2 rotates around the X-axis
        private Vector3 RotateJoint2(Vector3 containerLocalDir)
        {
            var transformRight = transform.right;

            var containerLocalDirInXPlaneOnly = new Vector3(0f, containerLocalDir.y, containerLocalDir.z).normalized;
            var containerLocalRight = _viewContainer.InverseTransformDirection(_transformRight);
            var angle = Vector3.SignedAngle(containerLocalRight, containerLocalDirInXPlaneOnly, Vector3.forward);
            _joint2.localRotation = Quaternion.Euler(angle, 0f, 0f);

            return transformRight;
        }

        // NOTE: Joint3 rotates around the Y-axis
        private (Vector3, Vector3) RotateJoint3(Vector3 containerLocalDir)
        {
            var joint2BackDir = -_joint2.forward;
            var joint2UpDir = _joint2.up;

            var localJoint2ForwardDir = _viewContainer.InverseTransformDirection(joint2BackDir);
            var localJoint2UpDir = _viewContainer.InverseTransformDirection(joint2UpDir);
            var angle = Vector3.SignedAngle(localJoint2ForwardDir, containerLocalDir, localJoint2UpDir);
            _joint3.localRotation = Quaternion.Euler(0f, angle, 0f);

            return (joint2BackDir, joint2UpDir);
        }

        // NOTE: Joint4 rotates around the Y-axis
        private void RotateJoint4()
        {
            var joint3LocalRot = _joint3.localRotation;
            _joint4.localRotation = Quaternion.Euler(0f, -joint3LocalRot.eulerAngles.y, 0f);
        }

        // NOTE: Joint4 rotates around the X-axis
        private void RotateJoint5()
        {
        }

        void OnDrawGizmos()
        {
            DrawLink2DirGizmos();
            DrawJoint2Gizmos();
            DrawJoint3Gizmos();
        }

        private void DrawLink2DirGizmos()
        {
            Gizmos.color = Color.green;
            var origin = _joint2.position;
            var target = origin + _worldLink2Dir * 0.1f;
            Gizmos.DrawLine(origin, target);
        }

        private void DrawJoint3Gizmos()
        {
            if (!_showJoin3Gizmos) return;

            {
                Gizmos.color = Color.red;
                var origin = _joint2.position;
                var target = origin + _joint2BackDir * 0.1f;
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