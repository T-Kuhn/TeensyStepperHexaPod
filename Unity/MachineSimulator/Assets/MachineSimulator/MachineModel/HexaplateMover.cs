using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MachineSimulator.Controlling;
using MachineSimulator.Machine;
using UniRx;
using UnityEngine;
using Logger = MachineSimulator.Logging.Logger;

namespace MachineSimulator.MachineModel
{
    public sealed class HexaplateMover : MonoBehaviour
    {
        private readonly Subject<bool> _onPoseChanged = new Subject<bool>();

        // NOTE: This Observable triggers the IK on the machine model.
        //       Because of that, every position/rotation change needs to cause an onNext on the Subject.
        public IObservable<bool> OnPoseChanged => _onPoseChanged;

        public float DefaultHeight { get; set; }
        public Vector3 RestPosition { get; private set; }
        public Quaternion RestRotation { get; private set; } = Quaternion.identity;
        private Dictionary<StrategyName, IHexaplateMovementStrategy> _strategies;

        public StrategyName CurrentStrategy;

        private bool _isInPlaybackMode;
        private Logger _logger;

        public bool UseManualTime;

        public Transform CameraOneTransform => _cameraPositionDummyOne.transform;
        public Transform CameraTwoTransform => _cameraPositionDummyTwo.transform;

        [SerializeField] private GameObject _cameraPositionDummyOne;
        [SerializeField] private GameObject _cameraPositionDummyTwo;

        [Range(0f, 10f)] public float ManualTime;

        public void StartPlaybackMode(List<HLInstruction> instructions, bool isLinear)
        {
            PlaybackSequenceAsync(instructions, isLinear).Forget();
        }

        public IHexaplateMovementStrategy GetStrategyFrom(StrategyName strategyName)
        {
            return _strategies.GetValueOrDefault(strategyName);
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
                { StrategyName.TiltArountX, new TiltAroundXStrategy() },
                { StrategyName.CircleTilt, new CircleTiltStrategy() }
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

        private async UniTaskVoid PlaybackSequenceAsync(List<HLInstruction> instructions, bool isLinear)
        {
            _isInPlaybackMode = true;
            // _logger.StartLogging();

            await UniTask.Yield(PlayerLoopTiming.Update);

            var carryoverTime = 0f;

            foreach (var instruction in instructions)
            {
                var currentPosition = transform.position;
                var currentRotation = transform.rotation;

                var targetPosition = instruction.TargetMachineState.PlateCenterPosition;
                var targetRotation = instruction.TargetMachineState.PlateRotationQuaternion;

                var moveTime = instruction.MoveTime;
                var elapsedTime = carryoverTime;
                carryoverTime = 0f;

                // Process as much of this instruction as possible within a single frame
                while (elapsedTime < moveTime)
                {
                    var frameTime = Time.deltaTime;
                    elapsedTime += frameTime;

                    // Clamp t to 1.0 to ensure we don't overshoot
                    var t = Mathf.Min(elapsedTime / moveTime, 1f);

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
                    await UniTask.Yield(PlayerLoopTiming.Update);

                    // If we've completed this instruction, carry over the excess time
                    if (elapsedTime >= moveTime)
                    {
                        carryoverTime = elapsedTime - moveTime;
                        break;
                    }
                }
            }

            _isInPlaybackMode = false;
            // _logger.StopLogging();
        }

        public (Vector3 DefaultPosition, Quaternion DefaultRotation) GetDefaultHeightPositionAndRotation()
        {
            return (Vector3.up * DefaultHeight, Quaternion.identity);
        }

        public void TeleportToDefaultHeight()
        {
            var (defaultPosition, defaultRotation) = GetDefaultHeightPositionAndRotation();
            RestPosition = defaultPosition;
            RestRotation = defaultRotation;
            UpdatePositionAndRotationTo(position: defaultPosition, rotation: defaultRotation, isTeleportToOriginPoseChange: true);
        }

        public void TeleportToRestPose()
        {
            UpdatePositionAndRotationTo(position: RestPosition, rotation: RestRotation, isTeleportToOriginPoseChange: false);
        }

        public void CaptureCurrentPoseAsRest()
        {
            RestPosition = transform.position;
            RestRotation = transform.rotation;
        }

        public void UpdatePositionAndRotationTo(Vector3? position = null, Quaternion? rotation = null, bool isTeleportToOriginPoseChange = false)
        {
            if (position.HasValue)
            {
                transform.position = position.Value;
            }

            if (rotation.HasValue)
            {
                transform.rotation = rotation.Value;
            }

            _onPoseChanged.OnNext(isTeleportToOriginPoseChange);
        }

        private void ExecuteStrategie()
        {
            if (CurrentStrategy == StrategyName.DoNothing) return;

            var time = UseManualTime ? ManualTime : Time.time * 3f;
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