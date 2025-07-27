using UniRx;
using UnityEngine;

namespace MachineSimulator.MachineModel
{
    public class MachineModel : MonoBehaviour
    {
        [SerializeField] private SingleArmMover _armLeftPrefab;
        [SerializeField] private SingleArmMover _armRightPrefab;
        [SerializeField] private GameObject _targetPrefab;
        [SerializeField] private HexaplateMover _hexaPlatePrefab;
        [SerializeField] private float _hexaplateDefaultHeight;
        [SerializeField] private float _distanceFromCenterMotorPairs;
        [SerializeField] private float _distanceFromCenterTargetPairs;
        [SerializeField] private float _distanceApartMotorPairs;
        [SerializeField] private float _distanceApartTargetPairs;
        [SerializeField] private float _downwardOffsetForTargetPairs;

        // Order of arms in array: FrontLeft first, then counter-clockwise around the center
        private SingleArmMover[] _arms = null;
        private HexaplateMover _hexaPlate;
        
        public Transform HexaPlateTransform => _hexaPlate.transform;
        public HexaplateMover HexaPlateMover => _hexaPlate;

        private void Start()
        {
            _hexaPlate = Instantiate(_hexaPlatePrefab);
            _hexaPlate.DefaultHeight = _hexaplateDefaultHeight;
            _hexaPlate.TeleportToDefaultHeight();
        }

        private void Update()
        {
            InstantiateIfNecessary();
        }

        private void InstantiateIfNecessary()
        {
            if (_arms != null) return;

            _arms = new SingleArmMover[6];

            var startDir = -transform.forward;
            var armIndex = 0;
            for (var i = 0; i < 3; i++)
            {
                var angle = i * -120f;
                var quaternion = Quaternion.Euler(0, angle, 0);
                var dir = quaternion * startDir;
                var leftRot = Quaternion.Euler(0, 90f, 0);
                var rightRot = Quaternion.Euler(0, -90f, 0);
                var leftDir = leftRot * dir;
                var rightDir = rightRot * dir;
                var centerPosition = dir * _distanceFromCenterMotorPairs;
                var leftPosition = centerPosition + leftDir * _distanceApartMotorPairs;
                var rightPosition = centerPosition + rightDir * _distanceApartMotorPairs;
                var targetCenterPosition = dir * _distanceFromCenterTargetPairs;
                var leftTargetPosition = targetCenterPosition + leftDir * _distanceApartTargetPairs;
                var rightTargetPosition = targetCenterPosition + rightDir * _distanceApartTargetPairs;

                var leftArm = InstantiateArm(leftPosition, quaternion, $"Arm{i}", true, true);
                InstantiateTarget(leftArm, leftTargetPosition);
                _arms[armIndex++] = leftArm;

                var rightArm = InstantiateArm(rightPosition, quaternion, $"Arm{i}", false, false);
                InstantiateTarget(rightArm, rightTargetPosition);
                _arms[armIndex++] = rightArm;
            }
        }

        private void InstantiateTarget(SingleArmMover arm, Vector3 targetPosition)
        {
            var target = Instantiate(_targetPrefab);
            var hexaPlateHeight = _hexaPlate.transform.position.y;
            target.transform.position = targetPosition + (hexaPlateHeight - _downwardOffsetForTargetPairs) * Vector3.up;
            target.transform.parent = _hexaPlate.transform;
            arm.SetupTargetRef(target.transform);
            arm.SetupCenterRef(_hexaPlate.transform);

            _hexaPlate.OnPoseChanged.Subscribe(_ => arm.RunIk()).AddTo(this);
        }

        private SingleArmMover InstantiateArm(
            Vector3 position,
            Quaternion quaternion,
            string name,
            bool useSecondSolution,
            bool isLeftArm)
        {
            var arm = Instantiate(isLeftArm ? _armLeftPrefab : _armRightPrefab, transform);
            arm.SetupUseSecondSolution(useSecondSolution);
            arm.transform.localPosition = position;
            arm.transform.localRotation = quaternion;
            arm.name = name;

            return arm;
        }
    }
}