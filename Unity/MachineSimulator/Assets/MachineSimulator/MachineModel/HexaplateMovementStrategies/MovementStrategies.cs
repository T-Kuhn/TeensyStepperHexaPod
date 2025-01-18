using UnityEngine;

namespace MachineSimulator.MachineModel
{
    public enum StrategyName
    {
        UpDown,
        BackForth,
        LeftRight,
        MoveInCircle,
        MoveInCircleCombinedWithUpDown
    }

    public sealed class UpDownStrategy : IHexaplateMovementStrategy
    {
        public (Vector3 Position, Quaternion Rotation) Move(float time)
        {
            var height = Mathf.Sin(time) * 0.05f;
            return (new Vector3(0, height, 0), Quaternion.identity);
        }
    }

    public sealed class BackForthStrategy : IHexaplateMovementStrategy
    {
        public (Vector3 Position, Quaternion Rotation) Move(float time)
        {
            var offset = Mathf.Sin(time) * 0.1f;
            return (new Vector3(0, 0, offset), Quaternion.identity);
        }
    }

    public sealed class LeftRightStrategy : IHexaplateMovementStrategy
    {
        public (Vector3 Position, Quaternion Rotation) Move(float time)
        {
            var offset = Mathf.Sin(time) * 0.1f;
            return (new Vector3(offset, 0, 0), Quaternion.identity);
        }
    }

    public sealed class MoveInCircleStrategy : IHexaplateMovementStrategy
    {
        public (Vector3 Position, Quaternion Rotation) Move(float time)
        {
            var x = Mathf.Sin(time) * 0.1f;
            var z = Mathf.Cos(time) * 0.1f;
            return (new Vector3(x, 0, z), Quaternion.identity);
        }
    }

    public sealed class MoveInCircleWhileGoingUpAndDownStrategy : IHexaplateMovementStrategy
    {
        public (Vector3 Position, Quaternion Rotation) Move(float time)
        {
            var height = Mathf.Sin(time) * 0.05f;
            var x = Mathf.Sin(time) * 0.05f;
            var z = Mathf.Cos(time) * 0.05f;
            return (new Vector3(x, height, z), Quaternion.identity);
        }
    }
}