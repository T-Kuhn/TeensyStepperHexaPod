using MachineSimulator.Machine;
using MachineSimulator.MachineModel;
using UnityEngine;

namespace MachineSimulator.Sequencing
{
    public static class SequenceFromCode
    {
        public static void UpDownSequence(MachineModel.MachineModel machineModel, SequenceCreator sequenceCreator, float commandTime)
        {
            var rotation = Quaternion.identity;
            var position = Vector3.up * 0.3f;
            machineModel.HexaPlateMover.UpdatePositionAndRotationTo(position, rotation);
            sequenceCreator.Add(HLInstructionFromCurrentMachineState(machineModel, commandTime));
        }

        public static void UpIntoCircleMovementSequence(MachineModel.MachineModel machineModel, SequenceCreator sequenceCreator, float commandTime)
        {
            var rotation = Quaternion.identity;
            var startPosition = new Vector3(0f, 0.26f, 0.025f);

            // Go to startPosition
            machineModel.HexaPlateMover.UpdatePositionAndRotationTo(startPosition, rotation);
            sequenceCreator.Add(HLInstructionFromCurrentMachineState(machineModel, commandTime));

            // circle strategy
            sequenceCreator.Add(new AbstractStrategyInstruction(StrategyName.MoveInCircle, 0f, Mathf.PI * 2f, commandTime));
        }

        public static HLInstruction HLInstructionFromCurrentMachineState(MachineModel.MachineModel machineModel, float commandTime)
        {
            var platePosition = machineModel.HexaPlateTransform.position;
            var plateRotation = machineModel.HexaPlateTransform.rotation;
            var hlMachineState = new HLMachineState(platePosition, plateRotation);
            var instruction = new HLInstruction(hlMachineState, commandTime);
            return instruction;
        }
    }
}