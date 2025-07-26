using UnityEngine;

namespace MachineSimulator.Machine
{
    public readonly struct HLMachineState
    {
        public Vector3 PlateCenterPosition { get; }
        public Quaternion PlateRotationQuaternion { get; }

        public HLMachineState(Vector3 plateCenterPosition, Quaternion plateRotationQuaternion)
        {
            PlateCenterPosition = plateCenterPosition;
            PlateRotationQuaternion = plateRotationQuaternion;
        }
    }
}
