using UnityEngine;

namespace MachineSimulator.MachineModel
{
    public interface IHexaplateMovementStrategy
    {
        (Vector3 Position, Quaternion Rotation) Move(float time);
    }
}

