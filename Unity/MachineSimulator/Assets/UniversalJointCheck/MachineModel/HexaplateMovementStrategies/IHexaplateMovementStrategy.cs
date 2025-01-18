using UnityEngine;

namespace UniversalJointCheck.MachineModel
{
    public interface IHexaplateMovementStrategy
    {
        (Vector3 Position, Quaternion Rotation) Move(float time);
    }
}

