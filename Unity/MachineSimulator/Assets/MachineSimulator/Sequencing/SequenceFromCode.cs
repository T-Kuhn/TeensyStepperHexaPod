using System.Threading;
using Cysharp.Threading.Tasks;
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
            var startRotation = Quaternion.identity;
            var startPosition = new Vector3(0f, 0.26f, 0.025f);

            // Go to startPosition
            machineModel.HexaPlateMover.UpdatePositionAndRotationTo(startPosition, startRotation);
            sequenceCreator.Add(HLInstructionFromCurrentMachineState(machineModel, commandTime));

            // circle strategy
            sequenceCreator.Add(new AbstractStrategyInstruction(StrategyName.MoveInCircle, 0f, Mathf.PI * 2f, commandTime, false));
        }

        public static void UpIntoTiltCircleMovementSequence(MachineModel.MachineModel machineModel, SequenceCreator sequenceCreator, float commandTime)
        {
            var startRotation = Quaternion.Euler(0f, 0f, 15f);
            var startPosition = new Vector3(0f, 0.21f, 0f);

            // Go to startPosition
            machineModel.HexaPlateMover.UpdatePositionAndRotationTo(startPosition, startRotation);
            sequenceCreator.Add(HLInstructionFromCurrentMachineState(machineModel, commandTime));

            // circle strategy
            sequenceCreator.Add(new AbstractStrategyInstruction(StrategyName.CircleTilt, 0f, Mathf.PI * 2f, commandTime, false));
        }

        public static async UniTaskVoid StartAsyncExecutionAsync(
            MachineModel.MachineModel machineModel,
            SequenceCreator sequenceCreator,
            float commandTime,
            CancellationToken ct,
            bool executeOnRealMachine = false)
        {
            //await GoUpAndDownForeverAsync(machineModel, sequenceCreator, commandTime, ct, executeOnRealMachine);
			await TiltCircleForeverAsync(machineModel, sequenceCreator, commandTime, ct, executeOnRealMachine);
        }

        // NOTE: command time and height cranked up near to limit (after pressing SpeedX3 button) to optimize for bounce ball height
        private static async UniTask GoUpAndDownForeverAsync(MachineModel.MachineModel machineModel, SequenceCreator sequenceCreator, float commandTime, CancellationToken ct, bool executeOnRealMachine)
        {
            commandTime *= 0.225f;
            var commandTimeInMs = Mathf.RoundToInt(commandTime * 1000f);

            while (true)
            {
                // move up
                sequenceCreator.ClearAll();
                var upRotation = Quaternion.Euler(0f, 0f, 0f);
                var upPosition = new Vector3(0f, 0.22f, 0f);
                machineModel.HexaPlateMover.UpdatePositionAndRotationTo(upPosition, upRotation);
                sequenceCreator.Add(HLInstructionFromCurrentMachineState(machineModel, commandTime));

                machineModel.HexaPlateMover.TeleportToDefaultHeight();
                sequenceCreator.StartStringedPlayback(executeOnRealMachine, onBeforeSendAction: () => machineModel.HexaPlateMover.TeleportToDefaultHeight());

                await UniTask.Delay(commandTimeInMs + 1, cancellationToken: ct);

                // go back to origin
                sequenceCreator.ClearAll();
                machineModel.HexaPlateMover.TeleportToDefaultHeight();
                sequenceCreator.Add(HLInstructionFromCurrentMachineState(machineModel, commandTime));

                machineModel.HexaPlateMover.UpdatePositionAndRotationTo(upPosition, upRotation);
                sequenceCreator.StartStringedPlayback(executeOnRealMachine, onBeforeSendAction: () => machineModel.HexaPlateMover.UpdatePositionAndRotationTo(upPosition, upRotation));

                await UniTask.Delay(commandTimeInMs + 1, cancellationToken: ct);
            }
        }

        private static async UniTask TiltCircleForeverAsync(MachineModel.MachineModel machineModel, SequenceCreator sequenceCreator, float commandTime, CancellationToken ct, bool executeOnRealMachine)
        {
            var commandTimeInMs = Mathf.RoundToInt(commandTime * 1000f);

            // Go to startPosition
            sequenceCreator.ClearAll();
            var startRotation = Quaternion.Euler(0f, 0f, 15f);
            var startPosition = new Vector3(0f, 0.21f, 0f);

            machineModel.HexaPlateMover.UpdatePositionAndRotationTo(startPosition, startRotation);
            sequenceCreator.Add(HLInstructionFromCurrentMachineState(machineModel, commandTime));

            machineModel.HexaPlateMover.TeleportToDefaultHeight();
            sequenceCreator.StartStringedPlayback(executeOnRealMachine, onBeforeSendAction: () => machineModel.HexaPlateMover.TeleportToDefaultHeight());
            await UniTask.Delay(commandTimeInMs + 1, cancellationToken: ct);

            var startTime = 0f;
            var endTime = Mathf.PI * 2f;
            var duration = endTime - startTime;
            var timeMultiplier = commandTime / 3f;
            var tiltWaitDelay = Mathf.RoundToInt(duration * timeMultiplier * 1000f);

            while (true)
            {
                // circle strategy
                sequenceCreator.ClearAll();
                sequenceCreator.Add(new AbstractStrategyInstruction(StrategyName.CircleTilt, startTime, endTime, commandTime, false));

                machineModel.HexaPlateMover.UpdatePositionAndRotationTo(startPosition, startRotation);
                sequenceCreator.StartStringedPlayback(executeOnRealMachine, onBeforeSendAction: () => machineModel.HexaPlateMover.UpdatePositionAndRotationTo(startPosition, startRotation));

                await UniTask.Delay(tiltWaitDelay + 1, cancellationToken: ct);
            }
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