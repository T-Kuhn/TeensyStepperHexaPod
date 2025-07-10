using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MachineSimulator.Machine
{
    public abstract class InstructableMachine : MonoBehaviour
    {
        private LLMachineState _levelingOffset = new LLMachineState();
        
        
        public void Instruct(List<LLInstruction> instructions)
        {
            // NOTE: The current Max amount of instructions which can be sent in one go is 100.
            var diffInstructionList = instructions
                .Take(100)
                .Select(instruction =>
                    new LLInstruction(
                        instruction.TargetMachineState + _levelingOffset,
                        instruction.MoveTime))
                .ToList();
            
            var levelingInstructions = instructions 
                .Where(instruction => instruction.IsLevelingInstruction)
                .ToList();
            
            SendInstructions(diffInstructionList);
            
            foreach(var instruction in levelingInstructions)
            {
                _levelingOffset += instruction.TargetMachineState;
            }
        }

        protected abstract void SendInstructions(List<LLInstruction> diffInstructions);

        public abstract void GoToOrigin();
    }
}