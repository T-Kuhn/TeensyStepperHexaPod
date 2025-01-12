using UnityEngine;

namespace UniversalJointCheck.MachineModel
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
            var centerPos = transform.position;

            InstantiateIfNecessary();
        }

        private void InstantiateIfNecessary()
        {
            if (_arms != null) return;

            _arms = new SingleArmMover[6];

            var startDir = -transform.forward;
            for (var i = 0; i < 3; i++)
            {
                // TODO: add pairs of arms, not just single arms and setup solution so that elbows point apart from each other
                var angle = i * -120f;
                var arm = Instantiate(_singleArmPrefab, transform);
                var quaternion = Quaternion.Euler(0, angle, 0);
                var dir = quaternion * startDir;
                arm.transform.localPosition = dir * _distanceFromCenterMotorPairs;
                arm.transform.localRotation = quaternion;
                arm.name = $"Arm{i}";
                
                var target = Instantiate(_targetPrefab);
                target.transform.position = arm.transform.position + Vector3.up * 0.2f;
                target.transform.parent = _hexaPlate.transform;
                arm.SetupTargetRef(target.transform);
                
                _arms[i] = arm;
            }
        }
    }
}