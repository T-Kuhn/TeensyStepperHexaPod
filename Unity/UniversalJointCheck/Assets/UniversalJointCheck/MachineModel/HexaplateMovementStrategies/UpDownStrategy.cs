using UnityEngine;

namespace UniversalJointCheck.MachineModel
{
    public class UpDownStrategy : IHexaplateMovementStrategy
    {
        public (Vector3 Position, Quaternion Rotation) Move(float time)
        {
            // NOTE: Move up down in a sine wave
            var height = Mathf.Sin(time) * 0.05f;
            return (new Vector3(0, height, 0), Quaternion.identity);
        }
    }
}