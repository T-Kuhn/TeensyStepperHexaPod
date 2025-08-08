using MachineSimulator.Machine;

namespace MachineSimulator
{
    public static class Constants
    {
        public const int BaudRate = 921600;

        public static readonly LLMachineState OriginMachineState = new LLMachineState();
        public static readonly LLMachineState OffsetFromTableState = new LLMachineState(-0.047f, -0.0125f, -0.075f, 0.053f, -0.016f, 0.028f);
    }
}