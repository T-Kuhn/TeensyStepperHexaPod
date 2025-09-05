using MachineSimulator.Machine;

namespace MachineSimulator
{
    public static class Constants
    {
        public const int BaudRate = 921600;

        public static readonly LLMachineState OriginMachineState = new LLMachineState();
        public static readonly LLMachineState OffsetFromTableState = new LLMachineState(-0.1f, 0.1f, -0.1f, 0.1f, -0.1f, 0.1f);
    }
}