using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MachineSimulator.Machine;
using UnityEngine;
using UnityEngine.Serialization;

namespace MachineSimulator.MachineModel
{
    public sealed class HexaplateMover : MonoBehaviour
    {
        public float DefaultHeight { get; set; }
        private Dictionary<StrategyName, IHexaplateMovementStrategy> _strategies;

        public StrategyName CurrentStrategy;

        private bool _isInPlaybackMode;

        public void StartPlaybackMode(List<HLInstruction> instructions)
        {
            Debug.Log("StartPlayback");
            PlaybackSequenceAsync(instructions).Forget();
            _isInPlaybackMode = true;
        }

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
            if (_isInPlaybackMode)
            {
                return;
            }
            
            ExecuteStrategie();
        }

        
        private async UniTaskVoid PlaybackSequenceAsync(List<HLInstruction> instructions)
        {
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
                    var s = (2 - r) / 2f;
                    
                    // Interpolate position and rotation
                    var position = Vector3.Lerp(currentPosition, targetPosition, s);
                    var rotation = Quaternion.Lerp(currentRotation, targetRotation, s);
                    transform.position = position;
                    transform.rotation = rotation;
                    
                    await UniTask.Yield();
                }
            }
        }
        
        public void TeleportToDefaultHeight()
        {
            transform.position = Vector3.up * DefaultHeight;
        }

        private void ExecuteStrategie()
        {
            var time = Time.time * 3f;
            var (position, rotation) = _strategies[CurrentStrategy].Move(time);
            transform.position = position + Vector3.up * DefaultHeight;
            transform.rotation = rotation;
        }
    }
}