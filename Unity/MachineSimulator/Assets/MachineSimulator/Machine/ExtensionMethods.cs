using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineSimulator.Machine
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Serialization in order to transfer the data via serial interface
        /// </summary>
        public static string Serialize(this LLInstruction llInstruction)
        {
            var builder = new StringBuilder();

            builder.Append((llInstruction.TargetMachineState.Motor1Rotation).ToString("0.00000"));
            builder.Append(":");
            builder.Append((llInstruction.TargetMachineState.Motor2Rotation).ToString("0.00000"));
            builder.Append(":");
            builder.Append((llInstruction.TargetMachineState.Motor3Rotation).ToString("0.00000"));
            builder.Append(":");
            builder.Append((llInstruction.TargetMachineState.Motor4Rotation).ToString("0.00000"));
            builder.Append(":");
            builder.Append((llInstruction.TargetMachineState.Motor5Rotation).ToString("0.00000"));
            builder.Append(":");
            builder.Append((llInstruction.TargetMachineState.Motor6Rotation).ToString("0.00000"));
            builder.Append(":");
            builder.Append(llInstruction.MoveTime.ToString("0.00000"));

            return builder.ToString();
        }

        public static List<LLInstruction> ToList(this LLMachineState machineState, float moveTime = 1f, bool isLevelingInstruction = false)
        {
            return new List<LLInstruction>
            {
                new LLInstruction(machineState, moveTime, isLevelingInstruction)
            };
        }
        
        public static List<LLInstruction> ToList(this List<LLMachineState> machineStates, float moveTime = 1f, bool isLevelingInstruction = false)
        {
            return machineStates.Select(state => new LLInstruction(state, moveTime, isLevelingInstruction)).ToList();
        }
    }
}