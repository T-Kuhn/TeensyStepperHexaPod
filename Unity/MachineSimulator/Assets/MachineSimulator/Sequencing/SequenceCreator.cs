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
            var listOfStringedInstructions = CreateListOfStringedHighLevelInstructions();

            _machineModel.HexaPlateMover.StartPlaybackMode(listOfStringedInstructions, true);
        }

        private List<HLInstruction> CreateListOfStringedHighLevelInstructions()
        {
            return _sequence.SelectMany(instruction =>
            {
                var currentPosition = _machineModel.HexaPlateTransform.position;
                var currentRotation = _machineModel.HexaPlateTransform.rotation;

                var targetPosition = instruction.TargetMachineState.PlateCenterPosition;
                var targetRotation = instruction.TargetMachineState.PlateRotationQuaternion;

                var moveTime = instruction.MoveTime;
                var elapsedTimes = new List<float>();
                var numberOfSteps = 10;

                // NOTE: Goes from 1 to 9 (in the case of 10 steps)
                for (var i = 1; i < numberOfSteps; i++)
                {
                    elapsedTimes.Add(i * moveTime / numberOfSteps);
                }

                // NOTE: We fill in the last step manually to make sure there are no floating point errors
                elapsedTimes.Add(moveTime);

                var stringedInstructions = new List<HLInstruction>();
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
                    stringedInstructions.Add(new HLInstruction(stringedMachineState, stringedMoveTime));
                }

                return stringedInstructions;
            }).ToList();
        }
    }
}