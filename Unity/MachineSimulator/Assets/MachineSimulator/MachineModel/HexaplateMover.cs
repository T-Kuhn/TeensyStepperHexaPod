using System.Collections.Generic;
using UnityEngine;

namespace MachineSimulator.MachineModel
{
    public sealed class HexaplateMover : MonoBehaviour
    {
        public float DefaultHeight { get; set; }
        private Dictionary<StrategyName, IHexaplateMovementStrategy> _strategies;

        public StrategyName CurrentStrategy;

        private void Awake()
        {
            _strategies = new Dictionary<StrategyName, IHexaplateMovementStrategy>()
            {
                { StrategyName.UpDown, new UpDownStrategy() },
                { StrategyName.BackForth, new BackForthStrategy() },
                { StrategyName.LeftRight, new LeftRightStrategy() },
                { StrategyName.MoveInCircle, new MoveInCircleStrategy() },
                { StrategyName.MoveInCircleCombinedWithUpDown, new MoveInCircleWhileGoingUpAndDownStrategy() },
                { StrategyName.TiltArountX, new TiltAroundXStrategy() }
            };
        }

        private void Update()
        {
            var time = Time.time;
            var (position, rotation) = _strategies[CurrentStrategy].Move(time);
            transform.position = position + Vector3.up * DefaultHeight;
            transform.rotation = rotation;
        }
    }
}