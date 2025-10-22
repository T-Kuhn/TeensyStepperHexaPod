using MachineSimulator.MachineModel;

namespace MachineSimulator.Machine
{
    public class AbstractInstruction
    {
    }

    public class AbstractStrategyInstruction : AbstractInstruction
    {
        public StrategyName StrategyName { get; }
        public float StartTime { get; }
        public float EndTime { get; }
        public float ReferenceMoveTime { get; set; }
        public bool IsLinearMove { get; set; }

        public AbstractStrategyInstruction(StrategyName strategyName, float startTime, float endTime, float referenceMoveTime, bool isLinearMove)
        {
            ReferenceMoveTime = referenceMoveTime;
            StrategyName = strategyName;
            StartTime = startTime;
            EndTime = endTime;
            IsLinearMove = isLinearMove;
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