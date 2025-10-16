namespace MachineSimulator.Machine
{
    public class AbstractInstruction
    {
    }
    
    public class AbstractStrategyInstruction : AbstractInstruction
    {
        public string StrategyName { get; }
        public float Duration { get; }

        public AbstractStrategyInstruction(string strategyName, float duration)
        {
            StrategyName = strategyName;
            Duration = duration;
        }
    }

    public class AbstractHLInstruction : AbstractInstruction
    {
        public HLInstruction Instruction { get; }
        
        public AbstractHLInstruction(HLInstruction instruction)
        {
            Instruction = instruction;
        }
    }
}