using UnityEngine;

namespace UniversalJointCheck.MachineModel
{
    public sealed class HexaplateMover : MonoBehaviour
    {
        private readonly float _startHeight = 0.15f;
        private IHexaplateMovementStrategy _strategy = null;

        private void Awake()
        {
            _strategy = new UpDownStrategy();
        }

        private void Update()
        {
            var time = Time.time;
            var (position, rotation) = _strategy.Move(time);
            transform.position = position + Vector3.up * _startHeight;
            transform.rotation = rotation;
        }
    }
}