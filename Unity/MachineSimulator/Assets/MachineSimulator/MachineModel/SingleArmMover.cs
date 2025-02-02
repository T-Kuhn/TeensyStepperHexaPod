using MachineSimulator.Ik;
using UnityEngine;

namespace MachineSimulator.MachineModel
{
    public class SingleArmMover : MonoBehaviour
    {
        [SerializeField] private Transform _viewContainer;

        [SerializeField] private Transform _target;
        [SerializeField] private Transform _center;

        [SerializeField] private Transform _joint1;
        [SerializeField] private Transform _joint2;
        [SerializeField] private Transform _joint3;
        [SerializeField] private Transform _joint4;
        [SerializeField] private Transform _joint5;

        [SerializeField] private Transform _joint1Tip;

        [SerializeField] private bool _useSecondSolution;
        [SerializeField] private bool _showDebugLog;
        [SerializeField] private bool _showDebugGizmos;

        // NOTE: For debugging
        private Vector3 _worldLink2Dir;
        private Vector3 _realTarget;

        // NOTE: For debugging
        private (Vector3 Origin, Vector3 Dir) _greenDebugGizmoLine;
        private (Vector3 Origin, Vector3 Dir) _redDebugGizmoLine;
        private (Vector3 Origin, Vector3 Dir) _blueDebugGizmoLineThree;

        public void SetupTargetRef(Transform target) => _target = target;
        public void SetupCenterRef(Transform centerRef) => _center = centerRef;

        public void SetupUseSecondSolution(bool useSecondSolution) => _useSecondSolution = useSecondSolution;

        void Update()
        {
            var worldOffsetDir = transform.TransformDirection(Vector3.forward);
            var rotatedWorldOffsetDir = _center.rotation * worldOffsetDir;
            var realTarget = _target.position - rotatedWorldOffsetDir * 0.02f;
            var localTarget = transform.InverseTransformPoint(realTarget);
            var realTargetToTarget = _target.position - realTarget;
            /*
            SetupDebugGizmoData(
                origin: _target.position,
                greenDir: -realTargetToTarget,
                redDir: null,
                blueDir: null
            );
            */

            var ikResult = SphereCircleIntersectIK.Solve(
                sphereCenter: localTarget,
                circleCenter: Vector3.zero,
                sphereRadius: 0.124f,
                circleRadius: 0.112f);

            if (!ikResult.Success) return;

            var intersectionPoint = _useSecondSolution ? ikResult.P2 : ikResult.P1;

            if (_showDebugLog)
            {
                Debug.Log("frame: " + Time.frameCount + "  localTarget: " + localTarget + "  intersectionPoint: " + intersectionPoint);
            }

            RotateJoint1(intersectionPoint);
            var worldLink2Dir = MoveLink2ToJoint1TipAndGetLink2Dir(intersectionPoint, localTarget);
            var containerLocalDir = _viewContainer.InverseTransformDirection(worldLink2Dir);
            RotateJoint2(containerLocalDir);
            RotateJoint3(containerLocalDir);
            RotateJoint4(realTargetToTarget);
            RotateJoint5(realTargetToTarget);

            UpdateGizmoData(worldLink2Dir, realTarget);
        }

        private void UpdateGizmoData(Vector3 worldLink2Dir, Vector3 realTarget)
        {
            _worldLink2Dir = worldLink2Dir;
            _realTarget = realTarget;
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
        private void RotateJoint2(Vector3 containerLocalDir)
        {
            var containerLocalDirInXPlaneOnly = new Vector3(0f, containerLocalDir.y, containerLocalDir.z).normalized;
            var containerLocalRight = _viewContainer.InverseTransformDirection(transform.right);
            var angle = Vector3.SignedAngle(containerLocalRight, containerLocalDirInXPlaneOnly, Vector3.forward);
            _joint2.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        // NOTE: Joint3 rotates around the Y-axis
        private void RotateJoint3(Vector3 containerLocalDir)
        {
            var localJoint2ForwardDir = _viewContainer.InverseTransformDirection(-_joint2.forward);
            var localJoint2UpDir = _viewContainer.InverseTransformDirection(_joint2.up);
            var angle = Vector3.SignedAngle(localJoint2ForwardDir, containerLocalDir, localJoint2UpDir);
            _joint3.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        // NOTE: Joint4 rotates around the Y-axis
        private void RotateJoint4(Vector3 linkDir)
        {
            var joint3ForwardDir = -_joint3.forward;
            var _viewContainerForward = _viewContainer.right;
            var joint3UpDir = _joint4.up;
            var projectedVector = Vector3.ProjectOnPlane(linkDir, _joint3.up);
            var angle = Vector3.SignedAngle(joint3ForwardDir, projectedVector, _joint3.up);

            /*
            SetupDebugGizmoData(
                origin: _joint4.position,
                greenDir: joint3ForwardDir,
                redDir: projectedVector,
                blueDir: _joint3.up
            );
            */
            _joint4.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        // NOTE: Joint4 rotates around the X-axis
        private void RotateJoint5(Vector3 linkDir)
        {
            // NOTE: This joint will rotate accoring to hexaplate rotation to make sure we can handle the tilt!
            var joint3ForwardDir = -_joint4.forward;
            var origin = _joint4.position;
            var _viewContainerForward = _viewContainer.right;
            var joint3UpDir = _joint4.up;
            var joint4BackDir = -_joint4.forward;

            SetupDebugGizmoData(
                origin: _joint4.position,
                greenDir: linkDir,
                redDir: -_joint4.right,
                blueDir: -_joint4.forward
            );
            var angle = -Vector3.SignedAngle(-_joint4.forward, linkDir, -_joint4.right);

            _joint5.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        private void SetupDebugGizmoData(Vector3 origin, Vector3? greenDir, Vector3? redDir, Vector3? blueDir)
        {
            _greenDebugGizmoLine = greenDir != null ? (origin, greenDir.Value) : _greenDebugGizmoLine;
            _redDebugGizmoLine = redDir != null ? (origin, redDir.Value) : _redDebugGizmoLine;
            _blueDebugGizmoLineThree = blueDir != null ? (origin, blueDir.Value) : _blueDebugGizmoLineThree;
        }

        void OnDrawGizmos()
        {
            DrawLink2DirGizmos();
            DrawRealTargetGizmos();
            DrawDebugGizmoLines();
        }

        private void DrawRealTargetGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_realTarget, 0.001f);
        }

        private void DrawLink2DirGizmos()
        {
            Gizmos.color = Color.green;
            var origin = _joint2.position;
            var target = origin + _worldLink2Dir * 0.1f;
            Gizmos.DrawLine(origin, target);
        }

        private void DrawDebugGizmoLines()
        {
            if (!_showDebugGizmos) return;

            {
                Gizmos.color = Color.green;
                var (origin, dir) = _greenDebugGizmoLine;
                var target = origin + dir;
                Gizmos.DrawLine(origin, target);
            }
            {
                Gizmos.color = Color.red;
                var (origin, dir) = _redDebugGizmoLine;
                var target = origin + dir;
                Gizmos.DrawLine(origin, target);
            }
            {
                Gizmos.color = Color.blue;
                var (origin, dir) = _blueDebugGizmoLineThree;
                var target = origin + dir;
                Gizmos.DrawLine(origin, target);
            }
        }
    }
}