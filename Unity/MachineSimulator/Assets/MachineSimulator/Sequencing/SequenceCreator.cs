using System.Collections.Generic;
using MachineSimulator.Machine;
using UnityEngine;
using System.Linq;

namespace MachineSimulator.Sequencing
{
    public sealed class SequenceCreator : MonoBehaviour
    {
        [SerializeField] private MachineModel.MachineModel _machineModel;

        private readonly List<HLInstruction> _sequence = new List<HLInstruction>();

        public void Add(HLInstruction hlInstruction)
        {
            _sequence.Add(hlInstruction);
        }

        public void StartPlayback()
        {
            var hexaPlateMover = _machineModel.HexaPlateMover;
            hexaPlateMover.StartPlaybackMode(_sequence);
        }

        public void ClearAll()
        {
            _sequence.Clear();
        }

        public void StartStringedPlayback()
        {
            var stringedInstructions = CreateListOfStringedHighLevelInstructions();
            // FIXME: Get rid of the need to go through the data and create two new lists here.
            var sringedHighLevelInstructions = stringedInstructions.Select(data => data.Item1).ToList();
            var stringedLowLevelInstuctions = stringedInstructions.Select(data => data.Item2).ToList();

            _machineModel.HexaPlateMover.StartPlaybackMode(sringedHighLevelInstructions, true);
        }

        private List<(HLInstruction, LLInstruction)> CreateListOfStringedHighLevelInstructions()
        {
            return _sequence.SelectMany(instruction =>
            {
                var currentPosition = _machineModel.HexaPlateTransform.position;
                var currentRotation = _machineModel.HexaPlateTransform.rotation;

                var targetPosition = instruction.TargetMachineState.PlateCenterPosition;
                var targetRotation = instruction.TargetMachineState.PlateRotationQuaternion;

                var moveTime = instruction.MoveTime;
                var elapsedTimes = new List<float>();
                var numberOfSteps = 50;

                // NOTE: Goes from 1 to 9 (in the case of 10 steps)
                for (var i = 1; i < numberOfSteps; i++)
                {
                    elapsedTimes.Add(i * moveTime / numberOfSteps);
                }

                // NOTE: We fill in the last step manually to make sure there are no floating point errors
                elapsedTimes.Add(moveTime);

                var stringedInstructions = new List<(HLInstruction, LLInstruction)>();
                var stringedMoveTime = moveTime / numberOfSteps;
                foreach (var elapsedTime in elapsedTimes)
                {
                    // NOTE: t always goes from 0 to 1
                    var t = elapsedTime / moveTime;

                    // NOTE: theta always goes from 0 to PI
                    var theta = t * Mathf.PI;

                    // NOTE: r goes from 2 to 0
                    var r = Mathf.Cos(theta) + 1;

                    // NOTE: s goes from 0 to 1
                    var s = (2 - r) / 2f;

                    // Interpolate position and rotation
                    var position = Vector3.Lerp(currentPosition, targetPosition, s);
                    var rotation = Quaternion.Lerp(currentRotation, targetRotation, s);

                    _machineModel.HexaPlateMover.UpdatePositionAndRotationTo(position, rotation);

                    var stringedMachineState = new HLMachineState(position, rotation);
                    
                    // NOTE: Create LowLevelInstruction from Motor Position retreived AFTER IK was run on all Arms.
                    var state = _machineModel.MachineStateProvider.CurrentLowLevelMachineState;
                    var highLevelInstruction = new HLInstruction(stringedMachineState, stringedMoveTime);
                    var lowLevelInstruction = new LLInstruction(state, moveTime);
                    stringedInstructions.Add((highLevelInstruction, lowLevelInstruction));

                    /*
                    Debug.Log("motor1Rot: " + state.Motor1Rotation + " motor2Rot: " + state.Motor2Rotation
                              + " motor3Rot: " + state.Motor3Rotation + " motor4Rot: " + state.Motor4Rotation
                              + " motor5Rot: " + state.Motor5Rotation + " motor6Rot: " + state.Motor6Rotation);
                    */
                }

                return stringedInstructions;
            }).ToList();
        }
    }
}