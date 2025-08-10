using MachineSimulator.Ik;
using UnityEngine;
using Logger = MachineSimulator.Logging.Logger;

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
        [SerializeField] private bool _fixPiMinusPiDiscontiniuty;
        [SerializeField] private bool _showDebugLog;
        [SerializeField] private bool _showDebugGizmos;

        [SerializeField] private float _finalJointOffset;

        private float _motorRotation;
        private Logger _logger;

        // NOTE: For debugging
        private Vector3 _worldLink2Dir;
        private Vector3 _realTarget;

        // NOTE: For debugging
        private (Vector3 Origin, Vector3 Dir) _greenDebugGizmoLine;
        private (Vector3 Origin, Vector3 Dir) _redDebugGizmoLine;
        private (Vector3 Origin, Vector3 Dir) _blueDebugGizmoLineThree;

        private float _motorOriginOffset;
        private LLMachineStateProvider _llMachineStateProvider;
        private int _armIndex;

        public void SetupRefs(Transform target, Transform centerRef, Logger logger, LLMachineStateProvider stateProvider)
        {
            _target = target;
            _center = centerRef;
            _logger = logger;
            _llMachineStateProvider = stateProvider;
        }

        public void SetupUseSecondSolution(bool useSecondSolution) => _useSecondSolution = useSecondSolution;

        public void SetupFixPiMinusPiDiscontinuity(bool fixPiMinusPiDiscontinuity) => _fixPiMinusPiDiscontiniuty = fixPiMinusPiDiscontinuity;

        private void Update()
        {
            RunIk();
        }

        private void LateUpdate()
        {
            _logger?.UpdateLogging(_motorRotation);
        }

        public void RunIk(bool isTeleportToOriginPoseChange = false)
        {
            var worldOffsetDir = transform.TransformDirection(Vector3.forward);
            var rotatedWorldOffsetDir = _center.rotation * worldOffsetDir;
            var realTarget = _target.position - rotatedWorldOffsetDir * _finalJointOffset;
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
                sphereRadius: Mathf.Abs(_joint4.localPosition.z),
                circleRadius: Mathf.Abs(_joint1Tip.localPosition.z));

            if (!ikResult.Success) return;

            var intersectionPoint = _useSecondSolution ? ikResult.P2 : ikResult.P1;

            if (_showDebugLog)
            {
                Debug.Log("frame: " + Time.frameCount + "  localTarget: " + localTarget + "  intersectionPoint: " + intersectionPoint);
            }

            RotateJoint1(intersectionPoint, isTeleportToOriginPoseChange);
            var worldLink2Dir = MoveLink2ToJoint1TipAndGetLink2Dir(intersectionPoint, localTarget);
            var containerLocalDir = _viewContainer.InverseTransformDirection(worldLink2Dir);
            RotateJoint2(containerLocalDir);
            RotateJoint3(containerLocalDir);
            RotateJoint4(realTargetToTarget);
            RotateJoint5(realTargetToTarget);

            UpdateGizmoData(worldLink2Dir, realTarget);
            
            _llMachineStateProvider.SetRotationStateForArmWithIndex(_armIndex, _motorRotation);
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

        private void RotateJoint1(Vector3 intersectionPoint, bool isTeleportToOriginPoseChange)
        {
            var theta = Mathf.Atan2(intersectionPoint.y, intersectionPoint.x);
            SetMotorRotation(theta, isTeleportToOriginPoseChange);
            _joint1.localRotation = Quaternion.Euler(theta * Mathf.Rad2Deg, 0f, 0f);
        }

        private void SetMotorRotation(float theta, bool isTeleportToOriginPoseChange)
        {
            // NOTE: We want to motor rotation to be continuous and in the range [0, 2π]
            //       Without below fix theta switched form -PI to +PI.
            //       However, WITH below fix a new discontiniuty arises at the 2PI-0 border
            //       (theta will switch from 2PI to 0 instead of going from +0 to -0), that's why we only apply
            //       the fix to arms that actually can physically go through the range where
            //       the incontiniuty happens.
            if (_fixPiMinusPiDiscontiniuty && theta < 0f)
            {
                theta += Mathf.PI * 2f;
            }

            if (isTeleportToOriginPoseChange)
            {
                _motorOriginOffset = -theta;
            }

            _motorRotation = _motorOriginOffset + theta;
        }

        // NOTE: Joint2 rotates around the X-axis
        private void RotateJoint2(Vector3 containerLocalDir)
        {
            var containerLocalDirInXPlaneOnly = new Vector3(0f, containerLocalDir.y, containerLocalDir.z).normalized;
            var containerLocalRight = _viewContainer.InverseTransformDirection(transform.right);
            var containerLocalForward = _viewContainer.InverseTransformDirection(transform.forward);
            var angle = Vector3.SignedAngle(containerLocalRight, containerLocalDirInXPlaneOnly, containerLocalForward);
            /*
            SetupDebugGizmoData(
                origin: _joint1Tip.position,
                greenDir: _viewContainer.TransformDirection(containerLocalRight),
                redDir: _viewContainer.TransformDirection(containerLocalDirInXPlaneOnly),
                blueDir: _viewContainer.TransformDirection(containerLocalForward)
            );
            */
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
            /*
            SetupDebugGizmoData(
                origin: _joint4.position,
                greenDir: linkDir,
                redDir: -_joint4.right,
                blueDir: -_joint4.forward
            );
            */
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

        public void SetIndexTo(int index)
        {
            _armIndex = index;
        }
    }
}