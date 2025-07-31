using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MachineSimulator.Machine;
using UniRx;
using UnityEngine;
using Logger = MachineSimulator.Logging.Logger;

namespace MachineSimulator.MachineModel
{
    public sealed class HexaplateMover : MonoBehaviour
    {
        private readonly Subject<Unit> _onPoseChanged = new Subject<Unit>();

        // NOTE: This Observable triggers the IK on the machine model.
        //       Because of that, every position/rotation change needs to cause an onNext on the Subject.
        public IObservable<Unit> OnPoseChanged => _onPoseChanged;

        public float DefaultHeight { get; set; }
        private Dictionary<StrategyName, IHexaplateMovementStrategy> _strategies;

        public StrategyName CurrentStrategy;

        private bool _isInPlaybackMode;
        private Logger _logger;

        public void StartPlaybackMode(List<HLInstruction> instructions, bool isLinear = false)
        {
            PlaybackSequenceAsync(instructions, isLinear).Forget();
        }

        private void Awake()
        {
            _strategies = new Dictionary<StrategyName, IHexaplateMovementStrategy>()
            {
                { StrategyName.DoNothing, null },
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
            if (_isInPlaybackMode)
            {
                return;
            }

            ExecuteStrategie();
        }

        private void LateUpdate()
        {
            // _logger.UpdateLogging(transform.position.y);
        }

        private async UniTaskVoid PlaybackSequenceAsync(List<HLInstruction> instructions, bool isLinear)
        {
            _isInPlaybackMode = true;
            _logger.StartLogging();
            
            foreach (var instruction in instructions)
            {
                var currentPosition = transform.position;
                var currentRotation = transform.rotation;

                var targetPosition = instruction.TargetMachineState.PlateCenterPosition;
                var targetRotation = instruction.TargetMachineState.PlateRotationQuaternion;

                var moveTime = instruction.MoveTime;
                var elapsedTime = 0f;

                while (true)
                {
                    elapsedTime += Time.deltaTime;

                    // NOTE: t always goes from 0 to 1
                    var t = elapsedTime / moveTime;

                    if (t >= 1f)
                    {
                        break;
                    }

                    // NOTE: theta always goes from 0 to PI
                    var theta = t * Mathf.PI;

                    // NOTE: r goes from 2 to 0
                    var r = Mathf.Cos(theta) + 1;

                    // NOTE: s goes from 0 to 1
                    // NOTE: if we are moving to the target linearly, s the same as t
                    var s = isLinear ? t : (2 - r) / 2f;

                    // Interpolate position and rotation
                    var position = Vector3.Lerp(currentPosition, targetPosition, s);
                    var rotation = Quaternion.Lerp(currentRotation, targetRotation, s);

                    UpdatePositionAndRotationTo(position, rotation);

                    await UniTask.Yield();
                }
            }
            
            _isInPlaybackMode = false;
            _logger.StopLogging();
        }

        public void TeleportToDefaultHeight()
        {
            UpdatePositionAndRotationTo(position: Vector3.up * DefaultHeight);
        }

        public void UpdatePositionAndRotationTo(Vector3? position = null, Quaternion? rotation = null)
        {
            if (position.HasValue)
            {
                transform.position = position.Value;
            }

            if (rotation.HasValue)
            {
                transform.rotation = rotation.Value;
            }

            _onPoseChanged.OnNext(Unit.Default);
        }

        private void ExecuteStrategie()
        {
            if (CurrentStrategy == StrategyName.DoNothing) return;

            var time = Time.time * 3f;
            var (position, rotation) = _strategies[CurrentStrategy].Move(time);
            var newPosition = position + Vector3.up * DefaultHeight;

            UpdatePositionAndRotationTo(newPosition, rotation);
        }

        public void InjectRefs(Logger logger)
        {
            _logger = logger;
        }
    }
}