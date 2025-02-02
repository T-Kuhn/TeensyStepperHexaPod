using UnityEngine;

namespace MachineSimulator.MachineModel
{
    public class MachineModel : MonoBehaviour
    {
        [SerializeField] private SingleArmMover _singleArmPrefab;
        [SerializeField] private GameObject _targetPrefab;
        [SerializeField] private GameObject _hexaPlate;

        [SerializeField] private float _distanceFromCenterMotorPairs;
        [SerializeField] private float _distanceFromCenterTargetPairs;
        [SerializeField] private float _distanceApartMotorPairs;
        [SerializeField] private float _distanceApartTargetPairs;

        // Order of arms in array: FrontLeft first, then counter-clockwise around the center
        private SingleArmMover[] _arms = null;

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
                var leftTargetPosition = targetCenterPosition+ leftDir * _distanceApartTargetPairs;
                var rightTargetPosition = targetCenterPosition+ rightDir * _distanceApartTargetPairs;

                var leftArm = InstantiateArm(leftPosition, quaternion, $"Arm{i}", true);
                InstantiateTarget(leftArm, leftTargetPosition);
                _arms[armIndex++] = leftArm;

                var rightArm = InstantiateArm(rightPosition, quaternion, $"Arm{i}", false);
                InstantiateTarget(rightArm, rightTargetPosition);
                _arms[armIndex++] = rightArm;
            }
        }

        private void InstantiateTarget(SingleArmMover arm, Vector3 targetPosition)
        {
            var target = Instantiate(_targetPrefab);
            target.transform.position = targetPosition + Vector3.up * 0.14f;
            target.transform.parent = _hexaPlate.transform;
            arm.SetupTargetRef(target.transform);
            arm.SetupCenterRef(_hexaPlate.transform);
        }

        private SingleArmMover InstantiateArm(Vector3 position, Quaternion quaternion, string name, bool useSecondSolution)
        {
            var arm = Instantiate(_singleArmPrefab, transform);
            arm.SetupUseSecondSolution(useSecondSolution);
            arm.transform.localPosition = position;
            arm.transform.localRotation = quaternion;
            arm.name = name;

            return arm;
        }
    }
}