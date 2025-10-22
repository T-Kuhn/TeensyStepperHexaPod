using System;
using System.Collections.Generic;
using MachineSimulator.Machine;
using UnityEngine;
using System.Linq;

namespace MachineSimulator.Sequencing
{
    public sealed class SequenceCreator : MonoBehaviour
    {
        [SerializeField] private MachineModel.MachineModel _machineModel;
        [SerializeField] private RealMachine _realMachine;

        private List<AbstractInstruction> _sequence = new List<AbstractInstruction>();

        public void UpdateAllMoveTimesInSequenceTo(float newMoveTime)
        {
            _sequence = _sequence.Select(abstractInstruction =>
            {
                if (abstractInstruction is AbstractHLInstruction abstractHlInstruction)
                {
                    // NOTE: We currently only update AbstractHLInstructions' moveTimes.
                    return new HLInstruction(abstractHlInstruction.Instruction.TargetMachineState, newMoveTime).ToAbstract();
                }

                if (abstractInstruction is AbstractStrategyInstruction abstractStrategyInstruction)
                {
                    return new AbstractStrategyInstruction(
                        abstractStrategyInstruction.StrategyName,
                        abstractStrategyInstruction.StartTime,
                        abstractStrategyInstruction.EndTime,
                        newMoveTime,
                        abstractStrategyInstruction.IsLinearMove
                    );
                }

                return abstractInstruction;
            }).ToList();
        }

        public void Add(HLInstruction hlInstruction) => Add(hlInstruction.ToAbstract());

        public void Add(AbstractInstruction abstractInstruction)
        {
            _sequence.Add(abstractInstruction);
        }

        public void ClearAll()
        {
            _sequence.Clear();
        }

        public void StartStringedPlayback(bool sendToRealMachine = false, Action onBeforeSendAction = null)
        {
            var stringedInstructions = CreateListOfStringedHighLevelInstructions();
            // FIXME: Get rid of the need to go through the data and create two new lists here.
            var sringedHighLevelInstructions = stringedInstructions.Select(data => data.Item1).ToList();
            var stringedLowLevelInstuctions = stringedInstructions.Select(data => data.Item2).ToList();

            onBeforeSendAction?.Invoke();

            if (sendToRealMachine)
            {
                _realMachine.Instruct(stringedLowLevelInstuctions);
                return;
            }

            _machineModel.HexaPlateMover.StartPlaybackMode(sringedHighLevelInstructions, true);
        }

        // 1. HLinstructions
        // 2. stringed HLInstructions
        // 3. stringed LLInstructions
        private List<(HLInstruction, LLInstruction)> CreateListOfStringedHighLevelInstructions()
        {
            return _sequence.SelectMany(abstractInstruction =>
            {
                if (abstractInstruction is AbstractHLInstruction abstractHlInstruction)
                {
                    return GenerateStringedFrom(abstractHlInstruction);
                }

                if (abstractInstruction is AbstractStrategyInstruction strategyInstruction)
                {
                    return GenerateStringedFrom(strategyInstruction);
                }

                return null;
            }).ToList();
        }

        private List<(HLInstruction, LLInstruction)> GenerateStringedFrom(AbstractStrategyInstruction strategyInstruction)
        {
            var strategy = _machineModel.HexaPlateMover.GetStrategyFrom(strategyInstruction.StrategyName);
            var startTime = strategyInstruction.StartTime;
            var endTime = strategyInstruction.EndTime;
            var isLinearMove = strategyInstruction.IsLinearMove;

            if (strategy == null)
            {
                return new List<(HLInstruction, LLInstruction)>();
            }

            // NOTE: - Is 1 when referenceMoveTime is 3secs
            //       - Is 1/4 when referenceMoveTime is 3/4secs
            var timeMultiplier = strategyInstruction.ReferenceMoveTime / 3f;
            var totalDuration = endTime - startTime;
            var instructionsPerSecond = 50f;
            var numberOfSteps = instructionsPerSecond * (totalDuration * timeMultiplier);
            var stringedMoveTime = totalDuration / numberOfSteps;
            var elapsedTimes = new List<float>();

            // NOTE: Goes from 1 to 9 (in the case of 10 steps)
            for (var i = 1; i < numberOfSteps; i++)
            {
                elapsedTimes.Add(i * stringedMoveTime);
            }

            // NOTE: We fill in the last step manually to make sure there are no floating point errors
            elapsedTimes.Add(totalDuration);

            var stringedInstructions = new List<(HLInstruction, LLInstruction)>();

            foreach (var elapsedTime in elapsedTimes)
            {
                // NOTE: t always goes from 0 to 1
                var t = elapsedTime / totalDuration;

                // NOTE: theta always goes from 0 to PI
                var theta = t * Mathf.PI;

                // NOTE: r goes from 2 to 0
                var r = Mathf.Cos(theta) + 1;

                // NOTE: s goes from 0 to 1
                var s = (2 - r) / 2f;

                var strategyTime = isLinearMove
                    ? startTime + elapsedTime
                    : startTime + s * totalDuration;

                // Get position and rotation from strategy at this time
                var (strategyPosition, strategyRotation) = strategy.Move(strategyTime);

                // Add the default height offset (similar to ExecuteStrategie method)
                var finalPosition = strategyPosition + Vector3.up * _machineModel.HexaPlateMover.DefaultHeight;

                // Update the machine model position and rotation
                _machineModel.HexaPlateMover.UpdatePositionAndRotationTo(finalPosition, strategyRotation);

                var stringedMachineState = new HLMachineState(finalPosition, strategyRotation);

                // NOTE: Create LowLevelInstruction from Motor Position retrieved AFTER IK was run on all Arms.
                var state = _machineModel.MachineStateProvider.CurrentLowLevelMachineState;
                var highLevelInstruction = new HLInstruction(stringedMachineState, stringedMoveTime * timeMultiplier);
                var lowLevelInstruction = new LLInstruction(state, stringedMoveTime * timeMultiplier);
                stringedInstructions.Add((highLevelInstruction, lowLevelInstruction));
            }

            return stringedInstructions;
        }

        private List<(HLInstruction, LLInstruction)> GenerateStringedFrom(AbstractHLInstruction instruction)
        {
            var currentPosition = _machineModel.HexaPlateTransform.position;
            var currentRotation = _machineModel.HexaPlateTransform.rotation;

            var targetPosition = instruction.Instruction.TargetMachineState.PlateCenterPosition;
            var targetRotation = instruction.Instruction.TargetMachineState.PlateRotationQuaternion;

            var moveTime = instruction.Instruction.MoveTime;
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
                var lowLevelInstruction = new LLInstruction(state, stringedMoveTime);
                stringedInstructions.Add((highLevelInstruction, lowLevelInstruction));

                /*
                Debug.Log("motor1Rot: " + state.Motor1Rotation + " motor2Rot: " + state.Motor2Rotation
                          + " motor3Rot: " + state.Motor3Rotation + " motor4Rot: " + state.Motor4Rotation
                          + " motor5Rot: " + state.Motor5Rotation + " motor6Rot: " + state.Motor6Rotation);
                */
            }

            return stringedInstructions;
        }
    }
}