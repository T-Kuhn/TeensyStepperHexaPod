namespace MachineSimulator.Machine
{
    public readonly struct HLInstruction
    {
        public HLMachineState TargetMachineState { get; }
        public float MoveTime { get; }

        public HLInstruction(HLMachineState targetMachineState, float moveTime)
        {
            TargetMachineState = targetMachineState;
            MoveTime = moveTime;
        }
    }
}
